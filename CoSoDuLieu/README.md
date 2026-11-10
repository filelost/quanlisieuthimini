# Khởi tạo tài khoản quản trị

Ứng dụng đọc chuỗi kết nối SQL Server từ biến môi trường `ConnectionStrings__DefaultConnection`. Xem hướng dẫn tại [README chính](../README.md).

1. Database `QuanLySieuThiDb` có thể được tạo bằng `01_CreateDatabase.sql`.
2. Cập nhật database cũ sang các vai trò `Admin`, `Cashier`, `StockStaff`, `Customer` bằng `02_UpdateRoles.sql`. Ứng dụng cũng chuẩn hóa các role cũ khi khởi động.
3. Đặt hai biến môi trường `BootstrapAdmin__Username` và `BootstrapAdmin__Password` trước lần chạy đầu tiên cần tạo tài khoản Admin. Ứng dụng chỉ tạo tài khoản ban đầu nếu chưa có người dùng nào mang role Admin; mật khẩu được băm bằng `PasswordHasher` và không ghi vào file cấu hình.

Chạy `05_AddProductImages.sql` trên cơ sở dữ liệu đã tạo trước khi có tính năng ảnh sản phẩm. Script demo tự thực hiện bước này.

Chạy `06_AddPurchaseShortageActions.sql` để bổ sung lịch sử xử lý hàng nhập thiếu cho cơ sở dữ liệu đang sử dụng. Script demo tự thực hiện bước này.

Chạy `07_AddWarehouseAudit.sql` để bổ sung thời điểm lập phiếu, người thêm dòng hàng và mã chứng từ giao hàng cho cơ sở dữ liệu đang sử dụng. Script demo tự thực hiện bước này. Các phiếu cũ thiếu thông tin sẽ hiển thị “Chưa ghi nhận”.

PowerShell:

```powershell
$env:BootstrapAdmin__Username = "admin"
$env:BootstrapAdmin__Password = "thay-bang-mat-khau-rieng"
dotnet run
Remove-Item Env:BootstrapAdmin__Password
```

Thay mật khẩu mẫu bằng một mật khẩu riêng trước khi chạy. Sau khi tạo Admin, có thể xóa cả hai biến khỏi phiên PowerShell. Trang đăng nhập: `/Auth/Login`.

Khi chạy lần đầu, ứng dụng chuẩn hóa role và thêm một số danh mục mẫu nếu bảng `Categories` đang rỗng. Các tài khoản nhân viên được tạo trong màn hình **Nhân viên và tài khoản**; quản trị viên chọn vai trò bán hàng hoặc kho. Tài khoản cũ mang vai trò Employee cần được phân vai trên cùng màn hình để truy cập nghiệp vụ.

## Thanh toán MoMo

Đặt các biến môi trường `MoMo__PartnerCode`, `MoMo__AccessKey`, `MoMo__SecretKey` và `MoMo__PublicUrl`. URL công khai phải dùng HTTPS và định tuyến được đến ứng dụng. MoMo gửi kết quả đến `/payments/momo/ipn`. Mặc định ứng dụng dùng môi trường thử nghiệm. Đặt `MoMo__Production=true` cùng bộ thông tin merchant sản xuất khi triển khai thật.

Hóa đơn MoMo chờ thanh toán giữ số lượng hàng trong tồn khả dụng. Mở thanh toán hoặc đối soát từ trang chi tiết hóa đơn. Hệ thống hoàn tồn nếu MoMo xác nhận giao dịch thất bại. Khi cần hủy hóa đơn đã thanh toán, quản lý hoàn tiền qua kênh thanh toán rồi nhập mã hoặc chứng từ hoàn tiền trên hóa đơn để xác nhận hủy và hoàn tồn.

## Khuyến mãi hóa đơn

Mỗi hóa đơn chọn tối đa một mã được cấu hình. Ví dụ cấu hình giảm 10% và giảm cố định 5.000 ₫ qua biến môi trường:

```powershell
$env:Promotions__GIAM10__Type = "Percent"
$env:Promotions__GIAM10__Value = "10"
$env:Promotions__GIAM5000__Type = "Fixed"
$env:Promotions__GIAM5000__Value = "5000"
```

Chạy `08_AddCashPaymentAndReceipt.sql` trên cơ sở dữ liệu đang sử dụng để lưu số tiền khách đưa và tiền thừa trên hóa đơn. Script có thể chạy lặp lại an toàn. Cơ sở dữ liệu mới đã có sẵn hai cột này trong `01_CreateDatabase.sql`.
```
