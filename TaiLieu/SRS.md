# SRS — Hệ thống quản lý siêu thị mini

Phạm vi là vận hành một cửa hàng, một kho, tiền VND và dữ liệu lưu trong SQL Server. Khách lẻ có thể mua mà không cần hồ sơ. Admin, Cashier và StockStaff là người dùng nội bộ. Vai trò Customer hiện chỉ có màn hình tài khoản; tích điểm và mua trực tuyến nằm ngoài phạm vi nghiệm thu.

| Mã | Yêu cầu và tiêu chí chấp nhận |
|---|---|
| FR01 | Người dùng đăng nhập bằng tài khoản đang hoạt động. Admin tạo và phân vai nhân viên. API trả 401 khi chưa đăng nhập, 403 khi sai vai trò. |
| FR02 | Admin tạo, sửa, tìm và ngừng sử dụng sản phẩm, nhà cung cấp. SKU duy nhất. |
| FR03 | Admin hoặc StockStaff lập phiếu nhập, thêm dòng và nhận từng phần. Nhận vượt số đặt phải bị từ chối. |
| FR04 | Admin hoặc StockStaff tra tồn và lịch sử. Điều chỉnh tồn chỉ dành cho Admin, phải có lý do. Tồn không âm. |
| FR05 | Admin hoặc Cashier quản lý hồ sơ khách hàng và lập hóa đơn. Giá lấy từ DB, cập nhật hóa đơn và tồn cùng giao dịch. Yêu cầu lặp dùng cùng mã không được trừ tồn lần hai. |
| FR06 | Thu tiền mặt. Nếu cấu hình MoMo, ghi nhận trạng thái chờ và xác minh kết quả. Chỉ Admin được hủy hóa đơn đã thanh toán, kèm tham chiếu hoàn tiền. |
| FR07 | Admin xem doanh thu hóa đơn hoàn tất theo ngày UTC, số hóa đơn và số mặt hàng dưới ngưỡng. |
| NFR01 | Mật khẩu băm, cookie HttpOnly, biểu mẫu ghi dữ liệu có CSRF token. Chuỗi kết nối và mật khẩu bootstrap qua môi trường. |
| NFR02 | Build Release và chạy kiểm thử trên SQL Server riêng. Log thời gian và kết quả từng ca; lỗi phải truy được về mã yêu cầu. |

Ràng buộc dữ liệu gồm khóa chính và khóa ngoại, tồn không âm, số lượng tối đa ba chữ số thập phân và tổng tiền do máy chủ tính. Chứng từ giữ giá bán tại thời điểm giao dịch. Nhật ký kho truy theo loại và mã chứng từ.
