# Pipeline import Excel

Pipeline này dùng để chuyển `Đi đơn tháng 8.xlsx` và `PTOTdữ liệu.xlsx` vào SQL mà vẫn giữ được truy vết.

## 1. Nguồn dữ liệu

### Workbook `Đi đơn tháng 8.xlsx`

Sheet cần import:

- `Danh sách đơn`: đơn hàng, khách, sản phẩm, COD, phí ship, kho vận, mã vận đơn, trạng thái đơn, lý do hoàn, ghi chú.
- `Nhập hàng`: sản phẩm và số lượng nhập theo ngày.
- `Hàng Hoàn`: sản phẩm và số lượng hoàn theo ngày.
- `Kho`: tồn đầu kỳ, xuất, nhập, hoàn, lỗi, tồn cuối kỳ.

Sheet chỉ dùng đối soát:

- `Vận đơn`
- `Raw`
- `Đữ liệu`

### Workbook `PTOTdữ liệu.xlsx`

Sheet cần import CRM:

- `Nhân viên A`
- `Nhân viên B`
- `Nhân Viên C`
- `Nhân Viên D`
- `Nhân Viên E`
- `Nhan viên H`
- `Kho số thả nổi`
- `Databackup`

Sheet cần import marketing:

- ` Ngân Sách Maketing`

Sheet chỉ dùng đối soát:

- `Báo cáo tổng hợp`
- `Raw`

## 2. Nguyên tắc staging

Mỗi dòng Excel đầu tiên phải vào `stg_excel_rows`, gồm:

- `import_batch_id`
- `source_file`
- `source_sheet`
- `source_row`
- `row_json`
- `row_hash`

Không load trực tiếp vào `customers`, `crm_leads`, `orders`.

## 3. Mapping lead CRM

Nguồn: các sheet nhân viên, `Kho số thả nổi`, `Databackup`.

| Cột Excel | Bảng đích | Field |
|---|---|---|
| `NGÀY SỐ VỀ` | `crm_leads` | `lead_date` |
| `TÊN KHÁCH HÀNG` | `customers`, `crm_leads` | `customer_name`, `customer_name_raw` |
| `SĐT` | `customers`, `crm_leads` | `phone_normalized` |
| `SỐ NÓNG/SỐ CHECK` | `crm_leads` | `lead_type` |
| `PAGE` | `crm_leads`, `marketing_campaigns` | `page_name` |
| `CONTENT` | `crm_leads`, `marketing_campaigns` | `content_name` |
| `SP KHÁCH CHỐT` | `crm_leads`, `products` | `product_interest_raw`, `product_id` |
| `XE CÓ HƠI/KHÔNG HƠI` | `crm_leads` | `vehicle_air_type` |
| `LOẠI XE` | `crm_leads` | `vehicle_type` |
| `SALE` | `crm_leads`, `app_users` | `assigned_user_id`, `sale_name_raw` |
| `GỌI LẦN 1-4` | `crm_call_attempts` | `result_raw`, `result_status` |
| `NGÀY GỌI LẦN 1-4` | `crm_call_attempts` | `call_date` |
| `GHI CHÚ GỌI LẦN 1-4` | `crm_call_attempts` | `pain_note` |
| `SỐ PHÚT GỌI` | `crm_call_attempts` | `duration_seconds` |
| `GIÁ TRỊ ĐƠN CHỐT` | `orders` | `total_amount_vnd` |
| `NGÀY CHỐT ĐƠN` | `orders` | `order_date` |
| `NOTE` | `crm_leads` | `next_action_note` |

## 4. Mapping đơn hàng

Nguồn: `Đi đơn tháng 8.xlsx` sheet `Danh sách đơn`.

| Cột Excel | Bảng đích | Field |
|---|---|---|
| `NGÀY CHỐT ĐƠN` | `orders` | `order_date` |
| `PAGE` | `crm_leads`, `marketing_campaigns` | `page_name` |
| `TÊN KHÁCH HÀNG` | `customers`, `orders` | `customer_name`, `customer_name_raw` |
| `SỐ ĐIỆN THOẠI` | `customers`, `orders` | `phone_normalized` |
| `ĐIA CHỈ` | `customers`, `orders` | `address_raw` |
| `COMBO SP MUA` | `orders` | `combo_raw` |
| `SẢN PHẨM` | `order_items`, `products` | `product_name_raw`, `product_id` |
| `SỐ LƯỢNG` | `order_items` | `quantity` |
| `SALE` | `orders`, `app_users` | `sale_user_id` |
| `COD` | `orders` | `cod_amount_vnd` |
| `PHÍ SHIP` | `orders` | `shipping_fee_vnd` |
| `KHO VẬN` | `orders` | `order_status` |
| `MÃ VẬN ĐƠN` | `shipments` | `tracking_code` |
| `ĐƠN VỊ VC` | `shipments` | `carrier_name` |
| `TRẠNG THÁI ĐƠN` | `orders`, `shipments` | `order_status`, `shipment_status` |
| `NGÀY THÀNH CÔNG` | `shipments`, `orders` | `delivered_at` |
| `XỬ LÝ ĐƠN` | `support_tickets` hoặc `shipments` | `delivery_issue_note` |
| `LÍ DO HOÀN` | `shipments`, `support_tickets` | `return_reason`, `issue_note` |
| `MỨC ĐỘ HÀI LÒNG` | `support_tickets` | `resolution_note` |
| `GHI CHÚ` | `orders` | `note_raw` |

## 5. Quy tắc làm sạch

### Phone

- Lấy chữ số từ ô gốc.
- Nếu có `84` đầu số thì đổi thành `0`.
- Nếu có 9 chữ số và đúng pattern mobile Việt Nam thì thêm `0`.
- Nếu nhỏ hơn 9 hoặc lớn hơn 11 chữ số thì reject hoặc flag.
- Không bao giờ lưu phone thành numeric vì sẽ mất số 0 đầu.

### Date

- Excel serial như `46235` phải đổi thành ngày thật. Với file này `46235 = 2026-08-01`.
- Nếu cell đã là datetime thì lấy ngày.
- Nếu text không parse được thì đưa vào `stg_rejections`.

### Money

- Bỏ `đ`, khoảng trắng, dấu phân tách nghìn.
- `1.200.000 đ` -> `1200000`.
- `1700k` -> `1700000`.
- Các giá trị bất thường như `0`, số quá thấp, số quá cao cần flag để QA.

### Product

- Tạo danh mục `products` trước khi import đơn.
- Tạo `product_aliases` cho các tên biến thể:
  - `70 - 1M`
  - `Còi hơi 70cm`
  - `Còi hơi 1m`
  - `ĐỆM GHẾ CÔNG THÁI HỌC`
  - `ĐẾ ĐỎ`
- Nếu sản phẩm chưa map thì không nạp vào order item chính, đưa vào danh sách cần map.

### Status

Lead:

- `KNM`, `k nghe máy`, `thuê bao`, `tb` -> `no_answer`
- `hẹn gọi lại`, `mua a nhắn`, `để xem`, `cân đối` -> `follow_up`
- `nhu cầu cao` -> `high_intent`
- `nhu cầu thấp` -> `low_intent`
- `chốt`, `CHỐT ĐƠN` -> `closed_won`
- `không quan tâm`, `k mua`, `mua rồi` -> `closed_lost`

Order:

- `Đã đóng hàng` -> `packed`
- `Đang vận chuyển`, `Đang giao` -> `shipping`
- `Thành công` -> `delivered`
- `Hoàn` -> `returned`
- `Hủy` -> `cancelled`
- `KHÔNG ĐI ĐƠN` -> `cancelled`

## 6. Deduplication

Customer:

- Khóa chính: `phone_normalized`.
- Nếu trùng phone, cập nhật tên/địa chỉ mới nhất nhưng giữ lịch sử raw.

Lead:

- Khóa gợi ý: `phone_normalized + page_name + product_interest_raw + lead_date`.
- Nếu cùng SĐT xuất hiện ở nhiều sheet nhân viên, tạo duplicate warning trước khi merge.

Order:

- Ưu tiên `tracking_code`.
- Fallback: `phone_normalized + order_date + cod_amount_vnd + combo_raw`.
- Sheet `Danh sách đơn` có thể có nhiều dòng cho cùng một đơn, nên phải gom thành `orders` + `order_items`.

Shipment:

- `tracking_code` unique nếu không rỗng.
- Nếu trùng tracking code, kiểm tra có phải cùng một đơn nhiều dòng sản phẩm hay lỗi import.

## 7. Thứ tự nạp dữ liệu

1. `import_batches`
2. `stg_excel_rows`
3. `app_users`
4. `products`, `product_aliases`
5. `marketing_campaigns`, `marketing_spend_daily`
6. `customers`, `customer_phones`
7. `crm_leads`
8. `crm_call_attempts`
9. `orders`
10. `order_items`
11. `shipments`
12. `inventory_movements`, `stock_snapshots`
13. `support_tickets`
14. `audit_logs` cho các import/action quan trọng

## 8. Đối soát sau import

Bắt buộc có report:

- Raw rows vs staged rows vs accepted rows vs rejected rows.
- Số lead theo sheet nhân viên.
- Số phone thiếu/sai/trùng.
- Số order theo trạng thái: delivered, returned, cancelled, shipping.
- Tổng COD theo trạng thái.
- Số tracking code rỗng/trùng.
- Số product chưa map.
- Số đơn có nhiều item.
- Tổng nhập/xuất/hoàn/lỗi/tổng tồn theo sản phẩm.

## 9. Lỗi phải chặn trước khi nạp chính

- Mất số 0 đầu SĐT.
- Ngày Excel serial bị lệch.
- Tiền bị lưu text.
- Header lỗi như `ƒ√`, `17363`, `42693`, `C31` làm lệch cột.
- Import nhầm dòng tổng hợp/báo cáo thành giao dịch.
- Re-import sinh duplicate.
- Đơn đã thành công nhưng thiếu ngày thành công.
- Đơn hoàn thiếu lý do hoàn.
- Đơn đã đóng hàng thiếu mã vận đơn.
