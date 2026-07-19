# Phase 3: Software Requirement Specification (III)

## 1. Product Overview

SmartDine là hệ thống quản lý nhà hàng thông minh được xây dựng trên kiến trúc Microservices với các thành phần chính:

| Component | Technology | Port | Responsibility |
|-----------|------------|------|----------------|
| **API Gateway** | YARP Reverse Proxy | 5000 | Route all requests, SignalR passthrough |
| **Identity API** | ASP.NET Core | 5001 | Auth, Staff, Settings management |
| **Menu API** | ASP.NET Core | 5002 | MenuItems, Categories CRUD |
| **Order API** | ASP.NET Core | 5003 | Orders, Payments, SignalR Hubs |
| **Table API** | ASP.NET Core | 5004 | Tables, DiningSessions, Reservations |
| **AI API** | ASP.NET Core | 5005 | AI Recommendations via Ollama |
| **Map Server** | Node.js/Express | 3001 | Robot map management |
| **Database** | PostgreSQL 15 | 5432 | Primary data storage |

### System Context
- **Backend**: .NET 9.0 Microservices with PostgreSQL
- **Frontend (Web)**: React 18 + Vite + Ant Design 5
- **Frontend (Mobile)**: Flutter 3.0+ with Riverpod
- **Robot**: Webots Simulator + C controller + DWA planner
- **Real-time**: SignalR (OrderHub + RobotHub)
- **Payment**: PayOS Gateway (Vietnamese payment provider)
- **AI**: Ollama with qwen2.5:1.5b local LLM

---

## 2. User Requirements

### 2.1 Actors

| Actor | Description | Access Level |
|-------|-------------|--------------|
| **Guest** | Customer scan QR at table, no account | Browse menu, place order (limited) |
| **Customer** | Registered user with account | Full ordering, payment, loyalty |
| **Staff** | Restaurant employee | Process orders, update status |
| **Chef** | Kitchen staff | View orders, update cooking status |
| **Manager** | Restaurant manager | Full admin access, reports, settings |

### 2.2 Use Case Diagram

```
                    ┌─────────────────────────────────────┐
                    │           SmartDine System           │
                    │                                     │
    ┌──────┐       │  ┌─────────┐  ┌─────────┐         │
    │Guest │──────▶│  │  Scan   │  │  Place  │         │
    └──────┘       │  │   QR    │  │  Order  │         │
                    │  └─────────┘  └─────────┘         │
    ┌──────────┐   │  ┌─────────┐  ┌─────────┐         │
    │Customer  │──▶│  │  Browse │  │  Pay    │         │
    └──────────┘   │  │  Menu   │  │  Bill   │         │
                    │  └─────────┘  └─────────┘         │
                    │  ┌─────────┐  ┌─────────┐         │
    │  Staff   │──▶│  │ Process │  │ Update  │         │
    └──────────┘   │  │  Order  │  │ Status  │         │
                    │  └─────────┘  └─────────┘         │
    │  Chef    │──▶│  ┌─────────┐  ┌─────────┐         │
    └──────────┘   │  │  View   │  │  Cook   │         │
                    │  │ Kitchen │  │  Order  │         │
    │ Manager  │──▶│  └─────────┘  └─────────┘         │
    └──────────┘   │  ┌─────────┐  ┌─────────┐         │
                    │  │Manage   │  │  View   │         │
                    │  │Menu/Table│  │Reports  │         │
                    │  └─────────┘  └─────────┘         │
                    └─────────────────────────────────────┘
```

### 2.3 Use Case Descriptions

#### UC01: Customer Scan QR & Place Order
- **Actor**: Guest/Customer
- **Precondition**: Customer at restaurant table with QR code
- **Flow**:
  1. Customer scans QR code at table
  2. System opens Customer App with table context
  3. Customer browses menu (filtered by availability)
  4. Customer adds items to cart
  5. Customer applies coupon (if available)
  6. Customer confirms order
  7. System creates order linked to dining session
  8. System notifies kitchen via SignalR
- **Postcondition**: Order created, kitchen notified

#### UC02: Staff Process Order
- **Actor**: Staff/Chef
- **Precondition**: Staff logged in, orders exist in system
- **Flow**:
  1. Staff views active orders dashboard (real-time)
  2. Staff selects order to process
  3. Staff updates order status (CONFIRMED → COOKING → READY)
  4. System broadcasts status update via SignalR
  5. Customer receives notification on mobile
- **Postcondition**: Order status updated, customer notified

#### UC03: Customer Payment
- **Actor**: Customer
- **Precondition**: Order placed, customer ready to pay
- **Flow**:
  1. Customer views bill summary
  2. Customer selects payment method (Cash/PayOS)
  3. If PayOS: system creates payment link
  4. Customer completes payment
  5. System verifies payment via webhook
  6. System updates order status to COMPLETED
  7. System awards loyalty points
- **Postcondition**: Payment completed, order closed

#### UC04: Manager Manage Menu
- **Actor**: Manager
- **Precondition**: Manager logged in
- **Flow**:
  1. Manager navigates to Menu Management
  2. Manager creates/edits/deletes menu items
  3. Manager manages categories
  4. Manager toggles item availability
  5. System updates menu in real-time
- **Postcondition**: Menu updated, visible to customers

#### UC05: Robot Delivery
- **Actor**: Staff/System
- **Precondition**: Order ready, robot available, map configured
- **Flow**:
  1. Staff triggers robot delivery for table
  2. System sends command via SignalR (RobotHub)
  3. Robot controller plans path (DWA algorithm)
  4. Robot navigates to kitchen
  5. Robot picks up food
  6. Robot navigates to table
  7. Robot delivers food
  8. Robot returns to kitchen
  9. System updates dashboard
- **Postcondition**: Food delivered, robot returned

---

## 3. Functional Requirements

### 3.1 System Functional Overview

**Screen Flow:**
```
Login → Dashboard (Manager)
     → Kitchen Display (Staff/Chef)
     → Menu Browser (Customer)
     → Order Tracking (Customer)
     → Payment Screen (Customer)
```

**User Roles & Authorization:**
| Role | Permissions |
|------|-------------|
| MANAGER | Full CRUD on all resources, view reports, manage staff |
| STAFF | Process orders, update status, process payments |
| CHEF | View orders, update cooking status, toggle menu availability |
| CUSTOMER | Browse menu, place orders, view history, loyalty |
| GUEST | Browse menu, place orders (limited) |

### 3.2 Feature: Authentication & Identity

#### 3.2.1 Login
- **Endpoint**: `POST /api/v1/auth/login`
- **Request**: `{ email, password }`
- **Response**: `{ accessToken, refreshToken, user }`
- **Description**: Unified login for Staff (User) and Customer
- **Authorization**: Public

#### 3.2.2 Register (Customer)
- **Endpoint**: `POST /api/v1/auth/register`
- **Request**: `{ fullName, phone, email, password }`
- **Response**: `{ customer }`
- **Description**: Customer self-registration
- **Authorization**: Public

#### 3.2.3 Guest Login
- **Endpoint**: `POST /api/v1/auth/guest`
- **Request**: `{ tableId }`
- **Response**: `{ accessToken, refreshToken }`
- **Description**: Guest login via QR scan (no account needed)
- **Authorization**: Public

#### 3.2.4 Refresh Token
- **Endpoint**: `POST /api/v1/auth/refresh`
- **Request**: `{ refreshToken }`
- **Response**: `{ accessToken, refreshToken }`
- **Description**: Token refresh with rotation
- **Authorization**: Public (valid refresh token)

#### 3.2.5 Logout
- **Endpoint**: `POST /api/v1/auth/logout`
- **Description**: Revoke all refresh tokens
- **Authorization**: Authenticated

#### 3.2.6 Forgot/Reset Password
- **Endpoint**: `POST /api/v1/auth/forgot-password`, `POST /api/v1/auth/reset-password`
- **Description**: Password reset flow
- **Authorization**: Public (forgot), Token-based (reset)

### 3.3 Feature: Menu Management

#### 3.3.1 Get Menu (Paginated)
- **Endpoint**: `GET /api/v1/menu?page=&pageSize=&categoryId=&search=`
- **Response**: `{ items[], totalCount, page, pageSize }`
- **Description**: Paginated menu with category filter and search
- **Authorization**: Public

#### 3.3.2 Get Menu Item Detail
- **Endpoint**: `GET /api/v1/menu/{id}`
- **Response**: `{ item }`
- **Authorization**: Public

#### 3.3.3 Create Menu Item
- **Endpoint**: `POST /api/v1/menu`
- **Request**: `{ name, description, price, categoryId, imageUrl, isAvailable }`
- **Authorization**: MANAGER

#### 3.3.4 Update Menu Item
- **Endpoint**: `PUT /api/v1/menu/{id}`
- **Authorization**: MANAGER

#### 3.3.5 Delete Menu Item
- **Endpoint**: `DELETE /api/v1/menu/{id}`
- **Authorization**: MANAGER

#### 3.3.6 Toggle Availability
- **Endpoint**: `PATCH /api/v1/menu/{id}/availability`
- **Authorization**: MANAGER, CHEF

#### 3.3.7 Manage Categories
- **Endpoints**: CRUD `/api/v1/categories`
- **Authorization**: MANAGER

### 3.4 Feature: Order Management

#### 3.4.1 Place Order
- **Endpoint**: `POST /api/v1/orders`
- **Request**: `{ sessionId, items: [{ menuItemId, quantity }], couponCode? }`
- **Response**: `201 Created { order }`
- **Description**: Place order within dining session
- **Authorization**: CUSTOMER, GUEST

#### 3.4.2 Get Active Orders
- **Endpoint**: `GET /api/v1/orders/active`
- **Response**: `{ orders[] }`
- **Description**: Orders not COMPLETED or CANCELLED (blacklist query)
- **Authorization**: STAFF, CHEF, MANAGER

#### 3.4.3 Get Order Detail
- **Endpoint**: `GET /api/v1/orders/{id}`
- **Authorization**: Owner, STAFF, CHEF, MANAGER

#### 3.4.4 Update Order Status
- **Endpoint**: `PATCH /api/v1/orders/{id}/status`
- **Request**: `{ status }`
- **State Machine**: PENDING → CONFIRMED → COOKING → READY → COMPLETED (or CANCELLED)
- **Authorization**: STAFF, CHEF, MANAGER

#### 3.4.5 Get Order History
- **Endpoint**: `GET /api/v1/orders/history`
- **Authorization**: CUSTOMER (own), MANAGER (all)

#### 3.4.6 Real-time Updates (SignalR)
- **Hub**: `/hubs/orders`
- **Events**: OrderCreated, OrderStatusChanged, OrderCompleted
- **Description**: Real-time order tracking for kitchen and customers

### 3.5 Feature: Payment System

#### 3.5.1 Create Payment Intent
- **Endpoint**: `POST /api/v1/payments/create-intent`
- **Request**: `{ sessionId, paymentMethod, splitCount? }`
- **Response**: `{ qrUrl, deeplink }` (for non-CASH)
- **Description**: CASH bypasses PayOS gateway entirely
- **Authorization**: CUSTOMER

#### 3.5.2 Handle Webhook
- **Endpoint**: `POST /api/v1/payments/webhook`
- **Description**: PayOS webhook with HMAC verification
- **Authorization**: PayOS system

#### 3.5.3 Get Payment History
- **Endpoint**: `GET /api/v1/payments/history`
- **Authorization**: MANAGER

#### 3.5.4 Loyalty Points
- **Description**: 1 point per 1,000 VND spent, auto-credited on payment success

### 3.6 Feature: Table & Dining Session

#### 3.6.1 Get Tables
- **Endpoint**: `GET /api/v1/tables`
- **Response**: `{ tables[] }` with real-time status
- **Authorization**: STAFF, MANAGER

#### 3.6.2 Create/Update Table
- **Endpoint**: `POST/PUT /api/v1/tables`
- **Authorization**: MANAGER

#### 3.6.3 Generate QR Code
- **Endpoint**: `GET /api/v1/tables/{id}/qr`
- **Response**: QR code image URL
- **Description**: QR encodes table ID for customer app
- **Authorization**: MANAGER

#### 3.6.4 Start Dining Session
- **Endpoint**: `POST /api/v1/dining-sessions`
- **Request**: `{ tableId, guestName?, guestPhone? }`
- **Description**: QR scan creates session, status = ACTIVE
- **Authorization**: CUSTOMER, GUEST

#### 3.6.5 Join Dining Session
- **Endpoint**: `POST /api/v1/dining-sessions/{id}/join`
- **Description**: Multi-diner support, host transfer on leave
- **Authorization**: CUSTOMER

#### 3.6.6 Checkout Session
- **Endpoint**: `POST /api/v1/dining-sessions/{id}/checkout`
- **Description**: Session lifecycle: ACTIVE → CHECKOUT → CLOSED
- **Authorization**: CUSTOMER (host), STAFF

#### 3.6.7 Table Reservation
- **Endpoint**: `POST /api/v1/reservations`
- **Description**: Conflict detection, time slot management
- **Authorization**: CUSTOMER

### 3.7 Feature: Robot Delivery

#### 3.7.1 Send Robot Command
- **Endpoint**: `POST /api/v1/robots/command`
- **Request**: `{ type: "deliver"|"return"|"manual", tableId?, position? }`
- **Authorization**: STAFF, MANAGER

#### 3.7.2 Get Robot Status
- **Endpoint**: `GET /api/v1/robots/status`
- **Response**: `{ robots[] }` with battery, position, state
- **Authorization**: STAFF, MANAGER

#### 3.7.3 Real-time Robot Communication (SignalR)
- **Hub**: `/hubs/robot`
- **Events**: Command, StateUpdate, PathUpdate
- **Description**: Bidirectional communication between dashboard and robot

#### 3.7.4 Map Management
- **Endpoint**: CRUD `/api/v1/maps`
- **Description**: Restaurant floor maps (PGM + YAML + Graph + Waypoints)
- **Authorization**: MANAGER

### 3.8 Feature: AI Recommendation

#### 3.8.1 Get Recommendations
- **Endpoint**: `GET /api/v1/ai/recommendations?customerId=`
- **Response**: `{ recommendations[] }`
- **Description**: Personalized menu suggestions via Ollama LLM
- **Authorization**: CUSTOMER

### 3.9 Feature: Loyalty Program

#### 3.9.1 Get Customer Profile
- **Endpoint**: `GET /api/v1/customers/profile`
- **Response**: `{ loyaltyPoints, membershipLevel, totalSpent, visitCount }`
- **Authorization**: CUSTOMER

#### 3.9.2 Get Loyalty History
- **Endpoint**: `GET /api/v1/customers/loyalty/history`
- **Authorization**: CUSTOMER

#### 3.9.3 Membership Tiers
| Tier | Threshold (VND) | Benefits |
|------|-----------------|----------|
| BRONZE | 0 - 999,999 | Base points |
| SILVER | 1,000,000 - 4,999,999 | 1.2x points |
| GOLD | 5,000,000 - 19,999,999 | 1.5x points, exclusive coupons |
| PLATINUM | 20,000,000+ | 2x points, priority service |

---

## 4. Non-Functional Requirements

### 4.1 External Interfaces

| Interface | Protocol | Purpose |
|-----------|----------|---------|
| PayOS API | REST + HMAC-SHA256 | Payment processing |
| Ollama API | HTTP | AI inference (local) |
| SignalR | WebSocket | Real-time communication |
| Prometheus | HTTP | Metrics scraping |
| Grafana | HTTP | Dashboard visualization |

### 4.2 Quality Attributes

| Attribute | Requirement |
|-----------|-------------|
| **Performance** | API response < 2s, SignalR < 500ms |
| **Availability** | 99.5% uptime |
| **Scalability** | Horizontal scaling via Docker containers |
| **Security** | JWT RSA-256, BCrypt passwords, HTTPS, CORS |
| **Usability** | Responsive UI, intuitive navigation, < 3 clicks to complete task |
| **Reliability** | Idempotency middleware, retry logic, graceful degradation |
| **Maintainability** | Clean architecture, 70%+ test coverage |
| **Portability** | Docker deployment, cross-platform (Windows/Linux) |

---

## 5. Requirement Appendix

### 5.1 Business Rules

| Rule ID | Rule | Description |
|---------|------|-------------|
| BR-01 | Session Required | Order must be linked to ACTIVE dining session |
| BR-02 | Session Payment | Payment covers all orders in a session (session-level) |
| BR-03 | CASH Bypass | CASH payment bypasses PayOS gateway entirely |
| BR-04 | Loyalty Points | 1 point per 1,000 VND spent |
| BR-05 | Membership Upgrade | Based on total spent thresholds |
| BR-06 | Reservation Conflict | Must check time slot conflicts before confirming |
| BR-07 | Robot Prerequisites | Map + waypoints must be configured before delivery |
| BR-08 | Order Timeout | Payment intent expires after 30 minutes |
| BR-09 | Soft Delete | All entities support soft delete with `IsDeleted` flag |
| BR-10 | Idempotency | Order placement uses idempotency middleware |

### 5.2 Common Requirements

- All API responses follow standard format: `{ success, data, message, errors? }`
- Pagination format: `{ items[], totalCount, page, pageSize }`
- Authentication: Bearer token in Authorization header
- Content-Type: application/json for all requests
- CORS configured for frontend domains

### 5.3 Application Messages List

| Code | Message | Type |
|------|---------|------|
| AUTH_001 | Email or password is incorrect | Error |
| AUTH_002 | Email already exists | Error |
| AUTH_003 | Token has expired | Error |
| ORDER_001 | Dining session not found | Error |
| ORDER_002 | Dining session is not active | Error |
| ORDER_003 | Menu item is not available | Error |
| ORDER_004 | Order not found | Error |
| ORDER_005 | Invalid order status transition | Error |
| PAY_001 | Payment creation failed | Error |
| PAY_002 | Payment webhook verification failed | Error |
| TABLE_001 | Table not found | Error |
| TABLE_002 | Table is not available | Error |
| TABLE_003 | Reservation conflict detected | Error |
| ROBOT_001 | Robot not available | Error |
| ROBOT_002 | Map not configured | Error |
