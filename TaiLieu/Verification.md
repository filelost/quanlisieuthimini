# Biên bản kiểm chứng

Môi trường Windows, .NET SDK 10.0.403 và SQL Server Express. Schema và dữ liệu demo đã chạy trên DB QA riêng. Build Release đạt 0 lỗi, 0 cảnh báo. Seed chạy hai lần trên cùng DB, lần hai không thêm bản ghi. Các ca HTTP gọi Kestrel và EF Core thật. Các giá trị kiểm tra tồn được đọc lại qua API.

| Ca | Yêu cầu | Kịch bản và kết quả mong đợi | Kết quả |
|---|---|---|---|
| TC01 | FR01 | Gọi API không cookie → 401 | Pass |
| TC02 | FR02 | Tìm DEMO-001 → đúng SKU và giá 7.000 | Pass |
| TC03 | FR05 | Mở POS → HTML có hàng demo từ DB | Pass |
| TC04 | FR05–06 | Bán 2 chai tiền mặt → chuyển đến hóa đơn | Pass |
| TC05 | FR05 | Gửi lại mã yêu cầu → cùng hóa đơn, tồn chỉ giảm 2 | Pass |
| TC06 | FR04–05 | Bán vượt tồn → không tạo hóa đơn, tồn giữ nguyên | Pass |
| TC07 | FR07 | Gọi báo cáo ngày UTC → có doanh thu; giao diện lọc ngày hiển thị 30 dòng theo ngày, 6 dòng theo tuần và 1 dòng theo tháng cho 11/2026 | Pass |
| TC08 | FR07 | Từ ngày sau đến ngày → 400 | Pass |
| TC09 | FR01 | Admin tạo Cashier và tài khoản mới đăng nhập được | Pass |
| TC10 | FR01 | Cashier xem sản phẩm được, tồn kho → 403 | Pass |
| TC11 | NFR01 | POST Checkout thiếu CSRF token → 400 | Pass |
| TC12 | FR03 | Chọn nhà cung cấp, tạo phiếu → trang chi tiết | Pass |
| TC13 | FR03 | Thêm mặt hàng → có dòng phiếu | Pass |
| TC14 | FR03–04 | Nhận 2/5 → tồn tăng 2 | Pass |
| TC15 | FR03–04 | Nhận 4 khi còn 3 → tồn giữ nguyên | Pass |
| TC16 | FR03–04 | Nhận 3 còn lại → tồn tăng 3 | Pass |
| TC17 | FR06 | MoMo sandbox, callback, đối soát, hủy và hoàn tồn | Chưa chạy |
| TC18 | FR02–04 | CRUD đầy đủ, điều chỉnh kho, StockStaff trên trình duyệt | Chưa chạy |
| TC19 | NFR02 | Build Release, pull request và CI trên GitHub | Chưa xác minh trên GitHub |
| TC20 | NFR01–02 | Hai yêu cầu đồng thời, HTTPS triển khai và tải | Chưa chạy |
| TC21 | NFR01 | JSON checkout thiếu CSRF token → 400 | Pass |
| TC22 | FR05–06 | JSON checkout tiền mặt, replay cùng hóa đơn và chỉ trừ tồn một lần | Pass |
| TC23 | FR07 | Mở chi tiết kỳ báo cáo → danh sách hóa đơn, trạng thái rỗng rõ ràng, điều khiển phân trang và liên kết xem hóa đơn | Pass |

Trang báo cáo được kiểm tra qua HTTP, API và tương tác trình duyệt cho các chế độ ngày, tuần, tháng cùng khoảng ngày tùy chọn; giao diện chi tiết có trạng thái rỗng, điều khiển phân trang và các dòng hàng mở rộng theo từng hóa đơn. Customer Portal hiện là giao diện mẫu, chưa liên kết hóa đơn hoặc tích điểm.

Postman Collection chạy qua Newman trên cùng ứng dụng QA. Kết quả 9 request, 12 assertion, 0 lỗi.
