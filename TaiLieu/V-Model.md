# Kế hoạch V-Model

| Cổng thiết kế | Cổng kiểm chứng tương ứng | Phụ trách |
|---|---|---|
| Chốt SRS, vai trò và quy tắc nghiệp vụ | Nghiệm thu các luồng theo vai trò | TV1 Team Lead, TV2 phân tích |
| Thiết kế MVC, Web API và xác thực | Kiểm thử HTTP, phân quyền và giao diện | TV3 kiến trúc/API, TV7 frontend |
| Thiết kế ERD, SQL và ràng buộc | Khởi tạo DB riêng, kiểm thử tích hợp và đối chiếu tồn kho | TV4 dữ liệu |
| Thiết kế bán hàng và nhập kho | Kiểm tra tiền, số lượng, trạng thái và rollback | TV5 bán hàng, TV6 kho |
| Tích hợp và phát hành | Build Release, chạy bộ smoke test và ghi biên bản | TV8 QA, TV1 duyệt |

Thứ tự triển khai là SRS, sơ đồ, dữ liệu, mã, build, kiểm thử tích hợp và nghiệm thu. Mỗi PR ghi mã yêu cầu, ca kiểm thử và kết quả. Team Lead quản lý phạm vi, duyệt thay đổi liên mô-đun và phân công lỗi.
