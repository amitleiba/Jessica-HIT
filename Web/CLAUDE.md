# Web — Full-Stack Application

Angular 18 SPA + .NET 9 microservices for remote robot control, live telemetry, recording management, and metrics.

## Directory Layout

```
Web/
├── Backend/
│   ├── Aspire/
│   │   ├── Aspire.AppHost/        # Local dev orchestrator (dotnet run here)
│   │   └── Aspire.ServiceDefaults/# Shared telemetry / health checks
│   ├── Gateway/                   # YARP reverse proxy + SignalR hub
│   ├── JessicaManager/            # Robot WebSocket bridge
│   ├── JessicaManager.Tests/
│   ├── AuthService/               # JWT auth + user management
│   ├── RecordingManager/          # Recording session CRUD
│   ├── MetricsService/            # Sensor data persistence
│   ├── MetricsService.Tests/
│   ├── Backend.slnx               # Solution file
│   ├── Directory.Build.props      # TargetFramework = net9.0, Nullable = enable
│   ├── Directory.Packages.props   # Centralised NuGet versions
│   └── docker-compose.yml
├── Frontend/                      # Angular 18 SPA
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/              # Services, guards, interceptors, DTOs
│   │   │   ├── features/          # Page-level modules
│   │   │   ├── shared/            # Reusable components & models
│   │   │   └── store/             # NgRx actions/reducers/effects/selectors
│   │   └── environments/
│   ├── angular.json
│   └── package.json
└── jessica-simulator/             # Python WebSocket robot simulator
    ├── app.py
    └── requirements.txt
```

## Backend Services

| Service | Port | DB | Purpose |
|---|---|---|---|
| Gateway | 5207 / 7215 | — | YARP reverse proxy, SignalR hub `/hubs/jessica`, JWT validation |
| AuthService | 5100 | `jessica_auth` | Login, register, user CRUD, JWT issuance |
| JessicaManager | 5000 / 5001 | — | WebSocket bridge to robot (or simulator) |
| RecordingManager | 5300 | `jessica_recordings` | Recording session CRUD |
| MetricsService | 5080 | `jessica_metrics` | Sensor data ingestion & query |
| postgres-auth | 5433 | — | Auth database |
| postgres-recording | 5434 | — | Recording database |
| postgres-metrics | 5435 | — | Metrics database |

All services validate JWTs from a shared secret (`Jwt__SecretKey` env var).

### Gateway (`Gateway/`)
- Entry point for all external traffic
- Routes `/api/*` to microservices via YARP
- Hosts SignalR hub; relays robot status to clients via `JessicaStatusRelayService`
- Rate limiting on auth routes (10 req/10 s)

### JessicaManager (`JessicaManager/`)
- Maintains a WebSocket client connection to the robot (or simulator at `ws://<robot-ip>:8765`)
- Persists the current robot IP in `gateway-ip.json`
- `JessicaWebSocketMoveCommandPublisher` background service translates HTTP movement commands to JSON WebSocket messages
- Controllers: `CarCommandsController`, `ConnectionController`

### AuthService (`AuthService/`)
- Clean architecture: Domain → Application → Infrastructure → API
- BCrypt password hashing, JWT (HS256) with configurable expiry
- Roles: `Operator`, `Admin`

### MetricsService (`MetricsService/`)
- `MetricsCollectorWorker` background service polls JessicaManager periodically
- Stores timestamped sensor readings (distance, battery, safety mode)

## Key NuGet Packages (centralised in Directory.Packages.props)

| Package | Version | Purpose |
|---|---|---|
| Yarp.ReverseProxy | 2.3.0 | Reverse proxy in Gateway |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.0 | JWT auth |
| Microsoft.EntityFrameworkCore + Npgsql | 9.0.3 | ORM + PostgreSQL |
| BCrypt.Net-Next | 4.0.3 | Password hashing |
| Swashbuckle.AspNetCore | 7.2.0 | Swagger UI |
| xUnit | 2.9.2 | Unit testing |

## Frontend

**Stack**: Angular 18 (standalone), TypeScript 5.4, NgRx 18, PrimeNG 18, SignalR client 10, RxJS 7.8, SCSS

### Feature Modules (`src/app/features/`)

| Module | Path | Access |
|---|---|---|
| Auth | `auth/` | Public |
| Home (dashboard) | `home/` | Authenticated |
| Manual Controller | `manual-controller/` | Operator+ |
| Live Feed | `live-feed/` | Operator+ |
| Recorder | `recorder/` | Operator+ |
| Metrics | `metrics/` | Operator+ |
| User Management | `user-management/` | Admin |

### Core Services (`src/app/core/`)

- `auth.service.ts` — login/register, token storage
- `connection.service.ts` — SignalR hub lifecycle
- `recording.service.ts` — RecordingManager API calls
- `metrics.service.ts` — MetricsService API calls
- `auth.interceptor.ts` — injects Bearer token on every HTTP request
- `auth.guard.ts` / `role.guard.ts` — route protection

### NgRx Store (`src/app/store/`)

Slices: `auth`, `car` (robot state), `recording`
Effects handle API calls and SignalR events.

### Shared Components (`src/app/shared/components/`)

`jessica-controller`, `sensor-data-panel`, `telemetry-chart`, `navbar`, `media-display`, `settings`, `theme-toggle`, `circular-button`

## Development

### Run with Aspire (recommended for local dev)
```bash
cd Backend/Aspire/Aspire.AppHost
dotnet run
# Aspire dashboard available at http://localhost:15888
```

### Run with Docker Compose
```bash
cd Backend
docker-compose up --build
```

### Frontend Dev Server
```bash
cd Frontend
npm install
ng serve          # http://localhost:4200
```

### Run Tests
```bash
# All backend tests
dotnet test Backend/Backend.slnx
```

## Robot Simulator

When no physical robot is available, run the Python simulator which mimics the robot WebSocket protocol:

```bash
cd jessica-simulator
pip install -r requirements.txt
python app.py     # ws://localhost:8765
```

Point JessicaManager at `ws://localhost:8765` (update `ConnectionSettings:RobotWebSocketUrl` in its `appsettings.json` or via env var).

## Environment Variables (docker-compose)

| Variable | Used by | Description |
|---|---|---|
| `Jwt__SecretKey` | All | Shared JWT signing key |
| `Jwt__Issuer` | All | Token issuer |
| `Jwt__Audience` | All | Token audience |
| `ConnectionStrings__DefaultConnection` | AuthService, RecordingManager, MetricsService | PostgreSQL connection string |
| `ConnectionSettings__RobotWebSocketUrl` | JessicaManager | Robot WebSocket endpoint |
