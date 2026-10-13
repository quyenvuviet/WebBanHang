# WebBanHang - CRM bán phụ tùng ô tô

Repo này chứa đầy đủ phần web ASP.NET Core, tài liệu triển khai, file Excel nguồn và backup SQL Server để chuyển sang máy khác.

## Cấu trúc

- `src/BanHangcoiWeb`: mã nguồn ASP.NET Core MVC.
- `database/BanHangcoiCrm.bak`: backup SQL Server có dữ liệu hiện tại.
- `database/restore_localdb.ps1`: script restore database vào SQL Server LocalDB.
- `database/restore_localdb.sql`: script SQL tham khảo nếu muốn restore thủ công.
- `data/excel`: file Excel nguồn đã dùng để import.
- `docs`: tài liệu schema, pipeline import, trạng thái CRM/CMS và checklist nghiệm thu.

## Chạy trên máy ở nhà

Yêu cầu:

- .NET SDK 10 hoặc mới hơn.
- SQL Server 2025 LocalDB (17.x). Backup đi kèm không thể restore trên SQL Server 2012/2016/2019/2022.
- `sqlcmd`.

Các bước nhanh:

```powershell
cd C:\Users\quyenbin\Desktop\WebBanHang\WebBanHang
powershell -ExecutionPolicy Bypass -File .\start_local.ps1
```

Mở web:

```text
http://localhost:5188
```

Connection string mặc định trong `src/BanHangcoiWeb/appsettings.json`:

```text
Server=(localdb)\BanHangcoi2025;Database=BanHangcoiCrm;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Script tạo instance riêng `BanHangcoi2025`, kiểm tra phiên bản và khôi phục backup vào `database/local`. Database đã tồn tại được giữ nguyên khi chạy lại. Không chạy script restore cũ có `DROP DATABASE` hoặc `REPLACE`.

Trong SSMS, kết nối `(localdb)\BanHangcoi2025` bằng Windows Authentication sau khi chạy script. Script SQL dùng biến SQLCMD; nên chạy qua script PowerShell ở trên.

Excel nguồn được đọc từ `data/excel`. Có thể đổi thư mục bằng cấu hình `ExcelImport:Directory`.

Nếu dùng SQL Server Express **2025** thay vì LocalDB, restore vào instance đó và đổi connection string thành:

```text
Server=.\SQLEXPRESS;Database=BanHangcoiCrm;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

## Tài khoản thử nghiệm

| Vai trò | Tài khoản | Mật khẩu |
|---|---|---|
| Quản trị | `admin` | `Admin@123` |
| Quản lý | `manager` | `Manager@123` |
| Marketing | `marketing` | `Marketing@123` |
| Sale | `sale.hong` | `Sale@123` |
| Sale | `sale.phuong` | `Sale@123` |
| Sale | `sale.giang` | `Sale@123` |
| Kho | `kho` | `Kho@123` |
| Vận chuyển | `shipping` | `Shipping@123` |
| CSKH | `support` | `Support@123` |
| Kế toán | `finance` | `Finance@123` |

## Git

Repo đã bỏ qua `bin`, `obj`, `.vs` và file user local. Backup database `.bak`, Excel nguồn, docs và source code được giữ lại để chuyển máy.
