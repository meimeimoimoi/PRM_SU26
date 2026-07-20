# Phase 2: Project Management Plan (II)

## 1. Overview

### 1.1 Scope & Estimation

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

### 1.2 Project Objectives

- Hoàn thành dự án trong [X] tuần (theo timeline thực tế)
- Đạt code coverage >= 70% cho unit tests
- Zero critical bugs at release
- Deploy thành công trên Render (BE) + Vercel (FE)
- Đảm bảo 100% functional requirements được implement

### 1.3 Project Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Robot integration phức tạp | High | High | Prototype sớm, Webots simulator, chia nhỏ task |
| PayOS sandbox instability | Medium | Medium | Test với mock service, fallback to CASH |
| Team thiếu kinh nghiệm microservices | Medium | Medium | Training session, pair programming, code review |
| Flutter Web performance | Low | Low | PWA fallback, responsive design |
| Database schema changes | Medium | Medium | Code-first migration, version control |
| Third-party API changes (PayOS, Ollama) | Low | Medium | Abstract interfaces, mock for testing |

---

## 2. Management Approach

### 2.1 Project Process

- **Agile Scrum** với sprint 2 tuần
- **Sprint Planning** → **Development** → **Sprint Review** → **Retrospective**
- **Daily Standup**: 15 phút mỗi ngày
- **Tools**: GitHub Projects, Slack/Discord, VS Code Live Share

### 2.2 Quality Management

- Code review PR trước khi merge (ít nhất 1 reviewer)
- Unit test + Integration test (xUnit + Moq)
- CI/CD pipeline (GitHub Actions) chạy tự động mỗi PR
- SonarQube cho code quality analysis
- Commit message convention: `feat:`, `fix:`, `chore:`, `docs:`

### 2.3 Training Plan

| Training Topic | Target Members | Duration | Source |
|----------------|----------------|----------|--------|
| Microservices Architecture (.NET) | All backend | 2 days | Microsoft Docs, YouTube |
| SignalR Real-time | Backend + Frontend | 1 day | Microsoft Learn |
| Flutter Riverpod State Management | Mobile team | 1 day | Flutter.dev |
| Webots Robot Simulator | Robot team | 2 days | Webots Documentation |
| Docker & Docker Compose | All | 1 day | Docker Documentation |
| PayOS Integration | Payment team | 0.5 day | PayOS API Docs |

---

## 3. Project Deliverables

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
| Docker Images | 6 Dockerfiles (5 APIs + Map Server) |
| Deployment Config | docker-compose.yml, Render/Vercel config |
| Source Code Repository | GitHub repo with full history |

---

## 4. Responsibility Assignments

| Role | Member | Responsibilities |
|------|--------|------------------|
| **Project Manager** | [Tên 1] | Planning, scheduling, risk management, stakeholder communication |
| **Backend Lead** | [Tên 1] | Architecture design, API development, code review |
| **Frontend Lead** | [Tên 2] | Web Dashboard development, UI/UX design |
| **Mobile Lead** | [Tên 3] | Flutter app development, QR scanning |
| **Backend Developer** | [Tên 4] | Service implementation, Robot integration |
| **QA/DevOps** | [Tên 5] | Testing, CI/CD, Docker deployment |

---

## 5. Project Communications

| Communication | Frequency | Participants | Tool |
|---------------|-----------|--------------|------|
| Daily Standup | Daily (15 min) | All team | Discord/Slack |
| Sprint Planning | Bi-weekly | All team | Discord + GitHub Projects |
| Sprint Review | Bi-weekly | All team + Supervisor | Discord + Zoom |
| Code Review | Continuous | Backend/Frontend teams | GitHub PR |
| Status Report | Weekly | PM + Supervisor | Email/Document |

---

## 6. Configuration Management

### 6.1 Document Management

- All documents stored in `docs/` folder of GitHub repository
- Version control via Git
- Final reports in Markdown format for easy collaboration
- Sequence diagrams in PDF format (in `app/BE/docs/`)

### 6.2 Source Code Management

- **Repository**: GitHub (private)
- **Branching Strategy**: Git Flow
  - `main`: Production-ready code
  - `develop`: Integration branch
  - `feature/*`: Feature branches
  - `hotfix/*`: Emergency fixes
- **Commit Convention**: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`)
- **PR Requirements**: At least 1 approval, CI passes, no conflicts

### 6.3 Tools & Infrastructures

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
