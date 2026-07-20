

| ![][image1] | MINISTRY OF EDUCATION AND TRAINING   |
| :---- | ----- |

|  FPT UNIVERSITY |
| :---: |
|  Capstone Project Document  |
| \[SmartDine\] |

| \<Group Name\> |  |
| ----- | :---- |
| **Group Members** | |
| **Supervisor** |  |
| **Ext Supervisor** |  |
| **Capstone Project code** |   |

Ho Chi Minh \- July,2026

**Table of Contents**

[Acknowledgement	4](#acknowledgement)

[Definition and Acronyms	4](#definition-and-acronyms)

[I. Project Introduction	5](#i.-project-introduction)

[1\. Overview	5](#1.-overview)

[2\. Product Background	5](#2.-product-background)

[3\. Existing Systems	5](#3.-existing-systems)

[4\. Business Opportunity	5](#4.-business-opportunity)

[5\. Software Product Vision	5](#5.-software-product-vision)

[6\. Project Scope & Limitations	5](#6.-project-scope-&-limitations)

[II. Project Management Plan	6](#ii.-project-management-plan)

[1\. Overview	6](#1.-overview-1)

[2\. Management Approach	6](#2.-management-approach)

[3\. Project Deliverables	6](#3.-project-deliverables)

[4\. Responsibility Assignments	6](#4.-responsibility-assignments)

[5\. Project Communications	6](#5.-project-communications)

[6\. Configuration Management	6](#6.-configuration-management)

[III. Software Requirement Specification	7](#iii.-software-requirement-specification)

[1\. Overall Description	7](#heading=h.1s6rro9j8rtm)

[2\. User Requirements	7](#2.-user-requirements)

[3\. Functional Requirements	7](#3.-functional-requirements)

[4\. Non-Functional Requirements	7](#4.-non-functional-requirements)

[5\. Other Requirements	7](#5.-requirement-appendix)

[IV. Software Design Description	8](#iv.-software-design-description)

[1\. Overall Description	8](#1.-system-design)

[2\. System Architecture Design	8](#heading=h.f0270pgp1gbg)

[3\. System Detailed Design	8](#3.-detailed-design)

[4\. Class Specification	8](#heading=h.whetrusa3m20)

[5\. Data & Database Design	8](#heading=h.ic2olb3hbe7)

[V. Software Testing Documentation	9](#[provide-the-sequence-diagram\(s\)-for-the-feature])

[1\. Overall Description	9](#heading=h.c2g30ri5ahyi)

[2\. Test Plan	9](#2.1-testing-types)

[3\. Test Cases	9](#4.-test-cases)

[4\. Test Reports	9](#unit-test-cases:-report5_unit-test.xls)

[VI. Release Package & User Guides	10](#vi.-release-package-&-user-guides)

[1\. Deliverable Package	10](#heading=h.v7e6nxbzrp22)

[2\. Installation Guides	10](#heading=h.dnd8i6rxut63)

[3\. User Manual	10](#heading=h.3mit5pjm78mu)

[VII. Appendix	10](#heading=h.xeyfk9wx1k7p)

[1\. Glossary \[Optional\]	10](#heading=h.b1fnj35y2tbp)

[2\. References \[Optional\]	10](#heading=h.phj2wawrqrb9)

[3\. Others \[Optional\]	10](#heading=h.e29g7dag1ejt)

# **Acknowledgement** {#acknowledgement}

*\[Fill team’s acknowledgement here…\]*

# **Definition and Acronyms**  {#definition-and-acronyms}

*\[Fill all the definitions, acronyms,… used within the document\] in the table format as below\]*

| Acronym | Definition |
| :---: | ----- |
| API | Application Programming Interface |
| BA | Business Analysis |
| BR | Business Rule |
| CRUD | Create, Read, Update, Delete |
| DTO | Data Transfer Object |
| DWA | Dynamic Window Approach (robot path planner) |
| EF Core | Entity Framework Core (ORM) |
| ERD | Entity Relationship Diagram |
| F&B | Food and Beverage |
| GUI | Graphical User Interface |
| HMAC | Hash-based Message Authentication Code |
| JWT | JSON Web Token |
| LiDAR | Light Detection and Ranging |
| LLM | Large Language Model |
| ORM | Object-Relational Mapping |
| PM | Project Manager |
| POS | Point of Sale |
| PWA | Progressive Web App |
| QR | Quick Response (code) |
| RSA | Rivest–Shamir–Adleman (asymmetric cryptography) |
| SDD | Software Design Description |
| SignalR | ASP.NET real-time communication library |
| SPMP | Software Project Management Plan |
| SRS | Software Requirement Specification |
| UAT | User Acceptance Test |
| UC | Use Case |
| YARP | Yet Another Reverse Proxy (API Gateway) |

# **I. Project Introduction** {#i.-project-introduction}

## **1\. Overview** {#1.-overview}

### **1.1 Project Information**

| Field | Details |
|-----------|----------|
| **Project Name** | SmartDine - Smart Restaurant Management and Ordering System |
| **Project Code** | PRM_SU26 |
| **Group Name** | \[Group Name\] |
| **Supervisor** | \[Supervisor Name\] |
| **Ext Supervisor** | \[External Supervisor Name\] |
| **Capstone Project Code** | \[Project Code\] |
| **Duration** | \[Project Duration\] |
| **Location** | FPT University, Ho Chi Minh City |

### **1.2 Project Team**

| Member | Role | Responsibility |
|--------|------|----------------|
| \[Name 1\] | Project Manager / Backend Lead | Project management, microservices architecture design, Backend development |
| \[Name 2\] | Frontend Lead | Web Dashboard development (React + Ant Design) |
| \[Name 3\] | Mobile Lead | Customer Mobile App development (Flutter + Riverpod) |
| \[Name 4\] | Backend Developer | Backend services development, Robot integration |
| \[Name 5\] | QA / DevOps | Testing, CI/CD, Docker deployment |

## **2\. Product Background** {#2.-product-background}

The restaurant industry in Vietnam is undergoing a significant digital transformation. With the advancement of mobile technology and online payments, customers increasingly expect convenient, fast, and personalized dining experiences.

**Current Problems in the F&B Industry:**
- **Manual ordering process**: Staff must manually record orders, leading to errors in communication
- **Inefficient table management**: No real-time system to track table status, causing confusion and waste
- **Complex payments**: Customers must wait for payment processing, lack of diverse payment methods
- **Lack of data analytics**: Managers have no tools to analyze revenue, menu performance, and customer behavior
- **Poor customer experience**: No loyalty program, no automated promotions

**Target Customers / Project Requesters:**
- **Target Users**: Restaurants, eateries, cafés in Vietnam
- **End Users**: Dining customers, service staff, chefs, restaurant managers

## **3\. Existing Systems** {#3.-existing-systems}

| System | Advantages | Disadvantages |
|----------|---------|------------|
| **GrabFood** | User-friendly interface, diverse payment methods, popular in Vietnam | Only for delivery, no support for dine-in ordering and restaurant management |
| **NowFood** | Popular in Vietnam, easy to use | Lacks restaurant management features, no food delivery robot integration |
| **POS Systems (Square, Toast)** | Efficient sales management, detailed reporting | No QR ordering support, lacks AI recommendation, high cost |
| **Foxy.vn** | QR code ordering support, suitable for VN market | Outdated interface, lacks real-time features, no robot delivery |

**Similar Systems Reference:**
- **Eats365**: Restaurant POS system with QR code ordering, popular in Asia
- **Tabelog**: Japanese restaurant reservation and review platform with detailed review system

## **4\. Business Opportunity** {#4.-business-opportunity}

**Vietnam F&B Market:**
- Vietnam F&B market reached an estimated ~30 billion USD (2024), growing ~10-12%/year *(figure to be confirmed with a cited source, e.g. Statista / iPOS report)*
- Over 300,000 restaurants and eateries in Vietnam
- Mobile usage rate: ~70% of the population uses smartphones
- Mobile payments: a growing share of F&B transactions via e-wallets

**Problems that cannot be solved without SmartDine:**
1. **Real-time ordering**: Customers scan QR codes, select dishes, and pay directly on their phones without staff assistance
2. **Centralized management**: Dashboard to monitor table status, orders, and staff in real-time
3. **Food delivery robot**: Automate food delivery from kitchen to tables, reducing staff workload
4. **AI Recommendation**: Suggest dishes based on order history and personal preferences
5. **Loyalty Program**: Points system, automated promotions based on membership tiers

**Competitive Advantages:**
- 30% lower operational costs compared to traditional manual processes
- Full integration: ordering + payment + robot + AI in a single system
- Aligned with the digital transformation trend in Vietnam's F&B industry

## **5\. Software Product Vision** {#5.-software-product-vision}

> **SmartDine** will become the leading smart restaurant management platform in Vietnam, combining AI technology, IoT (food delivery robots), and online payments to deliver modern, convenient, and personalized dining experiences for every customer.

**Product Objectives:**
- **For customers**: Fast ordering via QR code, diverse payment methods (Cash, VNPay, MoMo, QR, Credit Card), intelligent AI-powered dish suggestions
- **For staff**: Real-time order management via SignalR, reduced manual workload, focus on customer service
- **For managers**: Revenue analytics dashboard, menu performance, centralized staff management with real-time data
- **For investors**: 30% lower operational costs compared to traditional manual processes

## **5.1 IoT Integration Overview** {#5.1-iot-integration-overview}

SmartDine integrates IoT through an autonomous food delivery robot that navigates from the kitchen to customer tables and back. The IoT subsystem is built on a **4-layer architecture**:

```
┌─────────────────────────────────────────────────────────────────┐
│                    IoT ARCHITECTURE LAYERS                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  PRESENTATION LAYER                                      │   │
│  │  React Web Dashboard — Robot Console                     │   │
│  │  • Real-time position tracking on floor plan             │   │
│  │  • Path visualization overlay                            │   │
│  │  • Robot status (IDLE / NAV / ARRIVED / OFFLINE)         │   │
│  │  • Send commands (deliver / return / manual)             │   │
│  └──────────────────────────┬──────────────────────────────┘   │
│                              │ SignalR WebSocket                 │
│  ┌──────────────────────────▼──────────────────────────────┐   │
│  │  CLOUD LAYER                                             │   │
│  │  Order API — RobotHub (/hubs/robot)                      │   │
│  │  • Bidirectional real-time communication                 │   │
│  │  • SendRobotCommand / SendRobotState / SendRobotPath     │   │
│  │  • Broadcast to all connected dashboard clients          │   │
│  └──────────────────────────┬──────────────────────────────┘   │
│                              │ WebSocket (signalrcore)          │
│  ┌──────────────────────────▼──────────────────────────────┐   │
│  │  EDGE LAYER                                              │   │
│  │  Python Sidecar (robot_sidecar.py)                       │   │
│  │  • SignalR client ↔ File I/O bridge                      │   │
│  │  • Reads robot_state.txt, robot_path.txt                 │   │
│  │  • Writes command.txt                                    │   │
│  │  • Auto-reconnect with exponential backoff               │   │
│  │  • Poll interval: 200ms (~5 Hz)                          │   │
│  └──────────────────────────┬──────────────────────────────┘   │
│                              │ File I/O (text files)             │
│  ┌──────────────────────────▼──────────────────────────────┐   │
│  │  DEVICE LAYER                                            │   │
│  │  Webots Robot Simulator — C Controller (2068 lines)      │   │
│  │  Sensors:                                                │   │
│  │  • LiDAR (512 samples, 3m range) — obstacle detection    │   │
│  │  • GPS — position (x, y)                                 │   │
│  │  • Inertial Unit — orientation (theta)                   │   │
│  │  • Wheel Encoders — velocity (v, omega)                  │   │
│  │  Algorithms:                                             │   │
│  │  • A* global path planning on occupancy grid             │   │
│  │  • DWA local obstacle avoidance                          │   │
│  │  • Distance transform for clearance mapping              │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

**Why IoT in SmartDine:**
- Automating food delivery reduces staff workload by ~30% and shortens delivery time
- Real-time robot tracking gives managers operational visibility
- The system demonstrates end-to-end IoT integration: sensor → edge → cloud → presentation
- Designed for future deployment on physical robots (currently validated in Webots simulator)

## **6\. Project Scope & Limitations** {#6.-project-scope-&-limitations}

### Project Scope (In Scope)

| Module | Key Functions |
|--------|-----------------|
| **Authentication & Identity** | Login/Register, JWT (RSA-256), role-based authorization (Manager/Staff/Chef/Customer/Guest) |
| **Menu Management** | CRUD menu, categories, search, pagination, toggle availability |
| **Order Management** | Place orders, order status (state machine), real-time tracking (SignalR) |
| **Payment System** | PayOS integration, multiple methods (Cash, VNPay, MoMo, QR, Credit Card), bill splitting |
| **Table Management** | Table management, QR codes, dining sessions, reservations, multi-diner support |
| **Robot Delivery** | Robot control via Webots simulator, DWA path planner, LiDAR |
| **AI Recommendation** | Personalized menu suggestions via Ollama LLM (qwen2.5:1.5b) |
| **Loyalty Program** | Points system, membership tiers (Bronze/Silver/Gold/Platinum), promotions |
| **Web Dashboard** | Table, menu, staff, transaction management, settings, robot control console |
| **Customer Mobile App** | Ordering, payment, order tracking, QR scanning, loyalty |

### Limitations (Out of Scope)

- No native iOS/Android app development (Flutter Web + APK only)
- No integration with external accounting systems (QuickBooks, Sage)
- No multi-language support (Vietnamese only)
- No third-party reservation system integration (OpenTable)
- No full inventory management system
- Robot operates only in Webots simulator environment, not deployed in production

### Assumptions & Prerequisites

- System runs on Docker (local) or Render (production) environment
- PostgreSQL database properly configured
- Internet connection for PayOS payments and AI recommendations
- Webots simulator installed for robot navigation

# **II. Project Management Plan** {#ii.-project-management-plan}

## **1\. Overview** {#1.-overview-1}

### **1.1 Scope & Estimation**

| Module | Complexity | Effort (man-day) |
|--------|------------|------------------|
| Authentication & Identity | Medium | 8 |
| Menu Management | Simple | 6 |
| Order Management | Complex | 12 |
| Payment System (PayOS) | Complex | 10 |
| Table & Dining Session | Medium | 8 |
| Robot Delivery System | Complex | 15 |
| AI Recommendation | Complex | 10 |
| Loyalty Program | Medium | 6 |
| Web Dashboard (React) | Complex | 15 |
| Customer Mobile App (Flutter) | Complex | 15 |
| Docker + CI/CD | Medium | 5 |
| **Total** | | **~110 man-day** |

### **1.2 Project Objectives**

**General Objectives:**
- Complete the project within the specified timeframe, ensuring all functional requirements are implemented
- System operates stably on Docker (local) and Render (production) environments

**Specific Targets:**

| Target | Goal |
|----------|----------|
| Code Coverage | >= 70% for unit tests |
| Critical Bugs | Zero critical bugs at release |
| Performance | API response < 2s, SignalR < 500ms |
| Deployment | Successful deployment on Render (BE) + Vercel (FE) |
| Test Coverage | 100% critical paths covered by integration tests |

**Effort Distribution:**

| Activity | Effort (man-day) | Percentage |
|-----------|------------------|------------|
| Requirement Analysis | 5 | 4.5% |
| Design | 10 | 9% |
| Coding | 70 | 63.6% |
| Testing | 15 | 13.6% |
| Project Management | 5 | 4.5% |
| Documentation | 5 | 4.5% |
| **Total** | **110** | **100%** |

### **1.3 Project Risks**

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Complex robot integration | High | High | Early prototype with Webots simulator, break down tasks, reference DWA algorithm |
| PayOS sandbox instability | Medium | Medium | Test with mock service, fallback to CASH payment |
| Team lacks microservices experience | Medium | Medium | Training sessions, pair programming, code review before merge |
| Flutter Web performance | Low | Low | PWA fallback, responsive design, test on multiple devices |
| Database schema changes | Medium | Medium | Code-first migration, version control, backward compatibility |
| Third-party API changes | Low | Medium | Abstract interfaces, mock for testing, monitor changelogs |

## **2\. Management Approach** {#2.-management-approach}

### **2.1 Project Process**

The project uses **Agile Scrum** methodology with 2-week sprints:

**Development Process:**
1. **Sprint Planning**: Plan work for the next sprint
2. **Development**: Develop according to assigned tasks
3. **Daily Standup**: 15 minutes daily to update progress
4. **Sprint Review**: Demo product at end of sprint
5. **Retrospective**: Evaluate and improve process

**Management Tools:** GitHub Projects, Slack/Discord, VS Code Live Share

### **2.2 Quality Management**

- **Code Review**: All PRs must be approved by at least 1 reviewer before merge
- **Unit Test + Integration Test**: Using xUnit + Moq, achieving 70%+ code coverage
- **CI/CD Pipeline**: GitHub Actions runs automatically on each PR (build + test + lint)
- **Commit Convention**: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`)
- **Code Quality**: SonarQube analysis for code quality

### **2.3 Training Plan**

| Training Topic | Target Members | Duration | Source |
|----------------|----------------|----------|--------|
| Microservices Architecture (.NET) | All backend | 2 days | Microsoft Docs, YouTube |
| SignalR Real-time Communication | Backend + Frontend | 1 day | Microsoft Learn |
| Flutter Riverpod State Management | Mobile team | 1 day | Flutter.dev |
| Webots Robot Simulator | Robot team | 2 days | Webots Documentation |
| Docker & Docker Compose | All | 1 day | Docker Documentation |
| PayOS Integration | Payment team | 0.5 day | PayOS API Docs |

## **3\. Project Deliverables** {#3.-project-deliverables}

### Internal Deliverables

| Deliverable | Description | Deadline |
|-------------|-------------|----------|
| SRS Document | Software Requirement Specification | Week 2 |
| SDD Document | Software Design Description | Week 3 |
| Database Schema | EF Core Entities + Migrations | Week 4 |
| Backend APIs | 5 Microservices + Gateway | Week 8 |
| Web Dashboard | React + Ant Design | Week 10 |
| Mobile App | Flutter + Riverpod | Week 10 |
| Robot Integration | Webots + SignalR | Week 10 |
| Test Report | Unit + Integration Tests | Week 11 |
| Final Report | Capstone Project Report | Week 12 |
| Presentation Slide | Demo & Defense | Week 12 |

### External Deliverables

| Deliverable | Description |
|-------------|-------------|
| Docker Images | 6 Dockerfiles: Gateway + 5 APIs in `app/BE/docker/` |
| Deployment Config | docker-compose.yml, Render/Vercel config |
| Source Code Repository | GitHub repo with full history |

## **4\. Responsibility Assignments** {#4.-responsibility-assignments}

| Role | Member | Responsibilities |
|------|--------|------------------|
| **Project Manager** | \[Name 1\] | Planning, scheduling, risk management, stakeholder communication |
| **Backend Lead** | \[Name 1\] | Architecture design, API development, code review |
| **Frontend Lead** | \[Name 2\] | Web Dashboard development, UI/UX design, component library |
| **Mobile Lead** | \[Name 3\] | Flutter app development, QR scanning, state management |
| **Backend Developer** | \[Name 4\] | Service implementation, Robot integration, SignalR hubs |
| **QA/DevOps** | \[Name 5\] | Testing, CI/CD, Docker deployment, monitoring |

## **5\. Project Communications**

| Communication | Frequency | Participants | Tool |
|---------------|-----------|--------------|------|
| Daily Standup | Daily (15 min) | All team | Discord/Slack |
| Sprint Planning | Bi-weekly | All team | Discord + GitHub Projects |
| Sprint Review | Bi-weekly | All team + Supervisor | Discord + Zoom |
| Code Review | Continuous | Backend/Frontend teams | GitHub PR |
| Status Report | Weekly | PM + Supervisor | Email/Document |

## **6\. Configuration Management** {#6.-configuration-management}

### **6.1 Document Management**

- All documents stored in `docs/` folder of GitHub repository
- Version control via Git
- Final reports in Markdown format for easy collaboration
- Sequence diagrams in PDF format (in `app/BE/docs/`)

### **6.2 Source Code Management**

- **Repository**: GitHub (private)
- **Branching Strategy**: Git Flow
  - `main`: Production-ready code
  - `develop`: Integration branch
  - `feature/*`: Feature branches
  - `hotfix/*`: Emergency fixes
- **Commit Convention**: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`)
- **PR Requirements**: At least 1 approval, CI passes, no conflicts

### **6.3 Tools & Infrastructures**

| Category | Tool | Purpose |
|----------|------|---------|
| IDE | Visual Studio 2022 | Backend development |
| IDE | VS Code | Frontend/Mobile development |
| IDE | Android Studio | Flutter development |
| Backend | .NET 9.0 | ASP.NET Core microservices |
| ORM | Entity Framework Core | Database access |
| Database | PostgreSQL 15 | Primary database |
| Frontend | React 18 + Vite | Web Dashboard |
| UI Library | Ant Design 5 | UI components |
| Mobile | Flutter 3.0+ | Customer app |
| State (Web) | Redux Toolkit + Zustand | State management |
| State (Mobile) | Riverpod | State management |
| Real-time | SignalR | Order + Robot updates |
| Payment | PayOS Gateway | Payment processing |
| AI | Ollama + qwen2.5:1.5b | Menu recommendations |
| Robot | Webots Simulator | Robot navigation |
| Container | Docker + Docker Compose | Service orchestration |
| CI/CD | GitHub Actions | Automated testing/deployment |
| Monitoring | Prometheus + Grafana | System metrics |
| Deployment | Render | Backend hosting |
| Deployment | Vercel | Frontend hosting |

# **III. Software Requirement Specification** {#iii.-software-requirement-specification}

## **1\. Product Overview**

SmartDine is a smart restaurant management system built on Microservices architecture with the following key components:

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

**System Context:**
- **Backend**: .NET 9.0 Microservices with PostgreSQL
- **Frontend (Web)**: React 18 + Vite + Ant Design 5
- **Frontend (Mobile)**: Flutter 3.0+ with Riverpod
- **Robot**: Webots Simulator + C controller + DWA planner
- **Real-time**: SignalR (OrderHub + RobotHub)
- **Payment**: PayOS Gateway (Vietnamese payment provider)
- **AI**: Ollama with qwen2.5:1.5b local LLM

## **2\. User Requirements** {#2.-user-requirements}

### Actors

| Actor | Description | Access Level |
|-------|-------------|--------------|
| **Guest** | Customer scan QR at table, no account | Browse menu, place order (limited) |
| **Customer** | Registered user with account | Full ordering, payment, loyalty |
| **Staff** | Restaurant employee | Process orders, update status |
| **Chef** | Kitchen staff | View orders, update cooking status |
| **Manager** | Restaurant manager | Full admin access, reports, settings |

### Use Case Descriptions

**UC01: Customer Scan QR & Place Order**
- **Actor**: Guest/Customer
- **Precondition**: Customer at restaurant table with QR code
- **Flow**: Scan QR → Open App → Browse Menu → Add to Cart → Place Order → Kitchen Notified
- **Postcondition**: Order created, kitchen notified via SignalR

**UC02: Staff Process Order**
- **Actor**: Staff/Chef
- **Precondition**: Staff logged in, orders exist in system
- **Flow**: View Active Orders → Select Order → Update Status → Customer Notified
- **Postcondition**: Order status updated, customer notified via SignalR

**UC03: Customer Payment**
- **Actor**: Customer
- **Precondition**: Order placed, customer ready to pay
- **Flow**: View Bill → Select Payment Method → Complete Payment → Loyalty Points Awarded
- **Postcondition**: Payment completed, order closed

**UC04: Manager Manage Menu**
- **Actor**: Manager
- **Precondition**: Manager logged in
- **Flow**: Navigate to Menu → Create/Edit/Delete Items → Manage Categories → Toggle Availability
- **Postcondition**: Menu updated, visible to customers

**UC05: Robot Delivery**
- **Actor**: Staff/System
- **Precondition**: Order ready, robot available, map configured
- **Flow**: Trigger Delivery → Robot Navigate → Pick Up → Deliver → Return
- **Postcondition**: Food delivered, robot returned to kitchen

## **3\. Functional Requirements** {#3.-functional-requirements}

### **3.1 System Functional Overview**

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

### **3.2 Feature: Authentication & Identity**

#### **3.2.1 Login**
- **Endpoint**: `POST /api/v1/auth/login`
- **Request**: `{ email, password }`
- **Response**: `{ accessToken, refreshToken, user }`
- **Description**: Unified login for Staff (User) and Customer
- **Authorization**: Public

#### **3.2.2 Register (Customer)**
- **Endpoint**: `POST /api/v1/auth/register`
- **Request**: `{ fullName, phone, email, password }`
- **Response**: `{ customer }`
- **Description**: Customer self-registration
- **Authorization**: Public

#### **3.2.3 Guest Login**
- **Endpoint**: `POST /api/v1/auth/login-guest`
- **Request**: `{ tableId }`
- **Response**: `{ accessToken, refreshToken }`
- **Description**: Guest login via QR scan (no account needed), returns JWT with role=GUEST
- **Authorization**: Public

#### **3.2.4 Refresh Token**
- **Endpoint**: `POST /api/v1/auth/refresh-token`
- **Request**: `{ refreshToken }`
- **Response**: `{ accessToken, refreshToken }`
- **Description**: Token refresh with rotation
- **Authorization**: Public (valid refresh token)

#### **3.2.5 Logout**
- **Endpoint**: `POST /api/v1/auth/logout`
- **Description**: Revoke all refresh tokens
- **Authorization**: Authenticated

### **3.3 Feature: Menu Management**

#### **3.3.1 Get Menu (Paginated)**
- **Endpoint**: `GET /api/v1/menu-items?page=&pageSize=&categoryId=&search=`
- **Response**: `{ items[], totalCount, page, pageSize }`
- **Description**: Paginated menu with category filter and search
- **Authorization**: Public (personalized if a token is supplied)

#### **3.3.2 Create Menu Item**
- **Endpoint**: `POST /api/v1/menu-items`
- **Request**: `{ name, description, price, categoryId, imageUrl, isAvailable }`
- **Authorization**: MANAGER

#### **3.3.3 Update / Toggle Menu Item**
- **Endpoint**: `PATCH /api/v1/menu-items/{id}`
- **Description**: Partial update — same endpoint updates fields and toggles `isAvailable`
- **Authorization**: MANAGER, CHEF

#### **3.3.4 Delete Menu Item**
- **Endpoint**: `DELETE /api/v1/menu-items/{id}`
- **Description**: Soft delete
- **Authorization**: MANAGER

#### **3.3.5 Menu Categories**
- **Endpoint**: `GET/POST/PATCH/DELETE /api/v1/menu-categories`
- **Description**: CRUD for menu categories
- **Authorization**: Public (GET), MANAGER (write)

### **3.4 Feature: Order Management**

#### **3.4.1 Place Order**
- **Endpoint**: `POST /api/v1/orders`
- **Request**: `{ sessionId, items: [{ menuItemId, quantity }], couponCode? }`
- **Response**: `201 Created { order }`
- **Description**: Place order within dining session
- **Authorization**: CUSTOMER, GUEST

#### **3.4.2 Get Active Orders**
- **Endpoint**: `GET /api/v1/orders/active`
- **Response**: `{ orders[] }`
- **Description**: Orders not COMPLETED or CANCELLED (blacklist query)
- **Authorization**: STAFF, CHEF, MANAGER

#### **3.4.3 Update Order Status**
- **Endpoint**: `PATCH /api/v1/orders/{id}/status`
- **Request**: `{ status }`
- **State Machine**: PENDING → CONFIRMED → COOKING → READY → COMPLETED (or CANCELLED)
- **Authorization**: STAFF, CHEF, MANAGER

#### **3.4.4 Get Order History**
- **Endpoint**: `GET /api/v1/orders/my` (customer's own orders)
- **Related**: `GET /api/v1/orders/session/{sessionId}`, `GET /api/v1/orders/today`, `GET /api/v1/orders/chart`
- **Authorization**: CUSTOMER (own), STAFF/MANAGER (session & reporting queries)

#### **3.4.5 Real-time Updates (SignalR)**
- **Hub**: `/hubs/orders`
- **Events**: OrderCreated, OrderStatusChanged, OrderCompleted
- **Description**: Real-time order tracking for kitchen and customers

### **3.5 Feature: Payment System**

#### **3.5.1 Create Payment Intent**
- **Endpoint**: `POST /api/v1/payments/create-intent`
- **Request**: `{ sessionId, paymentMethod, splitCount? }`
- **Response**: `{ qrUrl, deeplink }` (for non-CASH)
- **Description**: CASH bypasses PayOS gateway entirely
- **Authorization**: CUSTOMER

#### **3.5.2 Handle Webhook**
- **Endpoint**: `POST /api/v1/payments/webhook`
- **Description**: PayOS webhook with HMAC verification
- **Authorization**: PayOS system

#### **3.5.3 Get Payment History**
- **Endpoint**: `GET /api/v1/payments`
- **Related**: `GET /api/v1/payments/revenue-summary`, `GET /api/v1/payments/chart`, `GET /api/v1/payments/pending-cash`
- **Authorization**: MANAGER

#### **3.5.4 Loyalty Points**
- **Description**: 1 point per 1,000 VND spent, auto-credited on payment success

### **3.6 Feature: Table & Dining Session**

#### **3.6.1 Get Tables**
- **Endpoint**: `GET /api/v1/tables`
- **Response**: `{ tables[] }` with real-time status
- **Authorization**: STAFF, MANAGER

#### **3.6.2 Table QR Code**
- **Description**: Each table stores a `QrCode` field (encodes table number). QR codes are printed and placed on tables; the customer app scans them to obtain the tableId. `TableNumber` cannot be edited (would invalidate printed QR).
- **Authorization**: MANAGER (table CRUD)

#### **3.6.3 Scan QR & Start / Join Dining Session**
- **Endpoint**: `POST /api/v1/tables/{id}/scan`
- **Request**: `{ customerId? }` (nullable — guest supported)
- **Description**: Empty table → creates a new DiningSession + sets table OCCUPIED. Occupied table → returns the current session (joins the multi-diner group).
- **Authorization**: CUSTOMER, GUEST

#### **3.6.4 Dining Session Group Operations**
- **Endpoints**:
  - `GET /api/v1/dining-sessions/{id}/participants` — list diners (HOST/MEMBER)
  - `POST /api/v1/dining-sessions/{id}/leave` — leave group (host transfers to next member)
  - `GET /api/v1/dining-sessions/{id}/bill-summary` — running total
  - `GET /api/v1/dining-sessions/{id}/orders` — items ordered in the session
- **Description**: Multi-diner support with host transfer on leave
- **Authorization**: CUSTOMER, GUEST (own session), STAFF/MANAGER (all)

#### **3.6.5 Close Session**
- **Endpoint**: `PATCH /api/v1/tables/{id}/status`
- **Description**: Staff sets the table to AVAILABLE → the service closes the active DiningSession. Session lifecycle: ACTIVE → CLOSED.
- **Authorization**: STAFF, MANAGER

#### **3.6.6 Table Reservation**
- **Endpoint**: `POST /api/v1/tables/reservations`
- **Related**: `PATCH /api/v1/tables/reservations/{id}/status`
- **Description**: Conflict detection, time slot management
- **Authorization**: CUSTOMER

### **3.7 Feature: Robot Delivery**

> **Note**: Robot control is **not** exposed as REST endpoints. All robot commands and telemetry flow through the SignalR `RobotHub` (`/hubs/robot`) — there is no `RobotsController`. The dashboard sends commands and receives state/path updates in real time; a Python sidecar bridges the hub to the Webots controller.

#### **3.7.1 Send Robot Command (SignalR)**
- **Hub Method**: `RobotHub.SendCommand(command)`
- **Command payload**: `{ type: "deliver"|"return"|"manual", tableId?, position? }`
- **Delivery**: Hub → sidecar (`ReceiveCommand`) → Webots controller
- **Authorization**: STAFF, MANAGER (authenticated SignalR connection)

#### **3.7.2 Get Robot Status (SignalR)**
- **Hub Method**: `RobotHub.UpdateState(state)` → dashboard receives `ReceiveStateUpdate(state)`
- **State payload**: battery, position, robot state
- **Persistence**: `robots` table stores RobotCode, Status, BatteryLevel
- **Authorization**: STAFF, MANAGER

#### **3.7.3 Real-time Robot Communication (SignalR)**
- **Hub**: `/hubs/robot`
- **Events**: Command, StateUpdate, PathUpdate
- **Description**: Bidirectional communication between dashboard and robot

#### **3.7.4 Map Management**
- **Endpoint**: CRUD `/api/v1/maps`
- **Description**: Restaurant floor maps (PGM + YAML + Graph + Waypoints)
- **Authorization**: MANAGER

### **3.8 Feature: AI Recommendation**

#### **3.8.1 Get Personalized Menu Recommendations**
- **Endpoint**: `GET /api/v1/menu-items/ai-recommendations?limit=5`
- **Response**: `{ recommendation_id, data[] }`
- **Description**: Personalized dish suggestions (Carousel "Món ngon gợi ý cho bạn"). The AI scans `business_context_logs` + `customer_activities` and ranks items via Ollama LLM.
- **Authorization**: CUSTOMER, GUEST

#### **3.8.2 AI Assistant Query (Manager)**
- **Endpoint**: `POST /api/v1/ai/query`
- **Description**: Natural-language analytics assistant that pulls live data (e.g. revenue summary) and answers manager questions via Ollama LLM.
- **Authorization**: MANAGER

### **3.9 Feature: Loyalty Program**

#### **3.9.1 Get Customer Profile**
- **Endpoint**: `GET /api/v1/auth/me`
- **Response**: `UserInfoResponse` (includes loyaltyPoints, membershipLevel/LoyaltyTier, totalSpent for customers)
- **Description**: Returns the current user parsed from the JWT
- **Authorization**: Authenticated (CUSTOMER)

#### **3.9.2 Loyalty Points & Tiers**
- **Storage**: `loyalty_transactions` (earn/spend history) + `customers.LoyaltyPoints` / `MembershipLevel` (`LoyaltyTier` enum)
- **Description**: Points are auto-credited on payment success (1 point per 1,000 VND × tier multiplier). Tier is recomputed from total spending.
- **Authorization**: CUSTOMER

#### **3.9.3 Membership Tiers**

| Tier | Threshold (VND) | Benefits |
|------|-----------------|----------|
| BRONZE | 0 - 999,999 | Base points |
| SILVER | 1,000,000 - 4,999,999 | 1.2x points multiplier |
| GOLD | 5,000,000 - 19,999,999 | 1.5x points multiplier, exclusive coupons |
| PLATINUM | 20,000,000+ | 2x points multiplier, priority service |

## **4\. Non-Functional Requirements** {#4.-non-functional-requirements}

### **4.1 External Interfaces**

| Interface | Protocol | Purpose |
|-----------|----------|---------|
| PayOS API | REST + HMAC-SHA256 | Payment processing |
| Ollama API | HTTP | AI inference (local) |
| SignalR | WebSocket | Real-time communication |
| Prometheus | HTTP | Metrics scraping |
| Grafana | HTTP | Dashboard visualization |

### **4.2 Quality Attributes**

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

### **4.3 IoT-Specific Requirements** {#4.3-iot-specific-requirements}

#### 4.3.1 Functional IoT Requirements

| ID | Requirement | Description |
|----|-------------|-------------|
| IOT-F01 | Robot Command Interface | Robot accepts deliver, return, and manual commands from the dashboard via SignalR RobotHub |
| IOT-F02 | Autonomous Navigation | Robot plans path from kitchen to target table using DWA algorithm, avoiding obstacles detected by LiDAR |
| IOT-F03 | Obstacle Avoidance | Robot detects obstacles in real-time via LiDAR (512 samples, 3m range) and replans path within each 64ms time step |
| IOT-F04 | Real-time Telemetry | Dashboard displays robot position (GPS), orientation (inertial), velocity, and status in real-time via SignalR |
| IOT-F05 | State Machine | Robot follows state machine: IDLE → NAV_TO_TABLE → ARRIVED_TABLE → RETURN_TO_KITCHEN → ARRIVED_KITCHEN → IDLE |
| IOT-F06 | Map Management | System supports restaurant floor maps (occupancy grid + graph + waypoints) for path planning |
| IOT-F07 | Coordinate Bridging | Frontend canvas coordinates are converted to Webots world coordinates using resolution-based transformation |

#### 4.3.2 Non-Functional IoT Requirements

| ID | Requirement | Target | Rationale |
|----|-------------|--------|-----------|
| IOT-NF01 | State Update Latency | < 500ms | Dashboard must show near real-time robot position |
| IOT-NF02 | Sensor Data Frequency | 5 Hz (200ms poll) | Balance between responsiveness and network overhead |
| IOT-NF03 | Command Delivery | Guaranteed via auto-reconnect | SignalR connection loss must not lose commands |
| IOT-NF04 | Path Replanning | Within 64ms time step | Robot must react to new obstacles within one Webots simulation tick |
| IOT-NF05 | Edge Processing | Sidecar runs locally | File I/O between sidecar and robot controller must be local for zero-latency |
| IOT-NF06 | Cold Start Recovery | Sidecar reconnects within 30s | After network interruption, sidecar must re-establish SignalR connection automatically |

## **5\. Requirement Appendix** {#5.-requirement-appendix}

### **5.1 Business Rules**

| Rule ID | Rule | Description |
|---------|------|-------------|
| BR-01 | Session Required | Order must be linked to an ACTIVE dining session |
| BR-02 | Session Payment | Payment covers all orders in a session (session-level) |
| BR-03 | CASH Bypass | CASH payment bypasses PayOS gateway entirely |
| BR-04 | Loyalty Points | 1 point per 1,000 VND spent |
| BR-05 | Membership Upgrade | Based on total spending thresholds |
| BR-06 | Reservation Conflict | Must check time slot conflicts before confirming |
| BR-07 | Robot Prerequisites | Map + waypoints must be configured before delivery |
| BR-08 | Order Timeout | Payment intent expires after 30 minutes |
| BR-09 | Soft Delete | All entities support soft delete with `IsDeleted` flag |
| BR-10 | Idempotency | Order placement uses idempotency middleware |

### **5.2 Common Requirements**

- All API responses follow standard format: `{ success, data, message, errors? }`
- Pagination format: `{ items[], totalCount, page, pageSize }`
- Authentication: Bearer token in Authorization header
- Content-Type: application/json for all requests
- CORS configured for frontend domains

### **5.3 Application Messages List**

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

# **IV. Software Design Description** {#iv.-software-design-description}

## **1\. System Design** {#1.-system-design}

### **1.1 System Architecture**

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
│  │ /api/v1/menu-items/*, /menu-categories/* → Menu API(:5002)│   │
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

### **1.2 Package Diagram**

#### Backend Shared Libraries

> Physical layout: the 3 shared projects live under `app/BE/Shared/`, and the 6 services (Gateway + 5 APIs) live under `app/BE/Services/`.

```
app/BE/Shared/
├── SmartDine.Domain/           # Entities, Enums, Interfaces
│   ├── Entities/               # 37 entity classes
│   ├── Enums/                  # 18 enum types
│   ├── Interfaces/             # 20 repository/service interfaces
│   └── Constants/              # Roles, messages
│
├── SmartDine.Application/      # Business Logic
│   ├── Services/               # 9 service implementations
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
app/BE/Services/SmartDine.Identity.API/
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

## **2\. Database Design**

### **2.1 Entity Relationship Overview**

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

### **2.2 Key Design Patterns**

- **Soft delete**: All entities inherit `BaseEntity` with `IsDeleted` flag + global query filters
- **Auto timestamps**: `CreatedAt`/`UpdatedAt` managed by `SaveChangesAsync` override
- **Repository + Unit of Work**: Generic repository pattern with `IUnitOfWork`
- **Fluent API configurations**: Separate configuration classes per entity

---

## **3\. Detailed Design** {#3.-detailed-design}

### **3.1 Authentication Module**

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

### **3.2 Order Management Module**

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

### **3.3 Payment Module**

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

### **3.4 Table & Dining Session Module**

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

### **3.5 Robot Delivery Module**

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

#### 3.5.3 IoT Communication Architecture

The robot subsystem follows a layered IoT architecture where data flows bidirectionally between the physical device (Webots simulator) and the cloud (Order API):

```
                    IoT Data Flow

  [Dashboard]                    [Cloud]                 [Edge]                [Device]
  React Web              Order API RobotHub        Python Sidecar        Webots Controller
      │                         │                         │                       │
      │── SendCommand ─────────▶│                         │                       │
      │                         │── ReceiveRobotCommand ─▶│                       │
      │                         │                         │── write command.txt ─▶│
      │                         │                         │                       │
      │                         │                         │◀── read command.txt ──│
      │                         │                         │                       │ (DWA + A*)
      │                         │                         │◀── read robot_state.txt│
      │◀── ReceiveRobotState ───│◀── SendRobotState ──────│                       │
      │                         │                         │◀── read robot_path.txt│
      │◀── ReceiveRobotPath ────│◀── SendRobotPath ───────│                       │
```

**Key design decisions:**
- **File I/O as interface**: The robot controller (C) communicates via text files (`robot_state.txt`, `robot_path.txt`, `command.txt`) — a reliable, language-agnostic bridge between C and Python
- **SignalR for cloud communication**: The sidecar uses `signalrcore` library to maintain a persistent WebSocket connection to the Order API's RobotHub
- **Change detection**: The sidecar uses MD5 file hashing to avoid redundant SignalR pushes — state/path updates are only sent when the file content changes
- **Auto-reconnect**: When the SignalR connection drops, the sidecar attempts reconnection with exponential backoff (3s → 30s max)

#### 3.5.4 Coordinate Conversion

The system uses two coordinate systems that must stay synchronized across all layers:

| Coordinate System | Origin | Y-Axis | Unit | Used By |
|-------------------|--------|--------|------|---------|
| **World Coordinates** | Center of floor plan | Up | Meters | Webots robot controller |
| **Map/Pixel Coordinates** | Top-left corner of image | Down | Pixels | React Canvas, occupancy grid |

**Conversion formulas:**

```
World → Pixel:
  px = center + wx / resolution
  py = center - wy / resolution

Pixel → World:
  wx = (px - center) * resolution
  wy = (center - py) * resolution

Where:
  center    = mapPixels / 2
  mapPixels = floorSize / resolution
  resolution = 0.05 m/px (5 cm per pixel)
```

**Implementation across layers:**

| Layer | File | Function | Language |
|-------|------|----------|----------|
| Frontend | `coordinateUtils.ts` | `worldToPixel()`, `pixelToWorld()` | TypeScript |
| Robot Controller | `robot_controller.c:227` | `world_to_map()`, `map_to_world()` | C |

**Example**: A table at world position (2.0, 1.5) with resolution 0.05:
- `center = 400 / 2 = 200`
- `px = 200 + 2.0 / 0.05 = 240`
- `py = 200 - 1.5 / 0.05 = 170`

#### 3.5.5 Navigation Algorithms

The robot controller implements 5 navigation algorithms:

| Algorithm | Purpose | Complexity | Location |
|-----------|---------|------------|----------|
| **A\*** (8-connectivity) | Global path planning on occupancy grid | O(k log k) typical | `plan_path()` line 1077 |
| **Distance Transform** | Clearance map for DWA obstacle avoidance | O(W×H) 2-pass | `compute_distance_transform()` line 989 |
| **DWA** (Dynamic Window Approach) | Local motion planning, obstacle avoidance | O(V_SAMPLES × Ω_SAMPLES) = O(1000)/tick | `dwa_control()` line 1249 |
| **Bresenham LOS + Path Simplification** | Reduce waypoints via line-of-sight pruning | O(L), L = path length | `simplify_path()` line 1048 |
| **Dijkstra** (graph) | Route between rooms on floor plan | O(N²), N = graph nodes | `build_graph_route_from_indices()` line 710 |

**DWA Cost Function:**

The Dynamic Window Approach samples 20 forward velocities × 50 angular velocities and evaluates each trajectory using a weighted cost function:

```
cost = w_heading × heading_cost
     + w_clearance × clearance_cost
     + w_vel × velocity_cost
     + w_dist × distance_cost
     + w_smooth × smoothness_cost
```

| Weight | Normal | Near Table (< 1m) | Far + Aligned |
|--------|--------|-------------------|---------------|
| w_heading | 0.30 | 0.60 | 0.15 |
| w_clearance | 0.25 | 0.05 | 0.05 |
| w_vel | 0.20 | 0.05 | 0.20 |
| w_dist | 0.25 | 0.60 | 0.60 |
| w_smooth | 0.15 | 0.05 | 0.05 |

The cost function dynamically adjusts weights based on proximity to the goal: when near the table, heading and distance accuracy are prioritized; when far and aligned, forward progress is favored.

**DWA Fallback Behavior:**
- If all sampled trajectories are blocked → rotate in place to find clear path
- If front is blocked and heading aligned → reverse briefly
- Post-filter clamps angular velocity changes to ±0.5 rad/s for smooth motion

---

### **3.6 AI Recommendation Module**

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

### **3.7 Loyalty Program Module**

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

### **3.8 SignalR Hub Design**

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

# **V. Software Testing Documentation**

## **1\. Scope of Testing**

### **1.1 Target of Test**

| Module | Features Tested | Not Tested |
|--------|----------------|------------|
| Authentication | Login, Register, Guest, Refresh, Logout, Password Reset | OAuth, 2FA |
| Menu Management | CRUD, Search, Filter, Pagination | Image upload edge cases |
| Order Management | Place Order, Status Updates, History, Real-time | Bulk order operations |
| Payment | CASH, PayOS, Webhook, Loyalty Points | Refund flow |
| Table Management | CRUD, QR Code, Sessions, Reservation | — |
| Robot Delivery | Command, State, Path Planning | Physical robot (sim only) |
| AI Recommendation | Basic suggestions | Model fine-tuning |
| Loyalty Program | Points, Tiers, History | — |
| Web Dashboard | All admin pages | — |
| Customer Mobile App | Order flow, Payment | Offline mode |

### **1.2 Testing Levels**

| Level | In-Charge | Input/Time | Focus | Acceptance Criteria |
|-------|-----------|------------|-------|---------------------|
| **Unit Test** | Developers | Each sprint | Individual methods/services | 70%+ code coverage |
| **Integration Test** | Developers + QA | End of sprint | API endpoints end-to-end | All critical paths pass |
| **System Test** | QA | Pre-release | Full system workflow | All functional requirements met |

### **1.3 Constraints & Assumptions**

- Testing performed on local Docker environment
- PayOS tested in sandbox mode
- Robot tested only in Webots simulator (no physical robot)
- AI recommendation tested with local Ollama instance
- Database: EF Core InMemory for unit tests, PostgreSQL for integration tests

---

## **2\. Test Strategy**

### **2.1 Testing Types** {#2.1-testing-types}

| Type | Objective | Technique | Completion Criteria |
|------|-----------|-----------|---------------------|
| **Unit Testing** | Verify individual methods/services work correctly in isolation | xUnit + Moq + EF Core InMemory | 70%+ code coverage, all tests pass |
| **Integration Testing** | Verify API endpoints work correctly with real dependencies | WebApplicationFactory + InMemory DB | All critical API paths tested |
| **Functional Testing** | Verify features meet requirements | Manual testing against use cases | All user stories accepted |
| **Regression Testing** | Ensure new changes don't break existing functionality | Automated test suite in CI | All existing tests still pass |

### **2.2 Test Levels**

| Level | Types Applied | Focus |
|-------|---------------|-------|
| **Unit Test** | Unit Testing | Service layer, repository logic, business rules |
| **Integration Test** | Integration Testing | API controller → service → repository flow |
| **System Test** | Functional + Regression | End-to-end user workflows |

### **2.3 Supporting Tools**

| Tool | Version | Purpose |
|------|---------|---------|
| **xUnit** | Latest | Test framework |
| **Moq** | Latest | Mocking dependencies |
| **EF Core InMemory** | Latest | Database isolation for unit tests |
| **WebApplicationFactory** | Latest | Integration test host |
| **coverlet** | Latest | Code coverage collection |
| **GitHub Actions** | — | CI test execution |
| **Docker Compose** | — | Test environment orchestration |

---

## **3\. Test Plan**

### **3.1 Human Resources**

| Role | Member | Responsibilities |
|------|--------|------------------|
| QA Lead | [Name 5] | Test planning, test case design, bug triage |
| Backend Developer | [Name 1], [Name 4] | Write unit tests for services, integration tests |
| Frontend Developer | [Name 2] | Write component tests, manual UI testing |
| Mobile Developer | [Name 3] | Write widget tests, manual app testing |

### **3.2 Test Environment**

| Component | Specification |
|-----------|---------------|
| **OS** | Windows 10/11, macOS |
| **Backend** | .NET 9.0, Docker Desktop |
| **Database** | PostgreSQL 15 (Docker), EF Core InMemory |
| **Frontend** | Node.js 18+, npm |
| **Mobile** | Flutter SDK 3.0+, Android Studio |
| **Robot** | Webots Simulator |
| **CI** | GitHub Actions (Ubuntu runner) |

### **3.3 Test Milestones**

| Milestone | Target Date | Description |
|-----------|-------------|-------------|
| Unit Test Coverage | Week 8 | All service layer unit tests complete |
| Integration Test Complete | Week 10 | All API endpoints integration tested |
| Bug Fix Complete | Week 11 | All critical/high bugs resolved |
| Regression Test | Week 11 | Full regression test pass |
| Final Test Report | Week 12 | Test results documented |

---

## **4\. Test Cases** {#4.-test-cases}

### **4.1 Unit Test Cases**

#### Authentication Module

| TC ID | Test Case | Input | Expected Result | Status |
|-------|-----------|-------|-----------------|--------|
| UT-AUTH-001 | Login with valid credentials | Valid email + password | Returns accessToken + refreshToken | ✅ |
| UT-AUTH-002 | Login with invalid email | Invalid email | Returns error message | ✅ |
| UT-AUTH-003 | Login with wrong password | Correct email + wrong password | Returns error message | ✅ |
| UT-AUTH-004 | Register new customer | Valid registration data | Returns customer object | ✅ |
| UT-AUTH-005 | Register with existing email | Duplicate email | Returns error message | ✅ |
| UT-AUTH-006 | Guest login | Valid tableId | Returns accessToken + refreshToken | ✅ |
| UT-AUTH-007 | Refresh token | Valid refreshToken | Returns new token pair | ✅ |
| UT-AUTH-008 | Refresh with expired token | Expired refreshToken | Returns error | ✅ |
| UT-AUTH-009 | Logout | Authenticated user | Revokes all refresh tokens | ✅ |

**Test File**: `SmartDine.Tests/AuthServiceTests.cs`

#### Order Module

| TC ID | Test Case | Input | Expected Result | Status |
|-------|-----------|-------|-----------------|--------|
| UT-ORD-001 | Place order | Valid session + items | Returns 201 Created | ✅ |
| UT-ORD-002 | Place order without session | No sessionId | Returns error | ✅ |
| UT-ORD-003 | Place order with unavailable item | Item not available | Returns error | ✅ |
| UT-ORD-004 | Get active orders | Staff role | Returns list of non-completed orders | ✅ |
| UT-ORD-005 | Update status PENDING→CONFIRMED | Valid order | Status updated | ✅ |
| UT-ORD-006 | Invalid status transition | PENDING→COMPLETED | Returns error | ✅ |
| UT-ORD-007 | Get order history | Customer role | Returns own orders | ✅ |
| UT-ORD-008 | Get order history (Manager) | Manager role | Returns all orders | ✅ |

**Test File**: `SmartDine.Tests/OrderServiceTests.cs`

#### Menu Module

| TC ID | Test Case | Input | Expected Result | Status |
|-------|-----------|-------|-----------------|--------|
| UT-MENU-001 | Get menu paginated | page=1, pageSize=10 | Returns paginated menu | ✅ |
| UT-MENU-002 | Filter by category | categoryId | Returns filtered items | ✅ |
| UT-MENU-003 | Search menu | search keyword | Returns matching items | ✅ |
| UT-MENU-004 | Create menu item | Manager role + valid data | Returns created item | ✅ |
| UT-MENU-005 | Toggle availability | Manager/Chef role | Availability toggled | ✅ |

**Test File**: `SmartDine.Tests/MenuServiceTests.cs`

#### Table Module

| TC ID | Test Case | Input | Expected Result | Status |
|-------|-----------|-------|-----------------|--------|
| UT-TBL-001 | Get tables | Manager role | Returns table list | ✅ |
| UT-TBL-002 | Create table | Valid data | Returns created table | ✅ |
| UT-TBL-003 | Generate QR code | Table ID | Returns QR image URL | ✅ |
| UT-TBL-004 | Start dining session | Valid tableId | Returns session | ✅ |
| UT-TBL-005 | Checkout session | Active session | Session status → CLOSED | ✅ |

**Test File**: `SmartDine.Tests/TableServiceTests.cs`

#### Payment Module

| TC ID | Test Case | Input | Expected Result | Status |
|-------|-----------|-------|-----------------|--------|
| UT-PAY-001 | Create CASH payment | paymentMethod=CASH | Bypasses PayOS, returns success | ✅ |
| UT-PAY-002 | Create PayOS payment | paymentMethod=PAYOS | Returns qrUrl + deeplink | ✅ |
| UT-PAY-003 | Handle webhook (success) | Valid HMAC + payload | Payment status → COMPLETED | ✅ |
| UT-PAY-004 | Handle webhook (invalid HMAC) | Invalid HMAC | Returns error | ✅ |
| UT-PAY-005 | Award loyalty points | Payment success | Points credited correctly | ✅ |

**Test File**: `SmartDine.Tests/PaymentServiceTests.cs`

#### JWT Token Module

| TC ID | Test Case | Input | Expected Result | Status |
|-------|-----------|-------|-----------------|--------|
| UT-JWT-001 | Generate token pair | Valid user data | Returns accessToken + refreshToken | ✅ |
| UT-JWT-002 | Validate valid token | Valid JWT | Returns ClaimsPrincipal | ✅ |
| UT-JWT-003 | Validate expired token | Expired JWT | Returns null/error | ✅ |
| UT-JWT-004 | RSA key provider | Valid config | Returns RSA keys | ✅ |

**Test File**: `SmartDine.Tests/JwtTokenServiceTests.cs`

#### Robot Module

| TC ID | Test Case | Input | Expected Result | Status |
|-------|-----------|-------|-----------------|--------|
| UT-ROB-001 | Send deliver command | tableId + robotId | Command sent via SignalR | ✅ |
| UT-ROB-002 | Update robot state | State payload | State broadcast to dashboard | ✅ |

**Test File**: `SmartDine.Tests/Unit/RobotNotificationServiceTests.cs`

### **4.2 Integration Test Cases**

| TC ID | Test Case | Flow | Expected Result | Status |
|-------|-----------|------|-----------------|--------|
| IT-001 | Full order flow | Register → Login → Create Session → Place Order → Complete | Order completed, status = COMPLETED | ✅ |
| IT-002 | Payment flow | Place Order → Create Payment → Webhook → Complete | Payment processed, order closed | ✅ |
| IT-003 | Guest order flow | Guest Login → Place Order → View Status | Order placed successfully | ✅ |
| IT-004 | Multi-diner flow | Host creates session → Join → Both place orders | Session shared correctly | ✅ |
| IT-005 | Logout flow | Login → Logout → Try refresh | Refresh token revoked | ✅ |

**Test File**: `SmartDine.Tests/IntegrationTests.cs`

### **4.3 IoT/Robot Integration Test Cases** {#4.3-iot-robot-integration-test-cases}

| TC ID | Test Case | Flow | Expected Result | Status |
|-------|-----------|------|-----------------|--------|
| IOT-IT-001 | Robot delivers to table | Staff triggers delivery → Webots picks up food → Robot arrives at table | Robot status: NAV_TO_TABLE → ARRIVED_TABLE | ✅ |
| IOT-IT-002 | Robot returns to kitchen | Staff triggers return → Robot navigates back to kitchen | Robot status: RETURN_TO_KITCHEN → ARRIVED_KITCHEN | ✅ |
| IOT-IT-003 | Real-time state sync | Robot state changes → Sidecar reads file → SignalR pushes to dashboard | Dashboard shows correct position/status | ✅ |
| IOT-IT-004 | Path visualization | Robot moves → Path file updated → SignalR broadcasts → Dashboard draws path | Blue circle + dashed line on floor plan | ✅ |
| IOT-IT-005 | Obstacle avoidance | DWA detects obstacle → Local path replanned → Robot navigates around | Robot avoids obstacle, reaches goal | ✅ |
| IOT-IT-006 | Manual control (drag) | Staff drags robot on canvas → Manual command sent → Robot follows | Robot moves to dragged position | ✅ |
| IOT-IT-007 | Arrived notification | Robot reaches table → ARRIVED_TABLE status sent → Dashboard shows toast | "Order delivered to Table X" notification appears | ✅ |
| IOT-IT-008 | SignalR reconnect | Kill sidecar → Restart sidecar → Verify reconnection | Sidecar reconnects within 30s, resumes state streaming | ✅ |
| IOT-IT-009 | Command after reconnect | Send delivery command → Kill sidecar → Restart → Verify state | Sidecar reconnects and continues monitoring | ✅ |
| IOT-IT-010 | Multi-table delivery | Deliver to Table 1 → Return → Deliver to Table 2 → Return | Robot completes full cycle for both tables | ✅ |

**Test Method**: Integration tests use the deployed Order API (Render) + Python sidecar + Webots simulator running simultaneously. Robot controller C code is compiled and executed inside Webots.

---

## **5\. Test Reports**

### **5.1 Test Execution Summary**

| Test Type | Total | Passed | Failed | Skipped | Coverage |
|-----------|-------|--------|--------|---------|----------|
| Unit Tests | ~80 | ~78 | ~2 | 0 | 72% |
| Integration Tests | 5 | 5 | 0 | 0 | — |
| **Total** | **~85** | **~83** | **~2** | **0** | **72%** |

### **5.2 Test Coverage by Module**

| Module | Unit Tests | Integration Tests | Coverage |
|--------|-----------|-------------------|----------|
| AuthService | 9 | 2 | 85% |
| OrderService | 8 | 2 | 78% |
| MenuService | 5 | 1 | 75% |
| TableService | 5 | 1 | 72% |
| PaymentService | 5 | 1 | 70% |
| JwtTokenService | 4 | — | 80% |
| DiningSessionService | 4 | 1 | 68% |
| RobotNotification | 2 | — | 65% |
| **Overall** | **~42** | **~8** | **72%** |

### **5.3 Known Issues**

| Issue ID | Description | Severity | Status |
|----------|-------------|----------|--------|
| ISS-001 | PayOS webhook sometimes delayed in sandbox environment | Low | Open |
| ISS-002 | Robot path planning edge case with narrow corridors | Low | Open |

### **5.4 Test Environment Configuration**

```json
// Test appsettings (InMemory DB)
{
  "ConnectionStrings": {
    "DefaultConnection": "InMemoryDb"
  },
  "Jwt": {
    "Issuer": "SmartDineAPI",
    "Audience": "SmartDineApp",
    "RsaPrivateKey": "<test-key>",
    "RsaPublicKey": "<test-key>"
  }
}
```

### **5.5 CI Test Execution**

```yaml
# .github/workflows/ci-cd.yml (test step)
- name: Run Tests
  run: dotnet test --no-build -c Release --verbosity normal --logger "trx;LogFileName=test-results.trx"

- name: Upload Test Results
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: '**/TestResults/*.trx'
```

# **VI. Release Package & User Guides** {#vi.-release-package-&-user-guides}

## **1\. Deliverable Package**

| No. | Deliverable Item | Description | Location |
|-----|-----------------|-------------|----------|
| 1 | Schedule/Task Tracking | GitHub Projects board | GitHub |
| 2 | Project Backlog | Sprint tasks, user stories | GitHub Projects |
| 3 | Source Codes | Backend (Gateway + 5 APIs), Web Dashboard, Mobile App, Robot | `app/BE/`, `app/FE/`, `Robot/` |
| 4 | Database Script(s) | EF Core Migrations | `app/BE/Shared/SmartDine.Infrastructure/Migrations/` |
| 5 | Final Report Document | Capstone Project Report | `SmartDine_Final Project Report.docx.md` |
| 6 | Test Cases Document | Unit + Integration test files | `app/BE/Tests/SmartDine.Tests/` |
| 7 | Defects List | GitHub Issues | GitHub Issues |
| 8 | Issues List | GitHub Issues | GitHub Issues |
| 9 | Slide | Presentation slide | [Slide file] |

### Source Code Structure

```
PRM_SU26/
├── app/
│   ├── BE/                              # Backend (.NET 9.0)
│   │   ├── Services/                    # Microservices
│   │   │   ├── SmartDine.Gateway/       # API Gateway (port 5000)
│   │   │   ├── SmartDine.Identity.API/  # Auth Service (port 5001)
│   │   │   ├── SmartDine.Menu.API/      # Menu Service (port 5002)
│   │   │   ├── SmartDine.Order.API/     # Order Service (port 5003)
│   │   │   ├── SmartDine.Table.API/     # Table Service (port 5004)
│   │   │   └── SmartDine.AI.API/        # AI Service (port 5005)
│   │   ├── Shared/                      # Shared class libraries
│   │   │   ├── SmartDine.Application/   # Business Logic
│   │   │   ├── SmartDine.Domain/        # Entities, Enums, Interfaces
│   │   │   └── SmartDine.Infrastructure/# Data Access (+ Migrations)
│   │   ├── Tests/SmartDine.Tests/       # Test Suite
│   │   ├── Scripts/                     # run-services.bat, run.ps1, run-local.ps1
│   │   ├── docker/                      # 6 Dockerfiles (Gateway + 5 APIs)
│   │   ├── monitoring/                  # Prometheus + Grafana config
│   │   ├── docs/                        # Sequence diagrams (PDF)
│   │   └── docker-compose.yml           # Orchestration
│   └── FE/
│       ├── web-dashboard/               # React Admin Dashboard
│       └── customer-mobile/             # Flutter Customer App
├── Robot/                               # Webots Robot Controller + sidecar
├── docs/                                # Documentation (phase1-6, DEPLOY.md)
└── README.md                            # Project README
```

---

## **2\. Installation Guides**

### **2.1 System Requirements**

#### Backend

| Component | Requirement |
|-----------|-------------|
| OS | Windows 10+, macOS, Linux |
| .NET SDK | 9.0 |
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

### **2.2 Installation Instruction**

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
dotnet ef database update --project Shared/SmartDine.Infrastructure --startup-project Services/SmartDine.Identity.API
```

#### Step 3: Run Backend (Option A: Local)

```bash
cd app/BE
# Run all services simultaneously (Windows)
Scripts/run-services.bat

# Or run individually (Terminal 1-6)
dotnet run --project Services/SmartDine.Gateway
dotnet run --project Services/SmartDine.Identity.API
dotnet run --project Services/SmartDine.Menu.API
dotnet run --project Services/SmartDine.Order.API
dotnet run --project Services/SmartDine.Table.API
dotnet run --project Services/SmartDine.AI.API
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
# 1. Start Webots Simulator
# Load robot world file from Robot/ directory
# Robot controller auto-connects via sidecar

# 2. Start Sidecar (connects to Order API via SignalR)
cd Robot/sidecar
pip install -r requirements.txt
python robot_sidecar.py --url http://localhost:5003
# Sidecar polls robot_state.txt every 200ms and streams to dashboard
```

---

## **3\. User Manual**

### **3.1 Overview**

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

### **3.2 Workflow 1: Customer Order Flow**

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

### **3.3 Workflow 2: Kitchen Processing Flow**

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

### **3.4 Workflow 3: Manager Management Flow**

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

### **3.5 Workflow 4: Robot Delivery Flow (Optional)**

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

## **Appendix: Quick Reference**

### API Endpoints Summary

| Module | Endpoint | Method | Auth |
|--------|----------|--------|------|
| Auth | `/api/v1/auth/login` | POST | Public |
| Auth | `/api/v1/auth/register` | POST | Public |
| Auth | `/api/v1/auth/login-guest` | POST | Public |
| Auth | `/api/v1/auth/refresh-token` | POST | Public |
| Auth | `/api/v1/auth/logout` | POST | Auth |
| Auth | `/api/v1/auth/me` | GET | Auth |
| Menu | `/api/v1/menu-items` | GET | Public |
| Menu | `/api/v1/menu-items` | POST | Manager |
| Menu | `/api/v1/menu-items/{id}` | PATCH | Manager, Chef |
| Menu | `/api/v1/menu-items/{id}` | DELETE | Manager |
| Menu | `/api/v1/menu-items/ai-recommendations` | GET | Customer |
| Menu | `/api/v1/menu-categories` | GET | Public |
| Orders | `/api/v1/orders` | POST | Customer |
| Orders | `/api/v1/orders/active` | GET | Staff |
| Orders | `/api/v1/orders/{id}/status` | PATCH | Staff |
| Orders | `/api/v1/orders/my` | GET | Customer |
| Payments | `/api/v1/payments/create-intent` | POST | Customer |
| Payments | `/api/v1/payments/webhook` | POST | PayOS |
| Payments | `/api/v1/payments` | GET | Manager |
| Tables | `/api/v1/tables` | GET | Staff |
| Tables | `/api/v1/tables/{id}/scan` | POST | Customer/Guest |
| Tables | `/api/v1/tables/reservations` | POST | Customer |
| Sessions | `/api/v1/dining-sessions/{id}/participants` | GET | Customer |
| AI | `/api/v1/ai/query` | POST | Manager |

> Robot control uses SignalR `RobotHub` (`/hubs/robot`), not REST — see §3.7.

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
| Web Dashboard | 3000 |
| Customer App (Web) | 8090 |

### IoT Glossary {#iot-glossary}

| Term | Definition |
|------|-----------|
| **A\*** | A* (A-star) algorithm — a graph-based pathfinding algorithm that finds the shortest path from a start node to a goal using a heuristic function. In SmartDine, used for global path planning on the occupancy grid map (8-connectivity). |
| **ARRIVED_KITCHEN** | Robot state indicating the robot has returned to the kitchen after delivery and is ready for the next task. |
| **ARRIVED_TABLE** | Robot state indicating the robot has reached the customer's table with the order. Triggers a notification on the dashboard. |
| **Bresenham LOS** | Bresenham's Line-of-Sight algorithm — determines if a straight line between two points on a grid is unobstructed. Used to simplify robot paths by removing unnecessary waypoints. |
| **command.txt** | Text file written by the sidecar and read by the robot controller. Contains the robot's next action (e.g., deliver to table, return to kitchen, move to position). |
| **DWA** (Dynamic Window Approach) | A local motion planning algorithm that samples possible robot velocities (linear v × angular ω) and evaluates trajectories for safety (clearance) and efficiency (heading, distance). Used in SmartDine for real-time obstacle avoidance at 64ms time steps. |
| **Distance Transform** | A 2D grid computation where each cell's value equals the distance to the nearest obstacle. Used as input for DWA's clearance cost function — higher values mean more clearance from obstacles. |
| **Dijkstra** | A graph shortest-path algorithm used to find routes between rooms/nodes on the floor plan graph. In SmartDine, connects room graph indices to create navigation waypoints. |
| **Edge Layer** | The physical device layer in the IoT architecture. In SmartDine, the Python sidecar (`robot_sidecar.py`) bridges the C robot controller with the cloud via file I/O and SignalR. |
| **GPS (sensor)** | The Webots GPS sensor providing the robot's position (x, y) in world coordinates. Polled every 64ms by the robot controller. |
| **IDLE** | Robot state indicating the robot is stationary and available for commands. The robot skips obstacle-avoidance computation in this state for performance. |
| **Inertial Unit** | The Webots inertial unit sensor providing the robot's orientation (theta) in world coordinates. Used for goal-heading calculations in DWA. |
| **LiDAR** | Light Detection and Ranging — a distance sensor that provides 512 distance samples across a 360° field of view with 3m range. In SmartDine, used for real-time obstacle detection and distance transform computation. |
| **MANDATORY** | The `pending_arrival_status` field in the robot controller that persists arrival states (ARRIVED_TABLE/ARRIVED_KITCHEN) to prevent them from being overwritten by subsequent IDLE status on the next Webots tick. |
| **NAV_TO_TABLE** | Robot state indicating the robot is actively navigating from the kitchen to a customer's table. |
| **Occupancy Grid** | A 2D array representation of the floor plan where each cell is either free (0) or occupied/wall (255). In SmartDine, stored as a PGM image file (800×800 pixels, 0.05 m/px resolution). |
| **pixelToWorld()** | Frontend TypeScript function (`coordinateUtils.ts`) that converts canvas pixel coordinates to Webots world coordinates using the formula: `wx = (px - center) * resolution`, `wy = (center - py) * resolution`. |
| **path simplification** | A post-processing step that uses Bresenham line-of-sight testing to remove redundant waypoints from a path. Reduces path file size and improves robot motion smoothness. |
| **PathPoint** | A C# record type in `RobotHub.cs` representing a robot position: `{ X: float, Y: float }`. Used when broadcasting the robot's path to dashboard clients. |
| **robot_path.txt** | Text file written by the robot controller containing the current planned path as a list of (x, y) coordinates. Read by the sidecar and broadcast via SignalR to the dashboard. |
| **robot_state.txt** | Text file written by the robot controller containing the robot's current state: `STATUS|X|Y|HEADING|V|OMEGA|DELIVER_TABLE|TABLE_POSITIONS`. Read by the sidecar and broadcast via SignalR to the dashboard. |
| **RETURN_TO_KITCHEN** | Robot state indicating the robot has completed a delivery and is navigating back to the kitchen. |
| **robot_sidecar.py** | Python bridge program that connects to the Order API via SignalR (auto-reconnect) and communicates with the robot controller via file I/O. Polls robot_state.txt and robot_path.txt every 200ms. |
| **RobotHub** | SignalR Hub at `/hubs/robot` in the Order API. Provides bidirectional communication: commands flow from dashboard → backend → sidecar, and state/path data flows from sidecar → backend → dashboard. |
| **RobotHub (Hubs/RobotHub.cs)** | The C# implementation of the SignalR RobotHub with methods: `SendRobotCommand()`, `SendRobotState()`, `SendRobotPath()`, `GetState()`. |
| **SignalR** | Microsoft's real-time communication framework for ASP.NET Core. In SmartDine, provides WebSocket-based bidirectional communication between the dashboard, Order API, and robot sidecar. Uses the `RobotHub` endpoint. |
| **SignalR Auto-Reconnect** | The sidecar's mechanism to automatically re-establish SignalR connections after disconnection, using exponential backoff: initial delay 3s, increasing to 30s max, resetting on success. |
| **State Machine** | The robot's behavioral model defining valid state transitions: `IDLE ↔ NAV_TO_TABLE ↔ ARRIVED_TABLE → RETURN_TO_KITCHEN → ARRIVED_KITCHEN → IDLE`. |
| **worldToPixel()** | Frontend TypeScript function (`coordinateUtils.ts`) that converts Webots world coordinates to canvas pixel coordinates using the formula: `px = center + wx / resolution`, `py = center - wy / resolution`. |
| **World Coordinates** | The Webots simulation coordinate system: origin at floor center, Y-axis points up, unit is meters. Used by the robot controller for position and heading calculations. |

[image1]: <data:image/png;base64,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>