# SmartDine Robot Real-time Communication — Implementation Plan

## Overview

Mục tiêu: Triển khai giao tiếp real-time giữa Dashboard → Backend (SignalR) → Sidecar → Robot, thay thế cơ chế HTTP polling hiện tại.

### Kiến trúc mới

```
Dashboard ──SignalR invoke──→ Backend RobotHub ──SignalR push──→ Sidecar ──file I/O──→ Robot C
Dashboard ←──SignalR push──── Backend RobotHub ←──SignalR invoke── Sidecar ←──file I/O── Robot C
```

### Flow数据

| Direction | Event | Dữ liệu |
|-----------|-------|---------|
| FE → BE | `SendRobotCommand` | `{ command, target, direction }` |
| BE → Sidecar | `ReceiveRobotCommand` | `{ command, target, direction }` |
| Sidecar → BE | `SendRobotState` | `{ x, y, theta, v, omega, status }` |
| BE → FE | `ReceiveRobotState` | `{ x, y, theta, v, omega, status }` |
| Sidecar → BE | `SendRobotPath` | `{ path: [{x,y}, ...] }` |
| BE → FE | `ReceiveRobotPath` | `{ path: [{x,y}, ...] }` |

---

## Phase 1: Backend — RobotHub + NotificationService

### Files cần tạo

#### 1.1 `SmartDine.Order.API/Hubs/RobotHub.cs` (MỚI)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartDine.Order.API.Hubs;

[Authorize]
public class RobotHub : Hub
{
    // Dashboard gửi command đến robot
    public async Task SendRobotCommand(string command, string target, string direction)
    {
        await Clients.Group("RobotGroup").SendAsync("ReceiveRobotCommand", new
        {
            command,
            target,
            direction
        });
    }

    // Dashboard gửi state update (nếu sidecar không khả dụng)
    public async Task SendRobotState(double x, double y, double theta,
                                      double v, double omega, string status)
    {
        await Clients.Group("RobotGroup").SendAsync("ReceiveRobotState", new
        {
            x, y, theta, v, omega, status
        });
    }

    // Dashboard gửi path update
    public async Task SendRobotPath(List<PathPoint> path)
    {
        await Clients.Group("RobotGroup").SendAsync("ReceiveRobotPath", new
        {
            path
        });
    }

    // Sidecar join group khi kết nối
    public async Task JoinRobotGroup()
        => await Groups.AddToGroupAsync(Context.ConnectionId, "RobotGroup");

    public async Task LeaveRobotGroup()
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "RobotGroup");
}

public class PathPoint
{
    public double X { get; set; }
    public double Y { get; set; }
}
```

#### 1.2 `SmartDine.Order.API/Services/RobotNotificationService.cs` (MỚI)

```csharp
using Microsoft.AspNetCore.SignalR;
using SmartDine.Order.API.Hubs;

namespace SmartDine.Order.API.Services;

public interface IRobotNotificationService
{
    Task SendCommandAsync(string command, string target, string direction);
    Task SendStateAsync(double x, double y, double theta, double v, double omega, string status);
    Task SendPathAsync(List<PathPoint> path);
}

public class RobotNotificationService : IRobotNotificationService
{
    private readonly IHubContext<RobotHub> _hubContext;

    public RobotNotificationService(IHubContext<RobotHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendCommandAsync(string command, string target, string direction)
    {
        await _hubContext.Clients.Group("RobotGroup").SendAsync("ReceiveRobotCommand", new
        {
            command, target, direction
        });
    }

    public async Task SendStateAsync(double x, double y, double theta,
                                      double v, double omega, string status)
    {
        await _hubContext.Clients.Group("RobotGroup").SendAsync("ReceiveRobotState", new
        {
            x, y, theta, v, omega, status
        });
    }

    public async Task SendPathAsync(List<PathPoint> path)
    {
        await _hubContext.Clients.Group("RobotGroup").SendAsync("ReceiveRobotPath", new
        {
            path
        });
    }
}
```

### Files cần sửa

#### 1.3 `SmartDine.Order.API/Program.cs` (SỬA)

```diff
// Thêm sau dòng builder.Services.AddSignalR();
+ builder.Services.AddScoped<IRobotNotificationService, RobotNotificationService>();

// Thêm sau dòng app.MapHub<OrderHub>("/hubs/orders");
+ app.MapHub<RobotHub>("/hubs/robot");
```

#### 1.4 `SmartDine.Gateway/appsettings.json` (SỬA)

```diff
// Thêm route mới trong Routes
+ "robot-signalr-route": {
+     "ClusterId": "orders-cluster",
+     "Match": {
+         "Path": "/hubs/robot/{**catch-all}"
+     }
+ }
```

---

## Phase 2: Frontend — SignalR Client

### Files cần sửa

#### 2.1 `app/FE/web-dashboard/package.json` (SỬA)

```bash
npm uninstall socket.io-client
npm install @microsoft/signalr
```

#### 2.2 `app/FE/web-dashboard/src/hooks/useSignalR.ts` (MỚI)

```typescript
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { useEffect, useRef, useState, useCallback } from 'react';

const SIGNALR_URL = import.meta.env.VITE_SIGNALR_URL || 'http://localhost:5000/hubs/robot';

export const useSignalR = () => {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem('access_token');
    if (!token) return;

    const conn = new HubConnectionBuilder()
      .withUrl(SIGNALR_URL, {
        accessTokenFactory: () => token,
        transport: 0, // WebSockets only
      })
      .withAutomaticReconnect()
      .build();

    conn.onreconnecting(() => setConnected(false));
    conn.onreconnected(() => setConnected(true));
    conn.onclose(() => setConnected(false));

    conn.start()
      .then(() => {
        setConnected(true);
        return conn.invoke('JoinRobotGroup');
      })
      .catch(err => console.error('SignalR connection error:', err));

    setConnection(conn);

    return () => { conn.stop(); };
  }, []);

  const invoke = useCallback((method: string, ...args: any[]) => {
    return connection?.invoke(method, ...args);
  }, [connection]);

  const on = useCallback((event: string, callback: (...args: any[]) => void) => {
    connection?.on(event, callback);
    return () => { connection?.off(event, callback); };
  }, [connection]);

  return { connection, connected, invoke, on };
};
```

#### 2.3 `app/FE/web-dashboard/src/components/components_draw_map/RobotConsole.tsx` (SỬA)

```diff
+ import { useSignalR } from '@/hooks/useSignalR';

  const RobotConsole = () => {
+   const { connected, invoke, on } = useSignalR();
+   const [robotState, setRobotState] = useState({ x:0, y:0, status:'OFFLINE' });

+   // Lắng nghe robot state real-time
+   useEffect(() => {
+     const cleanup = on('ReceiveRobotState', (data) => {
+       setRobotState(data);
+     });
+     return cleanup;
+   }, [on]);

-   // XÓA polling setInterval cho robot status
-   useEffect(() => {
-     const interval = setInterval(async () => {
-       const res = await fetch('http://localhost:3001/api/robot/status');
-       const data = await res.json();
-       setRobotState(data);
-     }, 150);
-     return () => clearInterval(interval);
-   }, []);

    const sendControlCommand = async (command: string, target?: string, direction?: string) => {
-     await fetch('http://localhost:3001/api/robot/control', {
-       method: 'POST',
-       headers: { 'Content-Type': 'application/json' },
-       body: JSON.stringify({ command, target, direction }),
-     });
+     await invoke('SendRobotCommand', command, target || 'NONE', direction || 'NONE');
    };

    // ... rest of component unchanged
  };
```

#### 2.4 `app/FE/web-dashboard/src/components/components_draw_map/MapCanvas.tsx` (SỬA)

```diff
+ import { useSignalR } from '@/hooks/useSignalR';

  const MapCanvas = () => {
+   const { on } = useSignalR();
+   const [robotPos, setRobotPos] = useState({ x:0, y:0, theta:0 });
+   const [robotPath, setRobotPath] = useState([]);

+   useEffect(() => {
+     const cleanupState = on('ReceiveRobotState', (data) => {
+       setRobotPos(data);
+     });
+     const cleanupPath = on('ReceiveRobotPath', (data) => {
+       setRobotPath(data.path);
+     });
+     return () => { cleanupState(); cleanupPath(); };
+   }, [on]);

-   // XÓA polling intervals cho status (150ms) và path (500ms)
-   useEffect(() => {
-     const statusInterval = setInterval(async () => { ... }, 150);
-     const pathInterval = setInterval(async () => { ... }, 500);
-     return () => { clearInterval(statusInterval); clearInterval(pathInterval); };
-   }, []);

    // ... rest of component unchanged
  };
```

#### 2.5 `app/FE/web-dashboard/.env` (MỚI — nếu cần)

```
VITE_SIGNALR_URL=http://localhost:5000/hubs/robot
VITE_SOCKET_URL=http://localhost:5000
```

---

## Phase 3: Python Sidecar — Thêm SignalR Client

### Files cần sửa

#### 3.1 `Robot/sidecar/requirements.txt` (SỬA)

```
requests>=2.28.0
signalrcore>=0.9.0
```

#### 3.2 `Robot/sidecar/robot_sidecar.py` (SỬA)

```diff
  import requests
+ from signalrcore.hub.base_hub_connection import HubConnectionBuilder
+ from signalrcore.services.base_reconnect_service import BaseReconnectService

  class RobotSidecar:
      def __init__(self, server_url, controller_dir, poll_interval, map_id):
          self.client = ServerClient(server_url)
          self.controller_dir = os.path.abspath(controller_dir)
          self.poll_interval = poll_interval
          self.map_id = map_id
          self.running = True
+         self.signalr_conn = None

      def startup(self):
          # ... existing startup code ...
+         self._connect_signalr()
          return True

+     def _connect_signalr(self):
+         """Connect to backend SignalR hub for real-time commands."""
+         signalr_url = os.environ.get("SIGNALR_URL", "http://localhost:5000/hubs/robot")
+         token = os.environ.get("AUTH_TOKEN", "")
+
+         self.signalr_conn = HubConnectionBuilder() \
+             .with_url(signalr_url, options={"access_token_factory": lambda: token}) \
+             .with_automatic_reconnect(BaseReconnectService(
+                 max_reconnect_attempts=10,
+                 reconnect_interval=5
+             )) \
+             .build()
+
+         self.signalr_conn.on_open(lambda: self._on_signalr_connected())
+         self.signalr_conn.on_close(lambda: self._on_signalr_disconnected())
+         self.signalr_conn.on("ReceiveRobotCommand", self._on_robot_command)
+
+         try:
+             self.signalr_conn.start()
+             log.info(f"SignalR connected to {signalr_url}")
+         except Exception as e:
+             log.error(f"SignalR connection failed: {e}")

+     def _on_signalr_connected(self):
+         log.info("SignalR connected, joining RobotGroup")
+         try:
+             self.signalr_conn.invoke("JoinRobotGroup")
+         except Exception as e:
+             log.error(f"Failed to join RobotGroup: {e}")

+     def _on_signalr_disconnected(self):
+         log.warning("SignalR disconnected")

+     def _on_robot_command(self, args):
+         """Handle command received via SignalR."""
+         if args and len(args) > 0:
+             cmd = args[0]
+             cmd_str = f"{cmd.get('command','NONE')} {cmd.get('target','NONE')} {cmd.get('direction','NONE')}"
+             command_file = os.path.join(self.controller_dir, "command.txt")
+             write_file(command_file, cmd_str)
+             log.info(f"SignalR command received: {cmd_str}")

      def run(self):
          # ... existing run code ...
          while self.running:
              try:
                  if not connected:
                      connected = self.startup()
                  else:
                      self.tick()
+                     self._sync_state_signalr()  # Push state via SignalR
+                     self._sync_path_signalr()   # Push path via SignalR
              except Exception as e:
                  log.error(f"Tick error: {e}")
              time.sleep(self.poll_interval)

+     def _sync_state_signalr(self):
+         """Push robot state via SignalR (replaces HTTP POST)."""
+         state_file = os.path.join(self.controller_dir, "robot_state.txt")
+         h = file_hash(state_file)
+         if h == self._last_state_hash:
+             return
+         self._last_state_hash = h
+         raw = read_file(state_file)
+         if not raw:
+             return
+         state = parse_robot_state(raw)
+         if state and self.signalr_conn:
+             try:
+                 self.signalr_conn.invoke("SendRobotState",
+                     state["x"], state["y"], state["theta"],
+                     state["v"], state["omega"], state["status"])
+             except Exception as e:
+                 log.warning(f"SignalR state push failed: {e}")

+     def _sync_path_signalr(self):
+         """Push robot path via SignalR (replaces HTTP POST)."""
+         path_file = os.path.join(self.controller_dir, "robot_path.txt")
+         h = file_hash(path_file)
+         if h == self._last_path_hash:
+             return
+         self._last_path_hash = h
+         raw = read_file(path_file)
+         if raw is None:
+             return
+         points = parse_robot_path(raw)
+         if self.signalr_conn:
+             try:
+                 self.signalr_conn.invoke("SendRobotPath", points)
+             except Exception as e:
+                 log.warning(f"SignalR path push failed: {e}")
```

---

## Phase 4: Map Server — Cleanup (Tùy chọn)

### Files cần sửa

#### 4.1 `map-server/server.js` (SỬA — xóa legacy file write)

```diff
  // POST /api/robot/control — chỉ giữ queue, xóa file write
  app.post('/api/robot/control', (req, res) => {
    const { command, target, direction } = req.body;
    commandQueue.push({ command, target, direction });
-   const commandPath = path.join(WEBOTS_CONTROLLER_DIR, 'command.txt');
-   fs.writeFileSync(commandPath, cmd);
    res.json({ success: true, queued: commandQueue.length });
  });

  // GET /api/robot/status — chỉ giữ in-memory, xóa file fallback
  app.get('/api/robot/status', (req, res) => {
-   if (robotState.status !== 'OFFLINE' || ...) {
-     return res.json(robotState);
-   }
-   // fallback: read file
-   ...
+   return res.json(robotState);
  });

  // GET /api/robot/path — tương tự
  app.get('/api/robot/path', (req, res) => {
+   return res.json(robotPath);
  });
```

---

## Phase 5: Docker & Config

### Files cần sửa

#### 5.1 `app/docker-compose.yml` (SỬA — thêm SignalR env vars cho Order.API)

```diff
  order-api:
    environment:
      # ... existing env vars ...
+     - SignalR__HubUrl=/hubs/robot
```

#### 5.2 `Robot/sidecar/.env` (MỚI)

```
MAP_SERVER_URL=http://your-server:3001
SIGNALR_URL=http://your-server:5000/hubs/robot
AUTH_TOKEN=your-jwt-token
CONTROLLER_DIR=../controllers/robot_controller
POLL_INTERVAL=0.2
```

---

## Checklist

### Phase 1 — Backend
- [ ] Tạo `RobotHub.cs`
- [ ] Tạo `RobotNotificationService.cs`
- [ ] Đăng ký trong `Program.cs`
- [ ] Thêm route Gateway
- [ ] Test: `dotnet build` không lỗi

### Phase 2 — Frontend
- [ ] Cài `@microsoft/signalr`
- [ ] Xóa `socket.io-client`
- [ ] Tạo `useSignalR.ts`
- [ ] Sửa `RobotConsole.tsx` — xóa polling, dùng SignalR
- [ ] Sửa `MapCanvas.tsx` — xóa polling, dùng SignalR
- [ ] Test: Dashboard render đúng, command gửi được

### Phase 3 — Sidecar
- [ ] Cài `signalrcore`
- [ ] Thêm SignalR connection vào sidecar
- [ ] Thêm `_on_robot_command` handler
- [ ] Thêm `_sync_state_signalr` và `_sync_path_signalr`
- [ ] Test: Sidecar nhận command real-time

### Phase 4 — Map Server (Tùy chọn)
- [ ] Xóa legacy file write trong `POST /api/robot/control`
- [ ] Xóa file fallback trong `GET /api/robot/status`
- [ ] Xóa file fallback trong `GET /api/robot/path`
- [ ] Test: Dashboard vẫn hoạt động

### Phase 5 — Docker
- [ ] Thêm env vars cho sidecar
- [ ] Test: `docker compose up` hoạt động
