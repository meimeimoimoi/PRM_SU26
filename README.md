# SmartDine - Hệ thống Quản lý và Đặt món thông minh

Hệ thống **SmartDine** bao gồm Backend (Microservices Architecture .NET Core + API Gateway YARP), Web Dashboard dành cho Admin/Quản lý (React/Vite) và Ứng dụng Di động dành cho Khách hàng (Flutter).

---

## Cấu trúc Dự án (Project Structure)

```text
PRM_SU26/
├── BE/                           # Backend (ASP.NET Core 10.0 Microservices)
│   ├── docker/                   # Chứa các tệp Dockerfile quản lý chung
│   │   ├── Gateway.Dockerfile
│   │   ├── Identity.Dockerfile
│   │   ├── Menu.Dockerfile
│   │   ├── Order.Dockerfile
│   │   └── Table.Dockerfile
│   ├── SmartDine.Gateway         # API Gateway (YARP Reverse Proxy - Port 5000)
│   ├── SmartDine.Identity.API    # Auth & Customer Identity Service (Port 5001)
│   ├── SmartDine.Menu.API        # Menu Catalog Service (Port 5002)
│   ├── SmartDine.Order.API       # Order & Realtime Hub Service (Port 5003)
│   ├── SmartDine.Table.API       # Dining Table & Session Service (Port 5004)
│   ├── SmartDine.Application     # Application Logic, Interfaces & DTOs (Shared)
│   ├── SmartDine.Domain          # Entities & Domain Models (Shared)
│   ├── SmartDine.Infrastructure  # Database & Security Persistence (Shared)
│   ├── SmartDine.Tests           # Test suite
│   ├── SmartDine.slnx            # Solution File (Visual Studio 2022 Format)
│   └── run-services.bat          # File chạy đồng thời tất cả các service
└── FE/                           # Frontend
    ├── web-dashboard             # Admin/Staff Dashboard (React + Vite + Ant Design)
    └── customer-mobile           # Customer Mobile App (Flutter + Riverpod)
```

---

## Yêu cầu Hệ thống (Prerequisites)

Trước khi bắt đầu, hãy đảm bảo máy tính của bạn đã cài đặt:
- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) (Khuyên dùng v18+)
- [Flutter SDK](https://docs.flutter.dev/get-started/install) (v3.0.0+)
- [PostgreSQL Database Server](https://www.postgresql.org/)

---

## Hướng dẫn Cài đặt & Khởi chạy (Quick Start Guide)

### 1. Cấu hình Cơ sở dữ liệu (Database Setup)
1. Đảm bảo PostgreSQL Server của bạn đang chạy.
2. Cập nhật chuỗi kết nối PostgreSQL tại mục `ConnectionStrings:DefaultConnection` trong tệp `appsettings.Development.json` của các dự án API (nếu cần thiết):
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=smartdine;Username=postgres;Password=YOUR_PASSWORD"
   }
   ```
3. Cài đặt công cụ Entity Framework Core CLI (nếu chưa cài):
   ```bash
   dotnet tool install --global dotnet-ef
   ```
4. Chạy câu lệnh migration thông qua dự án Identity để tự động tạo cơ sở dữ liệu và bảng:
   ```bash
   dotnet ef database update --project BE/SmartDine.Infrastructure --startup-project BE/SmartDine.Identity.API
   ```

---

### 2. Khởi chạy Backend (Microservices)

Để chạy hệ thống microservices cùng lúc, sử dụng một trong hai cách:

*   **Cách 1: Sử dụng tập lệnh `run-services.bat` (Khuyên dùng trên Windows)**
    Chạy tệp batch tại thư mục `BE/`:
    ```bash
    cd BE
    run-services.bat
    ```
    Script này sẽ tự động mở 5 cửa sổ console độc lập khởi chạy 4 service và 1 API Gateway.

*   **Cách 2: Cấu hình trên Visual Studio 2022**
    1. Nhấp chuột phải vào **Solution 'SmartDine'** $\rightarrow$ chọn **Properties**.
    2. Chọn **Startup Project** $\rightarrow$ **Multiple startup projects**.
    3. Chọn Action là **Start** cho cả 5 dự án: `Gateway`, `Identity.API`, `Menu.API`, `Order.API`, và `Table.API`.
    4. Nhấn **Start (F5)**.

*Các cổng dịch vụ hoạt động ở chế độ Local:*
*   **Gateway**: `http://localhost:5000` (Địa chỉ giao tiếp chính của Frontend)
*   **Identity API**: `http://localhost:5001`
*   **Menu API**: `http://localhost:5002`
*   **Order API**: `http://localhost:5003` (Chứa SignalR Hub tại `/hubs/orders`)
*   **Table API**: `http://localhost:5004`

---

### 3. Khởi chạy Web Dashboard (FE Admin/Staff)
Ứng dụng quản trị viên hiển thị dữ liệu thời gian thực:
```bash
# Di chuyển tới thư mục web-dashboard
cd FE/web-dashboard

# Cài đặt các thư viện dependencies
npm install

# Khởi chạy dev server
npm run dev
```
*Truy cập ứng dụng tại: `http://localhost:5173`.*

---

### 4. Khởi chạy Mobile App (FE Customer App)
Ứng dụng di động Flutter dành cho khách hàng đặt món:
```bash
# Di chuyển tới thư mục customer-mobile
cd FE/customer-mobile

# Tải các gói thư viện Flutter
flutter pub get

# Tạo các code generator cho Hive (nếu cần thiết)
flutter pub run build_runner build --delete-conflicting-outputs

# Khởi chạy ứng dụng (trên máy ảo hoặc thiết bị thật)
flutter run

# Chạy trên web (Chrome) — PHẢI cố định port 8090, vì mã QR do Table.API sinh ra
# trỏ cứng tới http://localhost:8090 (xem CustomerWeb:BaseUrl trong
# appsettings.Development.json của Table.API). Nếu không truyền --web-port, Flutter
# sẽ tự chọn 1 port ngẫu nhiên mỗi lần chạy và mã QR quét vào sẽ sai/không mở được.
flutter run -d chrome --web-port=8090
```
*Nếu chạy bằng nút Run/F5 của VS Code, dùng sẵn config "customer-mobile (Web, port 8090)"
trong `.vscode/launch.json` — đã cố định port 8090 tương tự.*

---

## Triển khai bằng Docker (Docker Deployment)

Hệ thống đã được cấu hình tệp `docker-compose.yml` để tự động xây dựng và khởi chạy cơ sở dữ liệu PostgreSQL cùng 5 dịch vụ backend microservices chỉ với một câu lệnh.

1. **Khởi chạy hệ thống:**
   Đứng tại thư mục gốc (nơi chứa tệp `docker-compose.yml`) và chạy câu lệnh:
   ```bash
   docker-compose up --build
   ```
   *Lưu ý:* Câu lệnh trên sẽ tự động xây dựng Docker images từ các tệp `Dockerfile` của từng microservice, khởi chạy cơ sở dữ liệu PostgreSQL, tự động thực hiện migrations/seeding dữ liệu khởi tạo, và bật API Gateway trên cổng `5000`.

2. **Dừng hệ thống:**
   ```bash
   docker-compose down
   ```

---

## Thông tin Bảo mật & Môi trường
- **JWT Authentication**: Cấu hình khóa bí mật tại `Jwt:SecretKey` trong các tệp `appsettings.json` của các dự án API. Vui lòng đổi khóa này khi triển khai thực tế (Production).
