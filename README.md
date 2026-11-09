# Hệ thống quản lý siêu thị mini

ASP.NET Core 10, EF Core, SQL Server, Razor HTML, CSS, JavaScript và Bootstrap. Giao diện dùng cookie đăng nhập cùng origin với API. Giao diện Razor gửi biểu mẫu MVC có CSRF token. Web API cung cấp tra cứu sản phẩm, tồn kho, báo cáo và ghi giao dịch bán hàng JSON dùng chung nghiệp vụ với POS.

## Chạy local trên Windows

Cài SDK .NET 9, PowerShell 7, SQL Server Express và sqlcmd. Chạy từ thư mục gốc bằng PowerShell.

```powershell
dotnet --list-sdks
sqlcmd -S '.\SQLEXPRESS' -E -C -b -f 65001 -i CoSoDuLieu/01_CreateDatabase.sql
sqlcmd -S '.\SQLEXPRESS' -E -C -b -d QuanLySieuThiDb -f 65001 -i CoSoDuLieu/03_DemoData.sql

$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=QuanLySieuThiDb;Integrated Security=true;TrustServerCertificate=true'
$env:BootstrapAdmin__Username = 'admin'
$env:BootstrapAdmin__Password = Read-Host 'Mật khẩu Admin' -MaskInput
dotnet build QuanLySieuThi.csproj -c Release
dotnet run --project QuanLySieuThi.csproj --launch-profile http
```

Script 01 tạo schema lần đầu trên DB mới. Script 03 có thể chạy lại để bổ sung các bản ghi mẫu còn thiếu. Mở http://localhost:5233/Auth/Login. Sau khi tạo Admin, bỏ hai biến bootstrap khỏi phiên. Admin tạo Cashier và StockStaff qua mục Nhân viên. Khi nâng cấp cơ sở dữ liệu đã có, xem [ghi chú SQL](CoSoDuLieu/README.md).

| Vai trò | Nghiệp vụ |
|---|---|
| Admin | Sản phẩm, nhà cung cấp, tài khoản nhân viên, báo cáo và toàn bộ nghiệp vụ cửa hàng |
| Cashier | Hồ sơ khách hàng, bán hàng và hóa đơn của mình |
| StockStaff | Phiếu nhập, tồn kho và nhật ký kho |

## Web API

| Endpoint | Quyền | Tham số |
|---|---|---|
| GET /api/products | Admin, Cashier, StockStaff | search theo SKU hoặc tên |
| GET /api/inventory | Admin, StockStaff | lowStock=true |
| GET /api/reports/sales | Admin | from, to định dạng yyyy-MM-dd, UTC |

API bán hàng dành cho Admin và Cashier. Gọi GET /api/sales/checkout-token sau khi đăng nhập, giữ cookie và gửi requestToken trong header có tên headerName khi gọi POST /api/sales/checkout với JSON. Ví dụ thân yêu cầu dùng requestId là UUID mới và productId lấy từ danh mục.

Trong màn hình **Báo cáo**, Admin có thể tải workbook Excel gồm doanh thu theo ngày, hóa đơn, tồn kho hiện tại và 500 sản phẩm bán chạy nhất. Khoảng thời gian áp dụng cho doanh thu, hóa đơn và hàng bán chạy; tồn kho là ảnh chụp hiện tại. Doanh thu và hàng bán chạy chỉ tính hóa đơn đã hoàn tất.

```json
{
  "requestId": "cd8f4256-bbf3-4715-a9af-2c8e08b06d3e",
  "paymentMethod": "Cash",
  "lines": [{ "productId": 1, "quantity": 2 }]
}
```

API trả mã hóa đơn, trạng thái và tổng tiền. Khi gửi lại cùng requestId của cùng tài khoản, API trả hóa đơn đã ghi. Có thể truyền customerId và promotionCode. Một requestId dùng cho một giao dịch.

Sản phẩm và tồn kho trả tối đa 100 dòng. API trả 401 khi thiếu phiên và 403 khi sai vai trò. Báo cáo trả 400 cho khoảng ngày đảo chiều hoặc dài hơn 366 ngày.

## Kiểm thử và bàn giao

- [V-Model và phân công 8 người](TaiLieu/V-Model.md)
- [SRS và mã yêu cầu](TaiLieu/SRS.md)
- [Use Case, Activity, Sequence, Class Diagram và ERD](TaiLieu/Design.md)
- [Test Case và kết quả](TaiLieu/Verification.md)
- [Postman Collection](KiemThu/MiniSupermarket.postman_collection.json)
- Chạy `pwsh ./KiemThu/Smoke.ps1 -BaseUrl http://localhost:5233` với DB QA đã seed và thông tin Admin QA trong biến môi trường. Script tạo giao dịch thật trên DB đã cấu hình.

## Cấu trúc dự án và quy trình GitHub

- DieuKhien, DuLieuTraoDoi, MoHinh, DichVu và TruyCapDuLieu chứa nghiệp vụ và truy cập dữ liệu.
- Views và wwwroot chứa giao diện, Bootstrap và JavaScript.
- CoSoDuLieu, TaiLieu và KiemThu chứa SQL, tài liệu và kịch bản kiểm thử.
- Tạo nhánh theo công việc từ `codex/nhom-phat-trien`, ví dụ `feature/fr05-checkout`. Mở pull request về nhánh nhóm để Team Lead duyệt và đính kèm mã yêu cầu, mã ca kiểm thử và kết quả chạy.

## Chạy bản demo bằng một lệnh

Trên Windows, từ thư mục gốc chạy `pwsh -File LenhChay/Start-Demo.ps1`. Script dùng SQL Server Express và DB riêng `QuanLySieuThi_Demo_Local`, nạp dữ liệu mẫu và mở ứng dụng ở http://localhost:5233/Auth/Login. Nhập mật khẩu Admin khi được hỏi. Khi khởi động lại, dùng mật khẩu đã tạo lần đầu. Nếu SDK hoặc SQL Server nằm ở vị trí khác, truyền `-Dotnet` hoặc `-SqlInstance` cho script.

## Đổi mật khẩu

Sau khi đăng nhập, chọn **Đổi mật khẩu** ở thanh trên cùng. Nhập mật khẩu hiện tại, mật khẩu mới ít nhất 8 ký tự và xác nhận. Sau khi lưu, đăng nhập lại bằng mật khẩu mới.
