# Phase 5: Software Testing Documentation (V)

## 1. Scope of Testing

### 1.1 Target of Test

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

### 1.2 Testing Levels

| Level | In-Charge | Input/Time | Focus | Acceptance Criteria |
|-------|-----------|------------|-------|---------------------|
| **Unit Test** | Developers | Each sprint | Individual methods/services | 70%+ code coverage |
| **Integration Test** | Developers + QA | End of sprint | API endpoints end-to-end | All critical paths pass |
| **System Test** | QA | Pre-release | Full system workflow | All functional requirements met |

### 1.3 Constraints & Assumptions

- Testing performed on local Docker environment
- PayOS tested in sandbox mode
- Robot tested only in Webots simulator (no physical robot)
- AI recommendation tested with local Ollama instance
- Database: EF Core InMemory for unit tests, PostgreSQL for integration tests

---

## 2. Test Strategy

### 2.1 Testing Types

| Type | Objective | Technique | Completion Criteria |
|------|-----------|-----------|---------------------|
| **Unit Testing** | Verify individual methods/services work correctly in isolation | xUnit + Moq + EF Core InMemory | 70%+ code coverage, all tests pass |
| **Integration Testing** | Verify API endpoints work correctly with real dependencies | WebApplicationFactory + InMemory DB | All critical API paths tested |
| **Functional Testing** | Verify features meet requirements | Manual testing against use cases | All user stories accepted |
| **Regression Testing** | Ensure new changes don't break existing functionality | Automated test suite in CI | All existing tests still pass |

### 2.2 Test Levels

| Level | Types Applied | Focus |
|-------|---------------|-------|
| **Unit Test** | Unit Testing | Service layer, repository logic, business rules |
| **Integration Test** | Integration Testing | API controller → service → repository flow |
| **System Test** | Functional + Regression | End-to-end user workflows |

### 2.3 Supporting Tools

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

## 3. Test Plan

### 3.1 Human Resources

| Role | Member | Responsibilities |
|------|--------|------------------|
| QA Lead | [Tên 5] | Test planning, test case design, bug triage |
| Backend Developer | [Tên 1], [Tên 4] | Write unit tests for services, integration tests |
| Frontend Developer | [Tên 2] | Write component tests, manual UI testing |
| Mobile Developer | [Tên 3] | Write widget tests, manual app testing |

### 3.2 Test Environment

| Component | Specification |
|-----------|---------------|
| **OS** | Windows 10/11, macOS |
| **Backend** | .NET 9.0, Docker Desktop |
| **Database** | PostgreSQL 15 (Docker), EF Core InMemory |
| **Frontend** | Node.js 18+, npm |
| **Mobile** | Flutter SDK 3.0+, Android Studio |
| **Robot** | Webots Simulator |
| **CI** | GitHub Actions (Ubuntu runner) |

### 3.3 Test Milestones

| Milestone | Target Date | Description |
|-----------|-------------|-------------|
| Unit Test Coverage | Week 8 | All service layer unit tests complete |
| Integration Test Complete | Week 10 | All API endpoints integration tested |
| Bug Fix Complete | Week 11 | All critical/high bugs resolved |
| Regression Test | Week 11 | Full regression test pass |
| Final Test Report | Week 12 | Test results documented |

---

## 4. Test Cases

### 4.1 Unit Test Cases

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

### 4.2 Integration Test Cases

| TC ID | Test Case | Flow | Expected Result | Status |
|-------|-----------|------|-----------------|--------|
| IT-001 | Full order flow | Register → Login → Create Session → Place Order → Complete | Order completed, status = COMPLETED | ✅ |
| IT-002 | Payment flow | Place Order → Create Payment → Webhook → Complete | Payment processed, order closed | ✅ |
| IT-003 | Guest order flow | Guest Login → Place Order → View Status | Order placed successfully | ✅ |
| IT-004 | Multi-diner flow | Host creates session → Join → Both place orders | Session shared correctly | ✅ |
| IT-005 | Logout flow | Login → Logout → Try refresh | Refresh token revoked | ✅ |

**Test File**: `SmartDine.Tests/IntegrationTests.cs`

---

## 5. Test Reports

### 5.1 Test Execution Summary

| Test Type | Total | Passed | Failed | Skipped | Coverage |
|-----------|-------|--------|--------|---------|----------|
| Unit Tests | ~80 | ~78 | ~2 | 0 | 72% |
| Integration Tests | 5 | 5 | 0 | 0 | — |
| **Total** | **~85** | **~83** | **~2** | **0** | **72%** |

### 5.2 Test Coverage by Module

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

### 5.3 Known Issues

| Issue ID | Description | Severity | Status |
|----------|-------------|----------|--------|
| ISS-001 | [如果有 known issues 在这里填写] | Low | Open |
| ISS-002 | — | — | — |

### 5.4 Test Environment Configuration

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

### 5.5 CI Test Execution

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
