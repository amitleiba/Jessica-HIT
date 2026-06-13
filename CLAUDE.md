# Jessica-HIT — System Overview

Jessica is a solar-powered agricultural robot. This monorepo contains all system layers: embedded firmware on the robot, a BLE-to-WebSocket gateway, and a full-stack web application for remote control and monitoring.

## Repository Layout

```
Jessica-HIT/
├── Robot_STM32/          # STM32F103C8 robot firmware (C, Keil MDK-ARM)
├── Robot_Gateway_ESP32/  # ESP32 BLE↔WebSocket bridge (C++, PlatformIO)
└── Web/                  # Full-stack web app
    ├── Backend/           # .NET 9 microservices + Aspire orchestration
    ├── Frontend/          # Angular 18 SPA
    └── jessica-simulator/ # Python WebSocket robot simulator
```

## End-to-End Communication Chain

```
[STM32 Robot] --UART--> [ESP32 Gateway] --BLE-- ... --WebSocket:81--> [JessicaManager] --> [Gateway :5207] --SignalR--> [Angular Frontend]
```

| Segment | Protocol | Format |
|---|---|---|
| STM32 → ESP32 | UART | `$STATUS,<dist>,<safety>,<mode>,<battery_mv>` |
| ESP32 ↔ BLE (STM32 side) | Bluetooth LE (UUID FFE0/FFE1) | `$STATUS…` / `$M,<l>,<r>` |
| ESP32 ↔ Backend | WebSocket (port 81) | JSON |
| Frontend ↔ Backend | SignalR + REST over HTTPS | JSON / JWT |

**Telemetry JSON (ESP32 → Backend)**
```json
{ "type": "telemetry", "distance": 25, "safety": 1, "mode": 2, "battery": 3.3 }
```

**Command JSON (Backend → ESP32)**
```json
{ "cmd": "move", "left": 150, "right": 150 }
{ "cmd": "stop" }
```

## Roles & Authentication

- Roles: **Operator**, **Admin**
- Issued as JWT by AuthService; validated in the Gateway on every request
- Operators control the robot; Admins additionally manage users

## Sub-project Docs

- [Robot_STM32/CLAUDE.md](Robot_STM32/CLAUDE.md) — embedded firmware
- [Robot_Gateway_ESP32/CLAUDE.md](Robot_Gateway_ESP32/CLAUDE.md) — BLE/WiFi gateway
- [Web/CLAUDE.md](Web/CLAUDE.md) — backend microservices + Angular frontend

## Quick Start

### Embedded
- STM32: open `Robot_STM32/Project.uvprojx` in Keil uVision, build & flash
- ESP32: `cd Robot_Gateway_ESP32 && pio run --target upload` (COM3)

### Web (Docker)
```bash
cd Web/Backend
docker-compose up --build
# Frontend dev server
cd ../Frontend && npm install && ng serve
```

### Web (Aspire — local dev)
```bash
cd Web/Backend/Aspire/Aspire.AppHost
dotnet run
```

### Robot Simulator (no hardware)
```bash
cd Web/jessica-simulator
pip install -r requirements.txt
python app.py          # WebSocket server on :8765
```
