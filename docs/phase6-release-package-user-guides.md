# Phase 6: Release Package & User Guides (VI)

## 1. Deliverable Package

| No. | Deliverable Item | Description | Location |
|-----|-----------------|-------------|----------|
| 1 | Schedule/Task Tracking | GitHub Projects board | GitHub |
| 2 | Project Backlog | Sprint tasks, user stories | GitHub Projects |
| 3 | Source Codes | Backend (5 APIs), Web Dashboard, Mobile App | `app/BE/`, `app/FE/` |
| 4 | Database Script(s) | EF Core Migrations | `app/BE/SmartDine.Infrastructure/Migrations/` |
| 5 | Final Report Document | Capstone Project Report | `SmartDine_Final Project Report.docx.md` |
| 6 | Test Cases Document | Unit + Integration test files | `app/BE/Tests/SmartDine.Tests/` |
| 7 | Defects List | GitHub Issues | GitHub Issues |
| 8 | Issues List | GitHub Issues | GitHub Issues |
| 9 | Slide | Presentation slide | [Slide file] |

### Source Code Structure

```
PRM_SU26/
├── app/
│   ├── BE/                              # Backend
│   │   ├── SmartDine.Gateway/           # API Gateway (port 5000)
│   │   ├── SmartDine.Identity.API/      # Auth Service (port 5001)
│   │   ├── SmartDine.Menu.API/          # Menu Service (port 5002)
│   │   ├── SmartDine.Order.API/         # Order Service (port 5003)
│   │   ├── SmartDine.Table.API/         # Table Service (port 5004)
│   │   ├── SmartDine.AI.API/            # AI Service (port 5005)
│   │   ├── SmartDine.Application/       # Shared Business Logic
│   │   ├── SmartDine.Domain/            # Shared Entities
│   │   ├── SmartDine.Infrastructure/    # Shared Data Access
│   │   ├── SmartDine.Tests/             # Test Suite
│   │   ├── docker/                      # Dockerfiles (6)
│   │   └── docker-compose.yml           # Orchestration
│   ├── FE/
│   │   ├── web-dashboard/               # React Admin Dashboard
│   │   └── customer-mobile/             # Flutter Customer App
│   └── map-server/                      # Node.js Map Server
├── Robot/                               # Webots Robot Controller
├── docs/                                # Documentation
└── README.md                            # Project README
```

---

## 2. Installation Guides

### 2.1 System Requirements

#### Backend

| Component | Requirement |
|-----------|-------------|
| OS | Windows 10+, macOS, Linux |
| .NET SDK | 10.0 |
| PostgreSQL | 15+ |
| Docker | Latest (optional) |
| Docker Compose | Latest (optional) |

#### Web Dashboard

| Component | Requirement |
|-----------|-------------|
| OS | Any (browser-based) |
| Node.js | 18+ |
| npm | 9+ |

#### Customer Mobile App

| Component | Requirement |
|-----------|-------------|
| Flutter SDK | 3.0.0+ |
| Dart SDK | 3.0.0+ |
| Android Studio | Latest (for Android) |
| Xcode | Latest (for iOS, macOS only) |

#### Robot (Optional)

| Component | Requirement |
|-----------|-------------|
| Webots Simulator | R2023a+ |
| GCC Compiler | Latest |
| Python | 3.8+ |

### 2.2 Installation Instruction

#### Step 1: Clone Repository

```bash
git clone https://github.com/.../PRM_SU26.git
cd PRM_SU26
```

#### Step 2: Setup Database

```bash
# Install EF Core CLI (if not installed)
dotnet tool install --global dotnet-ef

# Update database
cd app/BE
dotnet ef database update --project SmartDine.Infrastructure --startup-project SmartDine.Identity.API
```

#### Step 3: Run Backend (Option A: Local)

```bash
cd app/BE
# Run all services simultaneously (Windows)
run-services.bat

# Or run individually (Terminal 1-5)
dotnet run --project SmartDine.Gateway
dotnet run --project SmartDine.Identity.API
dotnet run --project SmartDine.Menu.API
dotnet run --project SmartDine.Order.API
dotnet run --project SmartDine.Table.API
```

#### Step 3: Run Backend (Option B: Docker)

```bash
cd app/BE
docker-compose up --build
```

#### Step 4: Run Web Dashboard

```bash
cd app/FE/web-dashboard
npm install
npm run dev
# Access at http://localhost:5173
```

#### Step 5: Run Customer Mobile App

```bash
cd app/FE/customer-mobile
flutter pub get

# Run on mobile device/emulator
flutter run

# Run on web (MUST use port 8090 for QR code to work)
flutter run -d chrome --web-port=8090
```

#### Step 6: Run Robot (Optional)

```bash
# 1. Start Map Server
cd app/map-server
npm install
npm start
# Server runs at http://localhost:3001

# 2. Open Webots Simulator
# Load robot world file from Robot/ directory
# Robot controller auto-connects via sidecar
```

---

## 3. User Manual

### 3.1 Overview

SmartDine consists of 3 main interfaces:

```
┌─────────────────────────────────────────────────────────┐
│                    SmartDine System                      │
├──────────────────┬──────────────────┬───────────────────┤
│  Customer Mobile │   Web Dashboard  │   Robot Console   │
│  (Flutter App)   │   (React Admin)  │   (Webots + Web)  │
├──────────────────┼──────────────────┼───────────────────┤
│ • Browse Menu    │ • Manage Menu    │ • View Robot      │
│ • Place Order    │ • Manage Tables  │ • Send Commands   │
│ • Pay Bill       │ • View Orders    │ • Monitor Path    │
│ • View History   │ • Manage Staff   │ • Configure Map   │
│ • Loyalty Points │ • View Reports   │                   │
│ • QR Scanning    │ • Robot Control  │                   │
└──────────────────┴──────────────────┴───────────────────┘
```

### 3.2 Workflow 1: Customer Order Flow

**Purpose**: Customer scans QR code at table, browses menu, places order, and pays.

#### Workflow Diagram

```
┌─────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Scan   │───▶│  Browse  │───▶│   Add    │───▶│  Place   │───▶│   Pay    │
│  QR Code│    │   Menu   │    │   Cart   │    │  Order   │    │   Bill   │
└─────────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘
```

#### Step-by-Step Guide

**Step 1: Scan QR Code**
1. Open camera on your phone
2. Scan the QR code on the table
3. Customer App opens with your table context

**Step 2: Browse Menu**
1. View menu categories on the home screen
2. Tap category to filter items
3. Use search bar to find specific dishes
4. Tap item to view details (price, description, image)

**Step 3: Add to Cart**
1. Tap "Add to Cart" on desired items
2. Adjust quantity in cart
3. View cart summary (subtotal, discount, total)

**Step 4: Place Order**
1. Review cart items
2. Apply coupon code (if available)
3. Tap "Place Order"
4. Order sent to kitchen (real-time notification)

**Step 5: Pay Bill**
1. Tap "Pay" when ready
2. Select payment method:
   - **Cash**: Pay directly to staff
   - **PayOS**: Scan QR / use banking app
3. Complete payment
4. Receive confirmation + loyalty points

### 3.3 Workflow 2: Kitchen Processing Flow

**Purpose**: Staff/Chef view and process incoming orders in real-time.

#### Workflow Diagram

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  View    │───▶│ Confirm  │───▶│  Cook    │───▶│  Mark    │
│  Orders  │    │  Order   │    │  Order   │    │  Ready   │
└──────────┘    └──────────┘    └──────────┘    └──────────┘
```

#### Step-by-Step Guide

**Step 1: View Orders**
1. Login to Web Dashboard (Staff/Chef account)
2. Navigate to "Staff Dashboard" or "Kitchen Display"
3. View active orders (real-time via SignalR)

**Step 2: Confirm Order**
1. Review new order details
2. Tap "Confirm" to acknowledge
3. Status updates: PENDING → CONFIRMED

**Step 3: Cook Order**
1. Prepare food items
2. Tap "Start Cooking"
3. Status: CONFIRMED → COOKING

**Step 4: Mark Ready**
1. When food is ready
2. Tap "Mark Ready"
3. Status: COOKING → READY
4. Customer receives notification

### 3.4 Workflow 3: Manager Management Flow

**Purpose**: Manager administers restaurant operations via Web Dashboard.

#### Workflow Diagram

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Login   │───▶│ Manage   │───▶│  View    │───▶│  Manage  │
│          │    │  Menu    │    │ Reports  │    │  Staff   │
└──────────┘    └──────────┘    └──────────┘    └──────────┘
```

#### Step-by-Step Guide

**Step 1: Login**
1. Access Dashboard at http://localhost:5173
2. Login with Manager credentials
3. View Dashboard overview

**Step 2: Manage Menu**
1. Navigate to "Menu Management"
2. Add/Edit/Delete menu items
3. Manage categories
4. Toggle item availability

**Step 3: Manage Tables**
1. Navigate to "Table Management"
2. View table status (Available/Occupied/Reserved)
3. Create/Edit tables
4. Generate QR codes
5. View reservations

**Step 4: View Reports**
1. Navigate to "Transactions"
2. View payment history
3. Export reports

**Step 5: Manage Staff**
1. Navigate to "Staff Management"
2. Add/Edit staff accounts
3. Assign roles (Staff/Chef)

**Step 6: Robot Control (Optional)**
1. Navigate to "Restaurant Draw Map"
2. View/edit floor plan
3. Send robot commands
4. Monitor robot status

### 3.5 Workflow 4: Robot Delivery Flow (Optional)

**Purpose**: Robot delivers food from kitchen to table autonomously.

#### Workflow Diagram

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Staff   │───▶│  Robot   │───▶│  Robot   │───▶│  Robot   │───▶│  Robot   │
│ Commands │    │ Navigate │    │ Pick Up  │    │ Deliver  │    │  Return  │
└──────────┘    └──────────┘    └──────────┘    └──────────┘    └──────────┘
```

#### Step-by-Step Guide

**Step 1: Staff Triggers Delivery**
1. In Dashboard, select ready order
2. Click "Deliver with Robot"
3. System sends command via SignalR

**Step 2: Robot Navigates to Kitchen**
1. Robot plans path using DWA algorithm
2. Avoids obstacles via LiDAR
3. Arrives at kitchen pickup point

**Step 3: Robot Picks Up Food**
1. Robot waits at pickup
2. Staff places food on robot
3. Robot proceeds to table

**Step 4: Robot Delivers to Table**
1. Robot navigates to table location
2. Arrives at table
3. Staff/customer retrieves food

**Step 5: Robot Returns**
1. Robot returns to kitchen/charging station
2. Status updated to AVAILABLE

---

## Appendix: Quick Reference

### API Endpoints Summary

| Module | Endpoint | Method | Auth |
|--------|----------|--------|------|
| Auth | `/api/v1/auth/login` | POST | Public |
| Auth | `/api/v1/auth/register` | POST | Public |
| Auth | `/api/v1/auth/guest` | POST | Public |
| Auth | `/api/v1/auth/refresh` | POST | Public |
| Auth | `/api/v1/auth/logout` | POST | Auth |
| Menu | `/api/v1/menu` | GET | Public |
| Menu | `/api/v1/menu` | POST | Manager |
| Menu | `/api/v1/menu/{id}` | PUT | Manager |
| Menu | `/api/v1/menu/{id}` | DELETE | Manager |
| Orders | `/api/v1/orders` | POST | Customer |
| Orders | `/api/v1/orders/active` | GET | Staff |
| Orders | `/api/v1/orders/{id}/status` | PATCH | Staff |
| Payments | `/api/v1/payments/create-intent` | POST | Customer |
| Payments | `/api/v1/payments/webhook` | POST | PayOS |
| Tables | `/api/v1/tables` | GET | Staff |
| Tables | `/api/v1/tables/{id}/qr` | GET | Manager |
| Sessions | `/api/v1/dining-sessions` | POST | Customer |
| Robots | `/api/v1/robots/command` | POST | Staff |
| AI | `/api/v1/ai/recommendations` | GET | Customer |

### SignalR Hubs

| Hub | URL | Purpose |
|-----|-----|---------|
| OrderHub | `/hubs/orders` | Real-time order updates |
| RobotHub | `/hubs/robot` | Robot communication |

### Default Ports

| Service | Port |
|---------|------|
| Gateway | 5000 |
| Identity API | 5001 |
| Menu API | 5002 |
| Order API | 5003 |
| Table API | 5004 |
| AI API | 5005 |
| Map Server | 3001 |
| Web Dashboard | 5173 |
| Customer App (Web) | 8090 |
