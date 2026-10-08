# Gói triển khai CRM/CMS BanHangcoi

Gói triển khai này dùng để chuyển đổi 2 workbook:

- `D:\AI_AGENCY_AGENTS\BanHangcoi\Đi đơn tháng 8.xlsx`
- `D:\AI_AGENCY_AGENTS\BanHangcoi\PTOTdữ liệu.xlsx`

thành hệ thống CRM/CMS bán phụ tùng ô tô có SQL lưu trữ, đăng nhập theo vai trò, và action live từ marketing đến đơn hàng hoàn tất.

## File trong gói

- `schema.sql`: schema PostgreSQL gồm staging import, CRM, đơn hàng, vận đơn, kho, hỗ trợ, KPI và audit log.
- `excel_import_pipeline.md`: quy trình đọc Excel, làm sạch, chống trùng, nạp vào SQL và đối soát.
- `crm_cms_status_actions.md`: trạng thái và action live cho marketing, sale, kho, vận chuyển, CSKH.
- `go_live_acceptance_checklist.md`: checklist nghiệm thu dữ liệu, nghiệp vụ, role, audit và báo cáo.

## Hướng triển khai khuyến nghị

1. Tạo database PostgreSQL và chạy `schema.sql`.
2. Import Excel vào các bảng `stg_*` trước, không nạp thẳng vào bảng chính.
3. Chạy làm sạch phone/date/money/product/status.
4. Đối soát raw vs staging vs bảng chính.
5. Dùng CRM/CMS theo các status/action trong `crm_cms_status_actions.md`.
6. Chỉ go-live khi đạt checklist trong `go_live_acceptance_checklist.md`.

## Nguyên tắc quan trọng

- Không xóa im lặng dòng Excel nào. Mỗi dòng import phải có `source_file`, `source_sheet`, `source_row`, `import_batch_id`.
- KPI không lấy từ sheet báo cáo tĩnh. KPI phải tính lại từ SQL giao dịch.
- Đơn chỉ `done` khi vận chuyển, ticket và công nợ đã đóng hết.
- Sale không được sửa dữ liệu sau khi kho/vận chuyển đã xử lý nếu không có quyền và audit reason.
