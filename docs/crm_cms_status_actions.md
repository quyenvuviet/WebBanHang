# Trạng thái CRM/CMS và action live

Tài liệu này định nghĩa trạng thái, action và automation cho hệ thống bán phụ tùng ô tô.

## 1. Role

| Role | Quyền chính |
|---|---|
| Admin | Quản lý user, role, import, mapping, cấu hình, sửa dữ liệu khóa |
| Manager | Xem team, chia lead, duyệt override, xem KPI |
| Sale | Xử lý lead được giao, gọi, hẹn lại, chốt đơn |
| Kho | Xem đơn cần đóng, đóng hàng, cập nhật tồn kho, hàng lỗi, hàng hoàn |
| Shipping | Cập nhật mã vận đơn, trạng thái giao, delay, hoàn |
| Support | Xử lý ticket khách nhận hàng, lỗi sản phẩm, đổi/bảo hành |
| Marketing | Quản lý campaign, page, content, ngân sách, CPL/ROAS |
| Finance | Đối soát COD, doanh số, phí ship |
| QA Readonly | Xem và export report để nghiệm thu |

## 2. Lead status

| Status | Nghĩa | Ai được chuyển | Điều kiện |
|---|---|---|---|
| `new` | Lead mới từ marketing/import | System/Admin | Có phone hoặc đủ thông tin raw |
| `assigned` | Đã giao sale | Manager/System | Có sale phụ trách |
| `called_once` | Đã gọi lần 1 | Sale | Có call log lần 1 |
| `no_answer` | Không nghe máy/thuê bao | Sale | Có kết quả gọi và lịch gọi lại hoặc lý do đóng |
| `follow_up` | Hẹn gọi lại/khách cần cân nhắc | Sale | Bắt buộc `next_action_at` |
| `low_intent` | Nhu cầu thấp | Sale | Có ghi chú lý do |
| `high_intent` | Nhu cầu cao | Sale | Có sản phẩm quan tâm và ghi chú nhu cầu |
| `qualified` | Đủ điều kiện chốt | Sale/Manager | Có phone, sản phẩm, giá dự kiến |
| `closed_won` | Đã chốt đơn | Sale | Tạo order thành công |
| `closed_lost` | Mất lead | Sale/Manager | Bắt buộc lý do |
| `recycled` | Đưa về kho số để chăm lại | Manager | Có lý do recycle |
| `duplicate` | Trùng số | System/Admin | Có lead gốc để link |
| `invalid` | Sai số/dữ liệu lỗi | System/Admin | Có rejection reason |

## 3. Order status

| Status | Nghĩa | Ai được chuyển | Điều kiện |
|---|---|---|---|
| `draft` | Đơn nháp | Sale/System | Có lead hoặc customer |
| `confirmed` | Đã xác nhận chốt | Sale | Có khách, SĐT, địa chỉ, sản phẩm, số lượng, COD |
| `waiting_pack` | Chờ kho đóng | System/Kho | Đơn confirmed và không thiếu thông tin |
| `packed` | Đã đóng hàng | Kho | Có item, tồn kho hợp lệ, audit người đóng |
| `handover_to_carrier` | Đã bàn giao vận chuyển | Kho/Shipping | Có đơn vị VC và mã vận đơn nếu có |
| `shipping` | Đang giao | Shipping/System | Có shipment active |
| `delivery_issue` | Có vấn đề giao | Shipping/Support/System | Có ghi chú vấn đề hoặc ticket |
| `delivered` | Khách đã nhận | Shipping/System | Có ngày thành công |
| `returned` | Hoàn hàng | Shipping/Support | Bắt buộc lý do hoàn và movement hàng hoàn |
| `cancelled` | Hủy | Sale/Manager/Shipping | Bắt buộc lý do hủy |
| `done` | Hoàn tất cuối | System/Manager | Trạng thái cuối + không còn ticket mở |

## 4. Support ticket status

| Status | Nghĩa |
|---|---|
| `open` | Mới phát sinh vấn đề |
| `in_progress` | Đang xử lý |
| `waiting_customer` | Đang đợi khách phản hồi/nhận hàng |
| `resolved` | Đã xử lý xong nghiệp vụ |
| `closed` | Đóng hồ sơ hỗ trợ |

## 5. Live action theo quy trình

### Marketing => lấy số

Action:

- Tạo campaign theo page/content/sản phẩm.
- Import hoặc đồng bộ lead từ form/inbox/call tracking.
- Gắn `lead_type`: `SỐ NÓNG` hoặc `SỐ CHECK`.

Automation:

- Nếu phone trùng, cảnh báo lead cũ và lịch sử mua/gọi.
- Nếu lead mới chưa được giao sau 15 phút, báo Manager.
- Dashboard hiện CPL, lead/page, lead/content, lead/product.

### Lấy số => gọi

Action:

- Chia lead cho sale theo rule: page, sản phẩm, sale online, tải việc.
- Sale xem màn hình lead gồm lịch sử call/order/support.

Automation:

- Số nóng phải gọi trong 5-15 phút.
- Quá SLA thì tạo alert cho sale và manager.
- Nếu `no_answer`, hệ thống tạo task gọi lại lần 2/3/4.

### Gọi => chốt

Action:

- Sale ghi kết quả từng lần gọi, số phút, nỗi đau, giá khách chấp nhận, lý do chưa mua.
- Khi chốt, sale tạo order từ lead.

Bắt buộc trước khi chốt:

- SĐT hợp lệ.
- Tên khách hoặc tên tạm.
- Địa chỉ giao hàng.
- Sản phẩm/SKU.
- Số lượng.
- COD/giá trị đơn.
- Phí ship nếu có.
- Ghi chú điện áp/màu/combo với hàng cần phân biệt.

Automation:

- Không cho `closed_won` nếu order thiếu trường bắt buộc.
- Giá trị đơn chốt phải khớp tổng item.
- Nếu sản phẩm tồn thấp hoặc chưa map, cảnh báo kho/manager.

### Chốt => đóng hàng

Action:

- Kho xem queue `waiting_pack`.
- Kho scan/confirm item, số lượng, màu/điện áp/combo.
- Kho chuyển `packed`.

Automation:

- Đơn confirmed từ quá cut-off mà chưa packed thì alert.
- Packed thành công tạo inventory movement `sale_out`.
- Hủy packed/sửa packed phải có reason và audit.

### Đóng hàng => vận chuyển

Action:

- Tạo shipment, carrier, tracking code.
- Cập nhật trạng thái đang giao/thành công/hoàn/hủy.

Automation:

- Tracking code không được trùng.
- Đơn packed mà thiếu tracking code quá SLA thì alert.
- Nếu carrier báo delay/không liên hệ được/hoàn, mở support ticket.

### Vận chuyển => hỗ trợ nhận hàng

Action:

- CS/Sale gọi khách khi đơn delay, ship không liên hệ được, khách hẹn nhận, khách đổi ý.
- Ghi lý do và kết quả xử lý.

Automation:

- Đơn `delivery_issue` tạo ticket `open`.
- Ticket quá SLA chưa xử lý thì alert support lead.
- Đơn `returned` tạo movement `return_in` nếu hàng về kho.

### Hỗ trợ => done

Action:

- Xác nhận đơn đã giao thành công hoặc đã đóng kết quả hoàn/hủy.
- Đóng ticket nếu không còn vấn đề.

Điều kiện `done`:

- Order là `delivered`, `returned`, hoặc `cancelled`.
- Không còn support ticket `open`, `in_progress`, `waiting_customer`.
- COD/công nợ đã đối soát nếu role finance bắt buộc.
- Audit log có đầy đủ các bước quan trọng.

## 6. Dashboard bắt buộc

Marketing:

- Spend, lead, CPL, đơn chốt, doanh số chốt, doanh số thành công, ROAS theo page/content.

Sale:

- Lead được giao, call attempts, contact rate, high intent rate, close rate, doanh số chốt, AOV.

Kho:

- Đơn chờ đóng, đơn đã đóng, đơn lỗi, thời gian từ confirmed đến packed, tồn kho.

Vận chuyển:

- Đơn đang giao, thành công, hoàn, hủy, delay, tỷ lệ thành công theo carrier.

Support:

- Ticket mở, quá SLA, lý do hoàn top, lỗi sản phẩm top, tỷ lệ cứu đơn.

Manager:

- Funnel lead -> call -> chốt -> packed -> shipping -> delivered -> done.
- KPI theo sale/page/sản phẩm/carrier.
