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
- SQL Server LocalDB hoặc SQL Server Express.
- `sqlcmd`.

Các bước nhanh:

```powershell
cd D:\AI_AGENCY_AGENTS\WebBanHang
powershell -ExecutionPolicy Bypass -File .\database\restore_localdb.ps1
dotnet restore .\src\BanHangcoiWeb\BanHangcoiWeb.csproj
dotnet run --project .\src\BanHangcoiWeb\BanHangcoiWeb.csproj --urls http://localhost:5188
```

Mở web:

```text
http://localhost:5188
```

Connection string mặc định trong `src/BanHangcoiWeb/appsettings.json`:

```text
Server=(localdb)\MSSQLLocalDB;Database=BanHangcoiCrm;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Nếu máy ở nhà dùng SQL Server Express thay vì LocalDB, đổi server thành:

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
