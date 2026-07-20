# Phase 1: Project Introduction (I)

## 1. Overview

### 1.1 Project Information

| Thông tin | Chi tiết |
|-----------|----------|
| **Project Name** | SmartDine - Hệ thống Quản lý và Đặt món thông minh |
| **Project Code** | PRM_SU26 |
| **Group Name** | [Tên nhóm] |
| **Supervisor** | [Tên giảng viên hướng dẫn] |
| **Ext Supervisor** | [Tên người hướng dẫn bên ngoài (nếu có)] |
| **Capstone Project Code** | [Mã đề tài] |
| **Duration** | [Thời gian thực hiện] |
| **Location** | FPT University, Ho Chi Minh City |

### 1.2 Project Team

| Member | Role | Responsibility |
|--------|------|----------------|
| [Tên 1] | Project Manager / Backend Lead | Quản lý dự án, thiết kế kiến trúc microservices, phát triển Backend |
| [Tên 2] | Frontend Lead | Phát triển Web Dashboard (React + Ant Design) |
| [Tên 3] | Mobile Lead | Phát triển Customer Mobile App (Flutter + Riverpod) |
| [Tên 4] | Backend Developer | Phát triển Backend services, Robot integration |
| [Tên 5] | QA / DevOps | Kiểm thử, CI/CD, Docker deployment |

---

## 2. Product Background

### Bối cảnh
Ngành công nghiệp nhà hàng tại Việt Nam đang trải qua quá trình chuyển đổi số mạnh mẽ. Với sự phát triển của công nghệ di động và thanh toán trực tuyến, khách hàng ngày càng mong đợi trải nghiệm dining tiện lợi, nhanh chóng và cá nhân hóa.

### Vấn đề hiện tại
- **Quy trình đặt món thủ công**: Nhân viên phải ghi chép lệnh bằng tay, dễ gây sai sót
- **Quản lý bàn không hiệu quả**: Không có hệ thống theo dõi thời gian thực trạng thái bàn
- **Thanh toán phức tạp**: Khách hàng phải chờ đợi thanh toán, thiếu đa dạng phương thức
- **Thiếu dữ liệu phân tích**: Nhà quản lý không có công cụ phân tích doanh thu, hiệu suất menu
- **Trải nghiệm khách hàng chưa tốt**: Không có hệ thống khuyến mãi, tích điểm khách hàng thân thiết

### Khách hàng / Người yêu cầu dự án
- **Target Users**: Nhà hàng, quán ăn, quán café tại Việt Nam
- **End Users**: Khách hàng dining, nhân viên phục vụ, đầu bếp, quản lý nhà hàng

---

## 3. Existing Systems

### Các hệ thống hiện có trên thị trường

| Hệ thống | Ưu điểm | Nhược điểm |
|----------|---------|------------|
| **GrabFood** | Giao diện thân thiện, thanh toán đa dạng | Chỉ dành cho giao hàng, không hỗ trợ dine-in ordering |
| **NowFood** | Phổ biến tại Việt Nam | Thiếu tính năng quản lý nhà hàng, không tích hợp robot |
| **POS Systems (Square, Toast)** | Quản lý bán hàng hiệu quả | Không hỗ trợ đặt món qua QR, thiếu AI recommendation |
| **Foxy.vn** | Hỗ trợ đặt món QR code | Giao diện cũ, thiếu tính năng realtime, không có robot delivery |

### Tham khảo hệ thống tương tự
- **Eats365**: Hệ thống POS nhà hàng với đặt món qua QR code
- **Tabelog**: Nền tảng đặt bàn và đánh giá nhà hàng Nhật Bản

---

## 4. Business Opportunity

### Thị trường
- Thị trường F&B Việt Nam đạt ~200 tỷ USD (2025), tăng trưởng 15-20%/năm
- Hơn 500,000 nhà hàng, quán ăn tại Việt Nam
- Tỷ lệ sử dụng di động: 70% dân số sử dụng smartphone
- Thanh toán di động: 40% giao dịch F&B qua ví điện tử

### Vấn đề không thể giải quyết nếu thiếu SmartDine
1. **Đặt món real-time**: Khách hàng quét QR code, chọn món, thanh toán trực tiếp trên điện thoại
2. **Quản lý tập trung**: Dashboard theo dõi trạng thái bàn, đơn hàng, nhân viên theo thời gian thực
3. **Robot giao thức ăn**: Tự động hóa việc giao thức ăn từ bếp đến bàn, giảm tải cho nhân viên
4. **AI Recommendation**: Gợi ý món ăn phù hợp dựa trên lịch sử đặt hàng và sở thích
5. **Loyalty Program**: Hệ thống tích điểm, khuyến mãi tự động theo hạng thành viên

---

## 5. Software Product Vision

### Vision Statement
> **SmartDine** sẽ trở thành nền tảng quản lý nhà hàng thông minh hàng đầu tại Việt Nam, kết hợp công nghệ AI, IoT (robot giao thức) và thanh toán trực tuyến để mang đến trải nghiệm dining hiện đại, tiện lợi và cá nhân hóa cho mọi khách hàng.

### Mục tiêu sản phẩm
- **Đối với khách hàng**: Trải nghiệm đặt món nhanh chóng, thanh toán đa dạng, nhận gợi ý món ăn thông minh
- **Đối với nhân viên**: Quản lý đơn hàng real-time, giảm tải công việc thủ công
- **Đối với quản lý**: Dashboard phân tích doanh thu, hiệu suất menu, quản lý nhân viên tập trung
- **Đối với nhà đầu tư**: Chi phí vận hành thấp hơn 30% so với quy trình thủ công truyền thống

---

## 6. Project Scope & Limitations

### Phạm vi dự án (In Scope)

| Module | Chức năng chính |
|--------|-----------------|
| **Authentication & Identity** | Đăng nhập/đăng ký, JWT, phân quyền (Manager/Staff/Chef/Customer/Guest) |
| **Menu Management** | CRUD menu, danh mục, tìm kiếm, phân trang |
| **Order Management** | Đặt món, trạng thái đơn hàng, tracking real-time (SignalR) |
| **Payment System** | PayOS integration, nhiều phương thức (Cash, VNPay, MoMo, QR, Credit Card) |
| **Table Management** | Quản lý bàn, mã QR, dining session, reservation |
| **Robot Delivery** | Điều khiển robot giao thức ăn qua Webots simulator, DWA path planner |
| **AI Recommendation** | Gợi ý menu cá nhân hóa qua Ollama LLM (qwen2.5:1.5b) |
| **Loyalty Program** | Tích điểm, hạng thành viên (Bronze/Silver/Gold/Platinum), khuyến mãi |
| **Web Dashboard** | Quản lý bàn, menu, nhân viên, giao dịch, settings |
| **Customer Mobile App** | Đặt món, thanh toán, theo dõi đơn, QR scanning |

### Hạn chế (Out of Scope)
- Không phát triển ứng dụng native iOS/Android (chỉ Flutter Web + APK)
- Không tích hợp hệ thống kế toán bên ngoài (QuickBooks, Sage)
- Không hỗ trợ đa ngôn ngữ (chỉ Tiếng Việt)
- Không tích hợp hệ thống đặt bàn bên thứ ba (OpenTable)
- Không phát triển hệ thống quản lý kho (inventory management) đầy đủ
- Robot chỉ hoạt động trong môi trường mô phỏng Webots, chưa deploy thực tế

### Giả định & Điều kiện tiên quyết
- Hệ thống hoạt động trên môi trường Docker (local) hoặc Render (production)
- PostgreSQL database được cấu hình đúng
- Internet connection cho thanh toán PayOS và AI recommendation
- Webots simulator được cài đặt cho robot navigation
