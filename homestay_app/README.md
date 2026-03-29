# Homestay App - README

## Tổng quan

Ứng dụng Homestay là một hệ thống quản lý và đặt phòng homestay với các chức năng cơ bản, CRUD, và nâng cao, phục vụ cả người dùng và quản trị viên. Dưới đây là mô tả chi tiết về cách thức hoạt động của từng nhóm chức năng.

---

## 1. Chức năng cơ bản

### 1.1 Đăng ký/Đăng nhập
- Người dùng có thể đăng ký tài khoản mới hoặc đăng nhập bằng tài khoản đã có.
- Xác thực thông tin qua email hoặc số điện thoại.

### 1.2 Phân quyền trên các ROLE
- Giao diện và chức năng hiển thị tùy theo vai trò (User, Host, Admin).
- Admin có thể quản lý người dùng, Host có thể quản lý homestay của mình.

### 1.3 Quản lý người dùng
- Admin có thể thêm, sửa, xóa người dùng.
- Host có thể nâng cấp tài khoản từ User lên Host.

### 1.4 Thêm/Xóa/Sửa Homestay
- Host có thể thêm mới, chỉnh sửa hoặc xóa homestay của mình.

### 1.5 Giỏ hàng
- Người dùng thêm homestay vào giỏ hàng cá nhân để đặt phòng sau.

### 1.6 Đặt phòng Homestay
- Người dùng thực hiện booking homestay, chọn ngày, số lượng phòng, thanh toán.

### 1.7 Thanh toán đặt cọc
- Người dùng thanh toán tiền cọc cho homestay khi đặt phòng.

### 1.8 Đánh giá Homestay
- Sau khi ở, người dùng có thể đánh giá và nhận xét về homestay.

### 1.9 Tìm kiếm Homestay
- Người dùng tìm kiếm homestay theo vị trí, giá, tiện nghi, đánh giá...

### 1.10 UI/UX
- Giao diện thân thiện, dễ sử dụng, tối ưu cho trải nghiệm người dùng.

---

## 2. Chức năng CRUD

### 2.1 Homestay
- Lưu trữ thông tin chi tiết về từng homestay (tên, địa chỉ, mô tả, hình ảnh, giá...)

### 2.2 User
- Lưu trữ thông tin tài khoản người dùng (họ tên, email, số điện thoại, vai trò...)

### 2.3 Đơn đặt phòng
- Lưu trữ thông tin các đơn đặt phòng, trạng thái, lịch sử đặt phòng.

### 2.4 Khuyến mãi
- Lưu trữ và quản lý các chương trình khuyến mãi, mã giảm giá.

### 2.5 Images
- Lưu trữ hình ảnh homestay, người dùng, hóa đơn...

### 2.6 Tin nhắn
- Lưu trữ các tin nhắn giữa người dùng với nhau hoặc với chủ homestay.

### 2.7 Cuộc trò chuyện
- Lưu trữ lịch sử các cuộc trò chuyện, hỗ trợ AI chat.

### 2.8 Thông báo
- Lưu trữ và gửi thông báo cho người dùng về trạng thái đặt phòng, khuyến mãi...

### 2.9 Tiện nghi
- Lưu trữ thông tin về các tiện nghi của homestay (wifi, bếp, máy lạnh...)

### 2.10 Ngày đóng cửa
- Lưu trữ các ngày homestay không nhận khách.

### 2.11 Giá Homestay
- Lưu trữ giá từng homestay, cập nhật theo mùa, ngày lễ...

### 2.12 Phương thức thanh toán
- Lưu trữ các phương thức thanh toán (chuyển khoản, ví điện tử, tiền mặt...)

---

## 3. Chức năng nâng cao

### 3.1 Nhúng AI (Chatbot Gemini)
- Tích hợp chatbot AI để hỗ trợ khách hàng, trả lời câu hỏi, tư vấn đặt phòng.

### 3.2 Lấy thông tin từ CCCD
- Cho phép quét/chụp CCCD để tự động điền thông tin cá nhân khi đăng ký.

### 3.3 Face ID/Vân tay
- Xác thực sinh trắc học khi đăng nhập hoặc xác nhận thanh toán.

### 3.4 Chuyển đổi ngôn ngữ
- Hỗ trợ chuyển đổi giao diện và nội dung giữa các ngôn ngữ (Việt/Anh).

### 3.5 Text to Speech/Speech to Text
- Đọc nội dung bằng giọng nói và nhận diện giọng nói để nhập liệu.

### 3.6 Bảo mật API thời tiết
- Tích hợp API thời tiết, bảo vệ API key, hiển thị thời tiết tại homestay.

### 3.7 Đặt nhiều lớp
- Cho phép người dùng đặt nhiều homestay cùng lúc, quản lý nhiều đơn đặt phòng.

### 3.8 Xem video Youtube ngay trên App
- Tích hợp xem video Youtube giới thiệu homestay trực tiếp trên ứng dụng.

### 3.9 Có tích hợp bản đồ
- Hiển thị vị trí homestay trên bản đồ, chỉ đường, tìm kiếm lân cận.

---

## 4. Điểm nổi bật
- Hệ thống phân quyền rõ ràng, bảo mật.
- Tích hợp AI và các công nghệ mới (Face ID, Speech, Map, Youtube...)
- Giao diện hiện đại, thân thiện, tối ưu trải nghiệm người dùng.

---

## 5. Hướng dẫn sử dụng
1. Đăng ký tài khoản và đăng nhập.
2. Tìm kiếm homestay phù hợp.
3. Thêm vào giỏ hàng hoặc đặt phòng trực tiếp.
4. Thanh toán và nhận xác nhận.
5. Đánh giá sau khi sử dụng dịch vụ.
6. Sử dụng các chức năng nâng cao như AI chat, bản đồ, video, v.v.

---

## 6. Liên hệ hỗ trợ
- Email: support@homestaybooking.vn
- Hotline: 1900-xxxxxx
- Fanpage: facebook.com/homestaybooking

---

# 7. Hướng dẫn luồng xử lý code các chức năng chính

## 7.1 Đăng ký/Đăng nhập
- **Frontend:**
  - Giao diện nhập thông tin (email, mật khẩu, xác thực OTP).
  - Gửi request qua API `/api/auth/login` hoặc `/api/auth/register`.
  - Nhận token, lưu vào local storage/shared preferences.
- **Backend:**
  - Controller nhận request, xác thực thông tin.
  - Nếu hợp lệ, sinh JWT token trả về frontend.
  - Lưu thông tin đăng nhập, cập nhật trạng thái user.

## 7.2 Phân quyền ROLE
- **Frontend:**
  - Sau khi đăng nhập, kiểm tra role từ token/user info.
  - Hiển thị menu, chức năng phù hợp (User, Host, Admin).
- **Backend:**
  - Middleware kiểm tra quyền truy cập API.
  - Chỉ cho phép truy cập các API phù hợp với từng role.

## 7.3 Đặt phòng Homestay
- **Frontend:**
  - Người dùng chọn homestay, ngày, số lượng phòng.
  - Gửi request đặt phòng qua API `/api/bookings`.
  - Hiển thị trạng thái đặt phòng, thông báo thành công/thất bại.
- **Backend:**
  - Controller nhận request, kiểm tra phòng trống.
  - Nếu hợp lệ, tạo booking, trừ phòng, gửi thông báo.
  - Lưu lịch sử đặt phòng vào database.

## 7.4 Đánh giá Homestay
- **Frontend:**
  - Sau khi hoàn thành chuyến đi, hiển thị form đánh giá.
  - Gửi đánh giá qua API `/api/reviews`.
- **Backend:**
  - Nhận và lưu đánh giá vào bảng reviews.
  - Tính điểm trung bình, cập nhật vào homestay.

## 7.5 Chat AI (Gemini)
- **Frontend:**
  - Giao diện chat, nhập câu hỏi.
  - Gửi message qua API `/api/ai/chat`.
  - Hiển thị phản hồi AI trả về.
- **Backend:**
  - Nhận message, gọi API Gemini hoặc AI nội bộ.
  - Xử lý, trả về câu trả lời phù hợp.
  - Lưu lịch sử chat vào database.

## 7.6 Quản lý CRUD (Homestay, User, Booking...)
- **Frontend:**
  - Giao diện danh sách, thêm, sửa, xóa.
  - Gửi request qua các API tương ứng (`/api/homestays`, `/api/users`, ...).
- **Backend:**
  - Controller nhận request, xác thực, thao tác với database.
  - Trả về kết quả (danh sách, chi tiết, trạng thái thành công/thất bại).

## 7.7 Tích hợp bản đồ, Youtube, API thời tiết
- **Frontend:**
  - Sử dụng Google Maps/Youtube API để hiển thị bản đồ, video.
  - Gọi API thời tiết, hiển thị thông tin tại homestay.
- **Backend:**
  - (Nếu cần) Proxy các request API, bảo vệ API key.

## 7.8 Xác thực sinh trắc học (Face ID/Vân tay)
- **Frontend:**
  - Sử dụng package Flutter hỗ trợ Face ID/Vân tay.
  - Khi đăng nhập/thanh toán, gọi hàm xác thực sinh trắc học.
- **Backend:**
  - Không xử lý, xác thực thực hiện trên thiết bị người dùng.

---

# 8. Ví dụ chi tiết: Cấu trúc file và vai trò từng file cho một chức năng

## 8.1 Chức năng: Đặt phòng Homestay

### 1. Frontend (Flutter)
- **lib/screens/booking/booking_screen.dart**
  - Giao diện chọn homestay, ngày, số lượng phòng, nhập thông tin đặt phòng.
  - Gọi hàm đặt phòng khi người dùng nhấn nút xác nhận.
- **lib/services/booking_service.dart**
  - Chứa các hàm gọi API backend (POST, GET booking).
  - Xử lý dữ liệu trả về, thông báo lỗi/thành công.
- **lib/models/booking.dart**
  - Định nghĩa model Booking (id, userId, homestayId, ngày, trạng thái...)
- **lib/providers/booking_provider.dart**
  - Quản lý trạng thái đặt phòng, danh sách booking của user.
  - Lắng nghe thay đổi và cập nhật UI khi có booking mới.

### 2. Backend (.NET)
- **Controllers/BookingsController.cs**
  - Nhận request đặt phòng từ frontend (POST /api/bookings).
  - Kiểm tra phòng trống, xác thực user, tạo booking mới.
  - Trả về kết quả (thành công/thất bại, chi tiết booking).
- **Models/Booking.cs**
  - Định nghĩa entity Booking, ánh xạ với bảng Booking trong database.
- **Services/BookingService.cs**
  - Chứa logic kiểm tra phòng trống, tạo booking, gửi thông báo.
- **Data/ApplicationDbContext.cs**
  - Quản lý truy vấn, lưu booking vào database.
- **Migrations/**
  - Chứa các file migration tạo bảng Booking trong database.

### 3. Luồng hoạt động tổng quát
1. Người dùng thao tác trên `booking_screen.dart` → nhập thông tin đặt phòng.
2. Gọi hàm trong `booking_service.dart` để gửi request tới API.
3. API `/api/bookings` được xử lý bởi `BookingsController.cs`.
4. Controller gọi `BookingService.cs` để kiểm tra logic và lưu vào DB qua `ApplicationDbContext.cs`.
5. Kết quả trả về frontend, cập nhật trạng thái qua `booking_provider.dart` và hiển thị trên UI.

---

> Bạn có thể áp dụng cấu trúc này cho các chức năng khác như đánh giá, chat, quản lý user... bằng cách thay đổi tên file và logic tương ứng.
