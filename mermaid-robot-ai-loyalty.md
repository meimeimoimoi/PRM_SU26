# Mermaid Diagrams - Robot, AI & Loyalty Modules (draw.io Compatible)

## 13. Class Diagram - Robot Module (§3.5.1)

```mermaid
classDiagram
    class RobotHub {
        +SendCommandAsync()
        +UpdateStateAsync()
        +BroadcastPath()
    }

    class PythonSidecar {
        -SignalR Client
        -File I/O Bridge
        -HTTP Client
    }

    class WebotsController {
        -DWA Path Planner
        -LiDAR Obstacle
        -GPS + Inertial
        -Waypoint Following
    }

    RobotHub --> PythonSidecar : communicates with
    PythonSidecar --> WebotsController : reads/writes files
```

---

## 14. Sequence Diagram - Robot Delivery (§3.5.2)

```mermaid
sequenceDiagram
    participant S as Staff
    participant D as Dashboard
    participant R as RobotHub
    participant B as Backend
    participant SC as Sidecar
    participant W as Webots

    S->>D: Trigger Delivery
    D->>R: POST /robots/command
    R->>B: SendCommandAsync()
    B->>SC: Write command file
    SC->>W: Read command
    W->>W: Plan path (DWA)
    W-->>SC: Navigate
    SC->>SC: Write state
    B->>SC: Read state
    B->>R: UpdateStateAsync()
    R-->>D: StateUpdate
    D-->>S: Dashboard updated
```

---

## 15. Class Diagram - AI Module (§3.6.1)

```mermaid
classDiagram
    class AIService {
        -IOllamaClient _ollamaClient
        -ICustomerActivityRepository _activityRepo
        +GetRecommendationsAsync() List~MenuItem~
        +LogRecommendation() void
    }

    class IOllamaClient {
        <<interface>>
        +GenerateAsync(prompt) String
    }

    class ICustomerActivityRepository {
        <<interface>>
        +GetByCustomerIdAsync(id) List~CustomerActivity~
    }

    AIService --> IOllamaClient
    AIService --> ICustomerActivityRepository
```

---

## 16. Sequence Diagram - Get Recommendations (§3.6.2)

```mermaid
sequenceDiagram
    participant C as Customer
    participant M as Mobile App
    participant G as Gateway
    participant A as AI API
    participant O as Ollama
    participant D as Database

    C->>M: View Menu
    M->>G: GET /ai/recommendations
    G->>A: GET /ai/recommendations
    A->>D: GetCustomerActivity()
    D-->>A: Activities
    A->>A: Build prompt (frequently ordered categories)
    A->>O: POST /api/generate
    O-->>A: {response}
    A->>D: LogRecommendation()
    A-->>G: {recommendations}
    G-->>M: {recommendations}
    M-->>C: View recommendations
```

---

## 17. Membership Tier Logic Diagram (§3.7.1)

```mermaid
flowchart TB
    subgraph Tiers["Customer Total Spent → Membership Tier"]
        direction TB
        A["< 1,000,000 VND"] --> B["BRONZE"]
        C["1,000,000 - 4,999,999 VND"] --> D["SILVER (1.2x points)"]
        E["5,000,000 - 19,999,999 VND"] --> F["GOLD (1.5x points + exclusive coupons)"]
        G[">= 20,000,000 VND"] --> H["PLATINUM (2x points + priority service)"]
    end

    style B fill:#CD7F32,color:#fff
    style D fill:#C0C0C0,color:#000
    style F fill:#FFD700,color:#000
    style H fill:#E5E4E2,color:#000
```
