# SmartDine - Hệ thống Đặt món Nhà hàng

API backend cho ứng dụng đặt món nhà hàng, xây dựng theo **Clean Architecture** 

---

## Cấu trúc dự án

```
SmartDine.slnx
│
├── SmartDine.Domain          → Lõi trung tâm (Entity, Interface)
├── SmartDine.Application     → Xử lý nghiệp vụ (Service, DTO)
├── SmartDine.Infrastructure  → Hạ tầng (Lưu trữ dữ liệu giả trong RAM)
└── SmartDine.API             → API Controller + Dependency Injection
```

### Luồng phụ thuộc

```
API → Application → Domain ← Infrastructure
```

> Domain **không phụ thuộc** bất kỳ tầng nào khác.

---

## Cách chạy

```bash
# Build toàn bộ solution
dotnet build SmartDine.slnx

# Chạy API
dotnet run --project SmartDine.API
```

Sau khi chạy, mở Swagger UI để test: **http://localhost:5264/swagger**

---

## Danh sách API

| Method | Endpoint           | Mô tả                   |
|--------|----------------    |-------------------------|
| `GET`  | `/api/orders/menu` | Xem thực đơn            |
| `POST` | `/api/orders`      | Đặt món                 |
| `GET`  | `/api/orders`      | Xem tất cả đơn          |
| `GET`  | `/api/orders/{id}` | Xem đơn hàng theo Id    |

### Ví dụ đặt món (POST `/api/orders`)

**Request body:**
```json
{
  "menuItemIds": [
    "11111111-1111-1111-1111-111111111111",
    "55555555-5555-5555-5555-555555555555"
  ]
}
```

**Response:**
```json
{
  "id": "fa93e367-...",
  "items": [
    { "id": "111...", "name": "Phở Bò", "price": 55000 },
    { "id": "555...", "name": "Trà Đá", "price": 5000 }
  ],
  "totalAmount": 60000,
  "status": "PENDING",
  "createdAt": "2026-05-27T17:44:13Z"
}
```

---

## Giải thích từng tầng

### 1. Domain — Lõi trung tâm
- **Entities**: `Order`, `MenuItem` — class thuần, không phụ thuộc thư viện.
- **Enums**: `OrderStatus` — PENDING, COOKING, COMPLETED.
- **Interfaces**: `IOrderRepository` — bản vẽ quy định hàm `Add()`, `GetById()`, `GetAll()`.

### 2. Application — Nghiệp vụ
- **DTOs**: `PlaceOrderRequest` (nhận từ client), `OrderResponse` (trả về client).
- **Services**: `OrderService` — tìm món → tính tiền → tạo đơn → lưu.
- **Constants**: `ValidationMessages` — các thông báo lỗi dùng chung.

### 3. Infrastructure — Hạ tầng
- `InMemoryOrderRepository` — implement `IOrderRepository`, dùng `List<Order>` lưu tạm trong RAM.
- Sau này có thể thay bằng EF Core + SQL Server mà **không cần sửa code tầng trên**.

### 4. API — Cổng giao tiếp
- `OrdersController` — định nghĩa các endpoint REST.
- `Program.cs` — đăng ký Dependency Injection (DI):
  ```csharp
  builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
  builder.Services.AddScoped<OrderService>();
  ```
