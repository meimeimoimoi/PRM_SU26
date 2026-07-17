# Deploy SmartDine (Render BE + Vercel FE)

## Tổng quan

| Thành phần | Nơi deploy | Ghi chú |
|------------|------------|---------|
| Gateway + Identity + Menu + Order + Table (+ AI) | **Render** | Docker images từ CI (`Docker Hub`) hoặc Dockerfile trong `app/BE/docker/` |
| Postgres | **Render Postgres** | Shared DB cho các API |
| Web dashboard (`web-dashboard`) | **Vercel** | Vite React |
| Customer app | **Vercel** (Flutter **Web**) hoặc store (APK/iOS) | APK không deploy Vercel |

CI hiện tại (`.github/workflows/ci-cd.yml`): build/test BE + push image Docker Hub khi push `main`. Deploy Render/Vercel cấu hình trên dashboard hoặc thêm bước sau.

File env mẫu:

- `app/BE/.env.example`
- `app/FE/web-dashboard/.env.example`
- `app/FE/customer-mobile/.env.example`

---

## 1. Render — Backend

### 1.1 Postgres

Tạo **PostgreSQL** trên Render → copy **Internal Database URL** (hoặc External) vào:

```text
ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
```

### 1.2 Web Services

Deploy từng service (Docker từ Hub hoặc Dockerfile):

| Service | Dockerfile |
|---------|------------|
| Gateway | `app/BE/docker/Gateway.Dockerfile` |
| Identity | `app/BE/docker/Identity.Dockerfile` |
| Menu | `app/BE/docker/Menu.Dockerfile` |
| Order | `app/BE/docker/Order.Dockerfile` |
| Table | `app/BE/docker/Table.Dockerfile` |
| AI (optional) | `app/BE/docker/AI.Dockerfile` |

### 1.3 Env theo service

**Mọi service có DB**

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection=...`
- `Jwt__RsaPublicKey=...`
- `Jwt__Issuer=SmartDineAPI`
- `Jwt__Audience=SmartDineApp`

**Chỉ Identity**

- `Jwt__RsaPrivateKey=...`

**Chỉ Order**

- `PayOS__ClientId` / `PayOS__ApiKey` / `PayOS__ChecksumKey`
- `Payment__ReturnUrl=https://YOUR_FLUTTER_WEB.vercel.app/payment/success`
- `Payment__CancelUrl=https://YOUR_FLUTTER_WEB.vercel.app/payment/cancel`

**Table**

- `CustomerWeb__BaseUrl=https://YOUR_FLUTTER_WEB.vercel.app`

**Menu**

- `Uploads__PublicBaseUrl=https://YOUR_GATEWAY.onrender.com`

**Gateway** — trỏ cluster tới URL nội bộ từng API (xem `app/BE/.env.example`).

### 1.4 PayOS webhook

Trong PayOS Dashboard:

```text
https://YOUR_GATEWAY.onrender.com/api/v1/payments/webhook
```

---

## 2. Vercel — Web Dashboard

1. New Project → root: `app/FE/web-dashboard`
2. Framework: Vite
3. Build: `npm ci && npm run build`
4. Output: `dist`
5. Environment Variables:

| Name | Value |
|------|--------|
| `VITE_API_URL` | `https://YOUR_GATEWAY.onrender.com/api/v1` |

6. Deploy. Đổi env → **Redeploy** (Vite embed lúc build).

SignalR tự dùng `{gateway}/hubs/orders` (bỏ `/api/v1` từ `VITE_API_URL`).

---

## 3. Vercel — Flutter Web (customer)

1. New Project → root: `app/FE/customer-mobile`
2. Cần image/build có Flutter SDK (hoặc build web ở CI rồi deploy `build/web`).
3. Build command ví dụ:

```bash
flutter build web --release --dart-define=API_BASE_URL=https://YOUR_GATEWAY.onrender.com/api/v1/
```

4. Output directory: `build/web`

`API_BASE_URL` phải có **slash cuối** `/`.

APK local/CI:

```bash
flutter build apk --release --dart-define=API_BASE_URL=https://YOUR_GATEWAY.onrender.com/api/v1/
```

---

## 4. Checklist sau deploy

- [ ] `GET https://GATEWAY/api/v1/...` (health / login) OK  
- [ ] Dashboard login với `VITE_API_URL` đúng  
- [ ] Flutter Web gọi API / SignalR OK  
- [ ] Quét QR bàn mở đúng `CustomerWeb__BaseUrl/?table=N`  
- [ ] PayOS webhook + Return/Cancel URL đúng domain Vercel  
- [ ] Không commit `.env` / RSA private / PayOS key  

---

## 5. Lưu ý

- Render free: service ngủ → cold start chậm.  
- JWT dùng **RSA** (`Jwt__RsaPublicKey` / `Jwt__RsaPrivateKey`), không dùng `Jwt__SecretKey`.  
- Map robot (`localhost:3001`) là tính năng phụ — không bắt buộc cho auth/order/payment.
