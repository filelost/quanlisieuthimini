# Nhật ký nâng cấp – 10/11/2026

## Mục tiêu
Giữ nguyên kiến trúc ASP.NET Core MVC, EF Core và SQL Server; nâng cấp luồng POS, thanh toán tiền mặt và in hóa đơn.

## Nội dung thay đổi
- Quầy bán hàng hiển thị tạm tính, giảm giá, tổng thanh toán và tiền thừa theo dữ liệu giỏ hàng.
- Khi thanh toán tiền mặt, thu ngân nhập số tiền khách đưa. Máy chủ tính lại tổng tiền và từ chối nếu số tiền nhận nhỏ hơn số phải trả, vượt giới hạn hoặc có quá hai chữ số thập phân.
- Lưu `CashReceived` và `ChangeAmount` trong `SalesInvoices`; cấu hình precision và check constraint ở EF Core.
- Bổ sung SQL schema cho database mới và script nâng cấp lặp an toàn `08_AddCashPaymentAndReceipt.sql` cho database hiện có.
- Script `Start-Demo.ps1` tự áp dụng script nâng cấp mới.
- Bổ sung trang in hóa đơn riêng, tối ưu khổ giấy nhiệt 80 mm; có thể in lại hóa đơn đã hoàn tất từ trang chi tiết hoặc danh sách hóa đơn.
- Không cho in hóa đơn chưa thanh toán thành công.
- Tương thích API checkout cũ: nếu API không gửi `CashReceived`, hệ thống giả định khách trả đúng số tiền phải thanh toán.
- Cập nhật hướng dẫn và tài liệu thay đổi.

## Lưu ý dữ liệu cũ
Phiên bản cũ không lưu số tiền khách thực tế đưa. Script nâng cấp gán `CashReceived = TotalAmount` và `ChangeAmount = 0` cho hóa đơn cũ để dữ liệu lịch sử hiển thị nhất quán; đây là giá trị mặc định suy ra, không phải số tiền thực nhận đã được ghi nhận trước đó.

## Kiểm tra thực hiện
- JavaScript `wwwroot/js/checkout.js`: kiểm tra cú pháp bằng `node --check` – đạt.
- `QuanLySieuThi.csproj`: kiểm tra cấu trúc XML – đạt.
- Không thể chạy `dotnet build` trong môi trường đóng gói hiện tại vì không có .NET SDK. Cần chạy build và kiểm thử tích hợp trên máy Windows có .NET 9 SDK và SQL Server trước khi triển khai thực tế.
