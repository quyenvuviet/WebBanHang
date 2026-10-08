# Checklist nghiệm thu go-live

Trạng thái mặc định: `NEEDS WORK`.

Chỉ chuyển `READY FOR GO-LIVE` khi tất cả mục critical đạt và có bằng chứng.

## 1. Nghiệm thu import dữ liệu

| Check | Pass khi |
|---|---|
| Import batch có log | Có `import_batches`, số dòng raw/staged/accepted/rejected |
| Truy vết Excel | Mỗi record có `source_file`, `source_sheet`, `source_row`, `import_batch_id` |
| Phone | Không mất số 0 đầu, phone sai bị reject/flag |
| Date | Excel serial được đổi đúng ngày |
| Money | COD, phí ship, doanh số, spend lưu numeric VND |
| Product | Sản phẩm trong order khớp `products` hoặc nằm trong danh sách chưa map |
| Status | Lead/order status được map enum |
| Duplicate | SĐT/tracking/order trùng có warning hoặc merge rule |
| Re-import | Import lại cùng batch không nhân đôi dữ liệu |
| Đối soát | Tổng lead, order, delivered, returned, cancelled, COD khớp nguồn trong tolerance |

Critical blockers:

- Mất dòng Excel mà không có rejection.
- Mất số 0 đầu SĐT.
- Re-import tạo duplicate lớn.
- KPI lấy từ sheet tổng hợp tĩnh thay vì SQL giao dịch.

## 2. Nghiệm thu marketing

- Tạo/sửa campaign theo page/content/sản phẩm.
- Nhập spend theo ngày.
- Tạo lead có nguồn page/content/campaign.
- Dashboard có spend, lead, CPL, order, revenue, ROAS.
- Marketing không sửa được order/kho/vận đơn.

## 3. Nghiệm thu sale/call

- Sale chỉ thấy lead được giao nếu phân quyền theo nhân viên.
- Sale ghi được call lần 1-4 với ngày, kết quả, số phút, ghi chú.
- `follow_up` bắt buộc ngày giờ gọi lại.
- `no_answer` tạo lịch gọi lại hoặc lý do đóng.
- `closed_won` tạo order và link lead gốc.
- Không cho chốt nếu thiếu SĐT/sản phẩm/giá/địa chỉ.

Test case bắt buộc:

1. Lead hợp lệ -> gọi lần 1 -> high intent -> chốt -> order confirmed.
2. Lead KNM -> hệ thống tạo task gọi lại.
3. Lead trùng SĐT -> hiện cảnh báo lịch sử.
4. Sale A không sửa được lead của Sale B.

## 4. Nghiệm thu order/kho

- Order có nhiều item không bị nhân đôi doanh số.
- Order confirmed chuyển vào queue `waiting_pack`.
- Kho chuyển `packed` và tạo movement `sale_out`.
- Tồn không đủ thì chặn hoặc yêu cầu manager override.
- Sửa/hủy packed phải có reason và audit log.

Test case bắt buộc:

1. Đơn có 2 sản phẩm, COD khớp tổng item.
2. Đơn thiếu địa chỉ không được packed.
3. Đơn tồn kho không đủ bị chặn.
4. Manager override tồn kho có audit reason.

## 5. Nghiệm thu vận chuyển

- Đơn packed có thể tạo shipment.
- Tracking code unique.
- Trạng thái `delivered` bắt buộc ngày thành công.
- Trạng thái `returned` bắt buộc lý do hoàn và movement `return_in`.
- Delay/không liên hệ được tự động tạo support ticket.

Test case bắt buộc:

1. Packed -> handover -> shipping -> delivered -> done.
2. Packed -> shipping -> delivery_issue -> support ticket.
3. Shipping -> returned -> return reason + inventory return.
4. Tracking code trùng bị chặn.

## 6. Nghiệm thu support/done

- Ticket gắn với order/customer.
- Ticket có status: open, in_progress, waiting_customer, resolved, closed.
- Đơn có ticket mở không được `done`.
- Đơn chỉ `done` khi order terminal và ticket đã đóng.

Test case bắt buộc:

1. Đơn delivered không ticket -> done.
2. Đơn delivered có ticket open -> không done.
3. Đóng ticket -> done thành công.
4. Đơn returned có lý do và hàng hoàn -> done sau khi kho/CS đóng việc.

## 7. Nghiệm thu login/role

| Role | Phải đạt |
|---|---|
| Admin | Quản lý user, role, import, mapping, override |
| Manager | Xem team, chia lead, duyệt override, xem KPI team |
| Sale | Xử lý lead/order mình phụ trách, không sửa kho/vận đơn sau packed |
| Warehouse | Xử lý đóng hàng/kho, không sửa marketing spend |
| Shipping | Cập nhật shipment, không sửa COD/sản phẩm |
| Support | Xử lý ticket, không sửa KPI/import |
| Marketing | Quản lý campaign/spend, không sửa order/kho |
| Finance | Đối soát COD/report, không sửa call history |
| QA Readonly | Xem/export, không sửa dữ liệu |

Critical blockers:

- Sale xem/sửa toàn bộ công ty khi chưa được cấp quyền.
- User không role vẫn truy cập được dữ liệu.
- Sửa COD/sản phẩm sau packed mà không cần manager/audit.

## 8. Audit log bắt buộc

Phải log:

- Login/logout/login fail.
- Import batch.
- Tạo/sửa/xóa lead, customer, order, item, shipment.
- Đổi owner lead.
- Chuyển status lead/order/ticket.
- Sửa COD, phí ship, sản phẩm, số lượng, tracking code.
- Override tồn kho.
- Export dữ liệu khách hàng.

Mỗi log phải có:

- `actor_id`
- `actor_role`
- `entity_type`
- `entity_id`
- `action`
- `old_value`
- `new_value`
- `reason` nếu là action rủi ro
- `created_at`
- IP/device nếu có

## 9. Báo cáo/KPI bắt buộc

- Lead theo ngày/page/content/sale.
- Call attempts, contact rate, follow-up overdue.
- Funnel: lead -> high intent -> closed won -> packed -> shipping -> delivered -> done.
- Doanh số chốt, doanh số thành công, doanh số hoàn/hủy.
- AOV theo sale/page/product.
- KPI call và KPI doanh số theo sale.
- Tồn đầu kỳ, nhập, xuất, hoàn, lỗi, tồn cuối.
- Marketing spend, CPL, CPA, ROAS.
- Tỷ lệ hoàn theo sale/page/product/carrier.
- Đơn thiếu vận đơn, đơn chưa đóng, đơn quá SLA.
- Ticket hỗ trợ theo issue type và SLA.

## 10. Quyết định go-live

`READY FOR GO-LIVE` khi:

- 100% critical blockers đã xử lý.
- Import đối soát được raw/staging/final.
- Re-import không duplicate.
- Role/permission pass.
- Audit log pass cho status/field nhạy cảm.
- Ít nhất 1 luồng thật chạy thành công: lead -> call -> chốt -> packed -> shipping -> delivered -> done.
- Báo cáo SQL khớp dữ liệu nguồn trong tolerance đã thỏa thuận.

Nếu còn lỗi nhưng không critical, go-live có điều kiện phải có owner, deadline và rollback plan.
