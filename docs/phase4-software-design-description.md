# Phase 4: Software Design Description (IV)

## 1. System Design

### 1.1 System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Flutter Mobile│  │ React Web   │  │ Webots Robot │          │
│  │ (Customer App)│  │ (Dashboard) │  │ (Simulator)  │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
└─────────┼─────────────────┼─────────────────┼───────────────────┘
          │                 │                 │
          ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────────────────┐
│              API GATEWAY (YARP Reverse Proxy, port 5000)         │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ /api/v1/auth/*    → Identity API (:5001)                │   │
│  │ /api/v1/menu/*    → Menu API (:5002)                    │   │
│  │ /api/v1/orders/*  → Order API (:5003)                   │   │
│  │ /api/v1/payments/*→ Order API (:5003)                   │   │
│  │ /api/v1/tables/*  → Table API (:5004)                   │   │
│  │ /api/v1/ai/*      → AI API (:5005)                      │   │
│  │ /hubs/orders      → SignalR OrderHub (WebSocket)        │   │
│  │ /hubs/robot       → SignalR RobotHub (WebSocket)        │   │
│  └─────────────────────────────────────────────────────────┘   │
└────┬─────────┬─────────┬─────────┬─────────┬───────────────────┘
     │         │         │         │         │
     ▼         ▼         ▼         ▼         ▼
┌─────────┐┌─────────┐┌─────────┐┌─────────┐┌─────────┐
│Identity ││  Menu   ││  Order  ││  Table  ││   AI   │
│API :5001││API :5002││API :5003││API :5004││API :5005│
│         ││         ││         ││         ││         │
│- Auth   ││- Menu   ││- Orders ││- Tables ││- Ollama │
│- Staff  ││- Categ. ││- Payment││- Session││- LLM   │
│- Setting││- Upload ││- SignalR││- Reserv.││         │
│         ││         ││- Jobs   ││- QR     ││         │
└────┬────┘└────┬────┘└────┬────┘└────┬────┘└────┬────┘
     │         │         │         │         │
     └─────────┴─────────┴────┬────┴─────────┘
                              │
                   ┌──────────▼──────────┐
                   │     PostgreSQL      │
                   │   Database (15)     │
                   │   Tables: 37        │
                   └─────────────────────┘

External Services:
┌─────────────────────────────────────────────────┐
│ PayOS Gateway (Payment)    │ Ollama (AI LLM)    │
│ Prometheus (Metrics)       │ Grafana (Dashboard)│
│ Map Server (Node.js:3001)  │ Webots (Robot)     │
└─────────────────────────────────────────────────┘
```

### 1.2 Package Diagram

#### Backend Shared Libraries

```
SmartDine.Shared/
├── SmartDine.Domain/           # Entities, Enums, Interfaces
│   ├── Entities/               # 37 entity classes
│   ├── Enums/                  # 18 enum types
│   ├── Interfaces/             # 20 repository/service interfaces
│   └── Constants/              # Roles, messages
│
├── SmartDine.Application/      # Business Logic
│   ├── Services/               # 10 service implementations
│   ├── DTOs/                   # 9 DTO categories
│   ├── Helper/                 # Utility classes
│   └── DependencyInjection.cs  # DI registration
│
└── SmartDine.Infrastructure/   # Data Access & External
    ├── Data/                   # DbContext, Configurations
    ├── Repositories/           # Repository implementations
    ├── Migrations/             # EF Core migrations
    ├── Security/               # JWT, RSA, BCrypt
    └── ExternalServices/       # PayOS, Ollama clients
```

#### Microservice Structure

```
SmartDine.Identity.API/
├── Controllers/     # AuthController, StaffController, SettingsController
├── Program.cs       # Service registration, middleware
└── appsettings.json # Configuration

SmartDine.Menu.API/
├── Controllers/     # MenuItemsController, MenuCategoriesController
└── ...

SmartDine.Order.API/
├── Controllers/     # OrdersController, PaymentsController, MapsController
├── Hubs/            # OrderHub, RobotHub
├── BackgroundJobs/  # PaymentExpiryCleanup
└── ...

SmartDine.Table.API/
├── Controllers/     # TablesController, DiningSessionsController, LocationsController
└── ...
```

#### Frontend Package Structure

```
web-dashboard/src/
├── pages/
│   ├── auth/           # LoginPage
│   ├── dashboard/      # DashboardPage, TableManagement, MenuManagement,
│   │                   # StaffManagement, Transactions, Settings, StaffDashboard
│   └── draw_map/       # RestaurantDrawPage (floor plan editor)
├── components/
│   ├── components_draw_map/  # MapCanvas, RobotConsole, Toolbar, Toolbox
│   ├── common/         # Shared UI components
│   └── feedback/       # Feedback components
├── hooks/              # useSignalR, useOrderHub
├── services/           # API services, aiService, menuService, orderService
├── store/              # Redux (slices/) + Zustand (mapStore)
└── types/              # TypeScript type definitions

customer-mobile/lib/
├── pages/
│   ├── auth/, cart/, checkout/, home/, orders/, profile/, settings/
├── models/             # auth_models, menu_models, order_models
├── services/           # api_client, repositories, socket/
├── viewmodels/         # auth, cart, menu, order (Riverpod)
├── routes/             # GoRouter configuration
├── config/, utils/, widgets/
```

---

## 2. Database Design

### 2.1 Entity Relationship Overview

**Core Domain Tables:**

| Table | Key Fields | Relationships |
|-------|------------|---------------|
| `users` | Id, FullName, Email, PasswordHash, Role, IsActive | — |
| `customers` | Id, FullName, Phone, Email, LoyaltyPoints, MembershipLevel | 1:N DiningSessions, Reviews, Activities |
| `tables` | Id, TableNumber, Capacity, QrCode, Status, LocationId | 1:N DiningSessions, Reservations |
| `locations` | Id, ... (table positioning) | — |
| `dining_sessions` | Id, CustomerId, TableId, GuestName, Status, StartedAt | 1:N Orders, Participants, Payments |
| `menu_categories` | Id, Name, Description | 1:N MenuItems |
| `menu_items` | Id, CategoryId, Name, Price, IsAvailable, ImageUrl | 1:N OrderDetails, Reviews |
| `orders` | Id, SessionId, TotalAmount, DiscountAmount, Status | 1:N OrderDetails, Payments |
| `order_details` | Id, OrderId, MenuItemId, Quantity, UnitPrice | — |
| `payments` | Id, SessionId, Amount, PaymentMethod, PaymentStatus | — |

**Loyalty & Promotions:**

| Table | Purpose |
|-------|---------|
| `loyalty_transactions` | Points earn/spend history |
| `promotions` | Discount codes (PERCENT/FIXED), date range |
| `promotion_memberships` | Tier-targeted promotions |
| `promotion_menu_items` | Item-specific promotions |
| `order_promotions` | Applied promotions per order |
| `customer_coupons` | Individual coupon assignments |
| `combos` / `combo_items` | Meal combo packages |

**Robot System:**

| Table | Purpose |
|-------|---------|
| `robots` | RobotCode, Status (AVAILABLE/DELIVERING/CHARGING/OFFLINE), BatteryLevel |
| `robot_delivery_batches` | Delivery batch per table |
| `robot_delivery_items` | Individual items in a batch |

**Supporting Tables:**

| Table | Purpose |
|-------|---------|
| `refresh_tokens` | JWT refresh token management |
| `password_reset_tokens` | Password reset flow |
| `notifications` | In-app notifications |
| `customer_activities` | Activity logging for AI |
| `customer_statistics` | Aggregated customer stats |
| `menu_item_statistics` | Aggregated menu stats |
| `session_participants` | Multi-diner sessions |
| `table_reservations` | Advance table booking |
| `restaurant_settings` | Global restaurant config |

### 2.2 Key Design Patterns

- **Soft delete**: All entities inherit `BaseEntity` with `IsDeleted` flag + global query filters
- **Auto timestamps**: `CreatedAt`/`UpdatedAt` managed by `SaveChangesAsync` override
- **Repository + Unit of Work**: Generic repository pattern with `IUnitOfWork`
- **Fluent API configurations**: Separate configuration classes per entity

---

## 3. Detailed Design

### 3.1 Authentication Module

#### 3.1.1 Class Diagram

```
┌─────────────────────┐
│   AuthService       │
├─────────────────────┤
│ - _userRepository   │ IUserRepository
│ - _customerRepo     │ ICustomerRepository
│ - _tokenService     │ IJwtTokenService
├─────────────────────┤
│ + LoginAsync()      │ LoginResponse
│ + RegisterAsync()   │ RegisterResponse
│ + GuestLoginAsync() │ LoginResponse
│ + RefreshTokenAsync()│ LoginResponse
│ + LogoutAsync()     │ void
│ + ForgotPasswordAsync()│ void
│ + ResetPasswordAsync()│ void
└─────────┬───────────┘
          │ uses
          ▼
┌─────────────────────┐
│ JwtTokenService     │
├─────────────────────┤
│ - _rsaKeyProvider   │ IRsaKeyProvider
├─────────────────────┤
│ + GenerateTokenPair()│ TokenPair
│ + ValidateToken()   │ ClaimsPrincipal
│ + RevokeRefreshToken()│ void
└─────────────────────┘
```

#### 3.1.2 Sequence Diagram: Customer Login

```
Client          Gateway         Identity API      Database
  │                │                │                │
  │ POST /auth/login│               │                │
  │───────────────▶│                │                │
  │                │ /auth/login    │                │
  │                │───────────────▶│                │
  │                │                │ FindByEmail()  │
  │                │                │───────────────▶│
  │                │                │ User           │
  │                │                │◀───────────────│
  │                │                │ ValidatePassword()
  │                │                │ GenerateTokenPair()
  │                │                │ SaveRefreshToken()
  │                │                │───────────────▶│
  │                │ {accessToken,  │                │
  │                │  refreshToken} │                │
  │                │◀───────────────│                │
  │ {accessToken,  │                │                │
  │  refreshToken} │                │                │
  │◀───────────────│                │                │
```

#### 3.1.3 Sequence Diagram: Guest Login (QR Scan)

```
Customer         Mobile App       Table API        Identity API    Database
  │                │                │                │                │
  │ Scan QR Code   │                │                │                │
  │───────────────▶│                │                │                │
  │                │ GET /tables/{id}│               │                │
  │                │───────────────▶│                │                │
  │                │ {table}        │                │                │
  │                │◀───────────────│                │                │
  │                │                │                │                │
  │                │ POST /auth/guest│               │                │
  │                │───────────────▶│───────────────▶│                │
  │                │                │                │ CreateGuest()  │
  │                │                │                │───────────────▶│
  │                │ {accessToken,  │                │                │
  │                │  refreshToken} │                │                │
  │                │◀───────────────│                │                │
  │ Open App Menu  │                │                │                │
  │◀───────────────│                │                │                │
```

---

### 3.2 Order Management Module

#### 3.2.1 Class Diagram

```
┌─────────────────────┐
│   OrderService      │
├─────────────────────┤
│ - _orderRepo        │ IOrderRepository
│ - _unitOfWork       │ IUnitOfWork
│ - _hubContext       │ IHubContext<OrderHub>
├─────────────────────┤
│ + PlaceOrderAsync() │ Order
│ + GetActiveOrdersAsync()│ List<Order>
│ + UpdateStatusAsync()│ Order
│ + GetHistoryAsync() │ List<Order>
└─────────────────────┘
         │
         │ uses
         ▼
┌─────────────────────┐
│  OrderHub (SignalR) │
├─────────────────────┤
│ + BroadcastOrderCreated()│
│ + BroadcastStatusChanged()│
└─────────────────────┘
```

#### 3.2.2 Sequence Diagram: Place Order

```
Customer        Mobile App      Gateway         Order API      Menu API      Database   SignalR
  │                │              │               │              │              │          │
  │ Confirm Order  │              │               │              │              │          │
  │───────────────▶│              │               │              │              │          │
  │                │ POST /orders │               │              │              │          │
  │                │─────────────▶│               │              │              │          │
  │                │              │ POST /orders  │              │              │          │
  │                │              │──────────────▶│              │              │          │
  │                │              │               │ ValidateSession()            │          │
  │                │              │               │──────────────▶──────────────│          │
  │                │              │               │ Session OK   │              │          │
  │                │              │               │◀─────────────│              │          │
  │                │              │               │              │              │          │
  │                │              │               │ CreateOrder()│              │          │
  │                │              │               │─────────────────────────────│          │
  │                │              │               │ Order        │              │          │
  │                │              │               │◀────────────────────────────│          │
  │                │              │               │              │              │          │
  │                │              │ 201 Created   │              │              │          │
  │                │              │◀──────────────│              │              │          │
  │                │ {order}      │               │              │              │          │
  │                │◀─────────────│               │              │              │          │
  │                │              │               │ BroadcastOrderCreated()     │          │
  │                │              │               │────────────────────────────────────────▶│
  │                │              │               │              │              │ Kitchen    │
  │                │              │               │              │              │ receives   │
```

#### 3.2.3 Order State Machine

```
                    ┌──────────┐
                    │ PENDING  │
                    └────┬─────┘
                         │
                         ▼
                    ┌──────────┐
              ┌─────│CONFIRMED │
              │     └────┬─────┘
              │          │
              ▼          ▼
         ┌──────────┐ ┌──────────┐
         │CANCELLED │ │ COOKING  │
         └──────────┘ └────┬─────┘
                           │
                           ▼
                      ┌──────────┐
                      │  READY   │
                      └────┬─────┘
                           │
                           ▼
                      ┌──────────┐
                      │COMPLETED │
                      └──────────┘
```

---

### 3.3 Payment Module

#### 3.3.1 Class Diagram

```
┌─────────────────────┐
│  PaymentService     │
├─────────────────────┤
│ - _paymentRepo      │ IPaymentRepository
│ - _payosClient      │ IPayOSClient
│ - _unitOfWork       │ IUnitOfWork
├─────────────────────┤
│ + CreatePaymentIntentAsync()│ PaymentResponse
│ + HandleWebhookAsync()│ void
│ + GetHistoryAsync() │ List<Payment>
└─────────────────────┘
```

#### 3.3.2 Sequence Diagram: Payment Flow

```
Customer       Mobile App     Gateway       Order API       PayOS        Database    SignalR
  │               │             │              │              │              │          │
  │ Pay Bill      │             │              │              │              │          │
  │──────────────▶│             │              │              │              │          │
  │               │ POST /payments/create-intent             │              │          │
  │               │────────────▶│              │              │              │          │
  │               │             │ POST /payments/create-intent              │          │
  │               │             │─────────────▶│              │              │          │
  │               │             │              │ Check CASH?  │              │          │
  │               │             │              │──┐           │              │          │
  │               │             │              │  │ CASH:      │              │          │
  │               │             │              │◀─┘ bypass    │              │          │
  │               │             │              │              │              │          │
  │               │             │              │ CreatePaymentLinkAsync()    │          │
  │               │             │              │─────────────▶│              │          │
  │               │             │              │ {qrUrl,      │              │          │
  │               │             │              │  deeplink}   │              │          │
  │               │             │              │◀─────────────│              │          │
  │               │             │              │              │              │          │
  │               │             │  {qrUrl}     │              │              │          │
  │               │             │◀─────────────│              │              │          │
  │               │ {qrUrl}     │              │              │              │          │
  │               │◀────────────│              │              │              │          │
  │               │             │              │              │              │          │
  │ Complete Payment (via PayOS app/bank)     │              │              │          │
  │               │             │              │              │              │          │
  │               │             │              │ POST /payments/webhook      │          │
  │               │             │              │◀─────────────│ (HMAC verify)│          │
  │               │             │              │ UpdatePaymentStatus()       │          │
  │               │             │              │─────────────────────────────│          │
  │               │             │              │ AwardLoyaltyPoints()        │          │
  │               │             │              │─────────────────────────────│          │
  │               │             │              │ BroadcastPaymentSuccess()   │          │
  │               │             │              │────────────────────────────────────────▶│
```

---

### 3.4 Table & Dining Session Module

#### 3.4.1 Class Diagram

```
┌─────────────────────┐
│  TableService       │
├─────────────────────┤
│ - _tableRepo        │ ITableRepository
│ - _sessionRepo      │ IDiningSessionRepository
│ - _unitOfWork       │ IUnitOfWork
├─────────────────────┤
│ + GetTablesAsync()  │ List<Table>
│ + CreateTableAsync()│ Table
│ + GenerateQrCode()  │ string
│ + StartSessionAsync()│ DiningSession
│ + JoinSessionAsync()│ void
│ + CheckoutAsync()   │ void
└─────────────────────┘
```

#### 3.4.2 Sequence Diagram: QR Scan & Start Session

```
Customer       Mobile App     Gateway       Table API      Database
  │               │             │              │              │
  │ Scan QR       │             │              │              │
  │──────────────▶│             │              │              │
  │               │ GET /tables/{id}           │              │
  │               │────────────▶│              │              │
  │               │             │ GET /tables/{id}           │
  │               │             │─────────────▶│              │
  │               │             │              │ FindById()   │
  │               │             │              │─────────────▶│
  │               │             │ {table}      │              │
  │               │             │◀─────────────│              │
  │               │ {table}     │              │              │
  │               │◀────────────│              │              │
  │               │             │              │              │
  │               │ POST /dining-sessions      │              │
  │               │ {tableId}   │              │              │
  │               │────────────▶│              │              │
  │               │             │ POST /dining-sessions      │
  │               │             │─────────────▶│              │
  │               │             │              │ CreateSession()
  │               │             │              │─────────────▶│
  │               │             │ {session}    │              │
  │               │             │◀─────────────│              │
  │               │ {session}   │              │              │
  │               │◀────────────│              │              │
  │ Open Menu     │             │              │              │
  │◀──────────────│             │              │              │
```

---

### 3.5 Robot Delivery Module

#### 3.5.1 Class Diagram

```
┌─────────────────────┐
│   RobotHub (SignalR)│
├─────────────────────┤
│ + SendCommandAsync()│
│ + UpdateStateAsync()│
│ + BroadcastPath()   │
└─────────┬───────────┘
          │ communicates with
          ▼
┌─────────────────────┐
│  Python Sidecar     │
├─────────────────────┤
│ - SignalR Client    │
│ - File I/O Bridge   │
│ - HTTP Client       │
└─────────┬───────────┘
          │ reads/writes files
          ▼
┌─────────────────────┐
│ Webots Controller   │
│ (C, ~2068 lines)    │
├─────────────────────┤
│ - DWA Path Planner  │
│ - LiDAR Obstacle    │
│ - GPS + Inertial    │
│ - Waypoint Following│
└─────────────────────┘
```

#### 3.5.2 Sequence Diagram: Robot Delivery

```
Staff          Dashboard       RobotHub        Backend        Sidecar        Webots
  │               │              │              │              │              │
  │ Trigger       │              │              │              │              │
  │ Delivery      │              │              │              │              │
  │──────────────▶│              │              │              │              │
  │               │ POST /robots/command         │              │              │
  │               │─────────────▶│              │              │              │
  │               │              │ SendCommandAsync()           │              │
  │               │              │─────────────▶│              │              │
  │               │              │              │ Write command file           │
  │               │              │              │─────────────▶│              │
  │               │              │              │              │ Read command │
  │               │              │              │              │─────────────▶│
  │               │              │              │              │              │ Plan path
  │               │              │              │              │              │ (DWA)
  │               │              │              │              │ Navigate     │
  │               │              │              │              │◀─────────────│
  │               │              │              │              │              │
  │               │              │              │              │ Write state  │
  │               │              │              │              │─────────────▶│
  │               │              │              │ Read state   │              │
  │               │              │              │◀─────────────│              │
  │               │              │              │              │              │
  │               │              │              │ UpdateStateAsync()          │
  │               │              │              │─────────────▶│              │
  │               │              │ StateUpdate  │              │              │
  │               │              │◀─────────────│              │              │
  │               │ StateUpdate  │              │              │              │
  │               │◀─────────────│              │              │              │
  │ Dashboard     │              │              │              │              │
  │ updated       │              │              │              │              │
  │◀──────────────│              │              │              │              │
```

---

### 3.6 AI Recommendation Module

#### 3.6.1 Class Diagram

```
┌─────────────────────┐
│   AIService         │
├─────────────────────┤
│ - _ollamaClient     │ IOllamaClient
│ - _activityRepo     │ ICustomerActivityRepository
├─────────────────────┤
│ + GetRecommendationsAsync()│ List<MenuItem>
│ + LogRecommendation()│ void
└─────────────────────┘
```

#### 3.6.2 Sequence Diagram: Get Recommendations

```
Customer       Mobile App      Gateway        AI API         Ollama        Database
  │               │              │              │              │              │
  │ View Menu     │              │              │              │              │
  │──────────────▶│              │              │              │              │
  │               │ GET /ai/recommendations     │              │              │
  │               │─────────────▶│              │              │              │
  │               │              │ GET /ai/recommendations     │              │
  │               │              │─────────────▶│              │              │
  │               │              │              │ GetCustomerActivity()        │
  │               │              │              │─────────────────────────────▶│
  │               │              │              │ Activities   │              │
  │               │              │              │◀─────────────────────────────│
  │               │              │              │              │              │
  │               │              │              │ Build prompt │              │
  │               │              │              │ (frequently  │              │
  │               │              │              │  ordered     │              │
  │               │              │              │  categories) │              │
  │               │              │              │              │              │
  │               │              │              │ POST /api/generate          │
  │               │              │              │─────────────▶│              │
  │               │              │              │ {response}   │              │
  │               │              │              │◀─────────────│              │
  │               │              │              │              │              │
  │               │              │              │ LogRecommendation()         │
  │               │              │              │─────────────────────────────▶│
  │               │              │              │              │              │
  │               │              │ {recommendations}           │              │
  │               │              │◀─────────────│              │              │
  │               │ {recommendations}           │              │              │
  │               │◀─────────────│              │              │              │
  │ View          │              │              │              │              │
  │ recommendations              │              │              │              │
  │◀──────────────│              │              │              │              │
```

---

### 3.7 Loyalty Program Module

#### 3.7.1 Membership Tier Logic

```
┌─────────────────────────────────────────┐
│ Customer Total Spent                     │
├─────────────────────────────────────────┤
│ < 1,000,000 VND        → BRONZE         │
│ 1,000,000 - 4,999,999  → SILVER (1.2x) │
│ 5,000,000 - 19,999,999 → GOLD (1.5x)   │
│ >= 20,000,000 VND       → PLATINUM (2x) │
└─────────────────────────────────────────┘
```

#### 3.7.2 Points Calculation

```
Points Earned = Floor(Amount / 1000) * Multiplier

Example:
- BRONZE customer spends 5,000 VND → Floor(5000/1000) * 1.0 = 5 points
- GOLD customer spends 5,000 VND → Floor(5000/1000) * 1.5 = 7 points
```

---

### 3.8 SignalR Hub Design

#### OrderHub (`/hubs/orders`)

| Client Method | Server Method | Description |
|---------------|---------------|-------------|
| — | `BroadcastOrderCreated(order)` | Kitchen receives new order |
| — | `BroadcastStatusChanged(orderId, status)` | Status update to all clients |
| — | `BroadcastPaymentSuccess(sessionId)` | Payment confirmation |

#### RobotHub (`/hubs/robot`)

| Client Method | Server Method | Description |
|---------------|---------------|-------------|
| `SendCommand(command)` | — | Dashboard sends robot command |
| — | `ReceiveCommand(command)` | Sidecar receives command |
| `UpdateState(state)` | — | Sidecar updates robot state |
| — | `ReceiveStateUpdate(state)` | Dashboard receives state |
| `UpdatePath(path)` | — | Sidecar updates navigation path |
| — | `ReceivePathUpdate(path)` | Dashboard receives path |
