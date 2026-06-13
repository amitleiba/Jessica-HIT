# Robot_Gateway_ESP32 — BLE/WiFi Gateway Firmware

ESP32 firmware that bridges the STM32 robot (over Bluetooth LE) to the web backend (over WebSocket/WiFi). It is the sole network-facing component on the robot hardware side.

## Role in the System

```
STM32 Robot ──BLE (UART-over-BLE, UUID FFE0/FFE1)──> ESP32 ──WebSocket :81──> JessicaManager (backend)
```

- **Inbound from backend**: JSON move/stop commands → forwarded to STM32 over BLE characteristic write
- **Outbound to backend**: BLE notifications from STM32 (`$STATUS,…`) → parsed, re-serialised as JSON, broadcast to all WebSocket clients

## Target Hardware & Toolchain

| Item | Detail |
|---|---|
| MCU | ESP32 (generic dev board) |
| Framework | Arduino (via PlatformIO) |
| Platform | `espressif32` |
| Flash partition | `huge_app.csv` |
| Upload port | COM3 |
| Monitor baud | 115200 |

## Directory Layout

```
Robot_Gateway_ESP32/
├── platformio.ini     # Build configuration
├── src/
│   └── main.cpp       # All firmware logic
├── include/
├── lib/
└── .pio/
    └── libdeps/esp32dev/
        ├── ArduinoJson/   # 6.21.3
        └── WebSockets/    # 2.4.1
```

## Dependencies (platformio.ini)

```ini
lib_deps =
  bblanchon/ArduinoJson @ ^6.21.3
  links2004/WebSockets  @ ^2.4.1
```

## Firmware Logic (`src/main.cpp`)

### WiFi

```cpp
const char* ssid     = "Leiba";
const char* password = "0549994806";
```

Update these constants before flashing to a different network.

### BLE Client

Connects to the STM32 robot by its fixed MAC address using the HM-10 BLE UART service:

```cpp
const char* macAddress = "F8:33:31:FC:70:BB";
BLEUUID serviceUUID("FFE0");
BLEUUID charUUID("FFE1");
```

Connection flow in `setup()`:
1. `BLEDevice::init("")`
2. Create client → connect by MAC
3. Get service `FFE0` → get characteristic `FFE1`
4. Register `notifyCallback` for incoming BLE notifications

### Telemetry Path (BLE notification → WebSocket broadcast)

```
notifyCallback(data)
  └─ parse "$STATUS,<dist>,<safety>,<mode>,<battery_mv>"
  └─ build JSON: {"type":"telemetry","distance":…,"safety":…,"mode":…,"battery":…}
  └─ webSocket.broadcastTXT(json)   // sent to all connected WebSocket clients
```

### Command Path (WebSocket message → BLE write)

```
webSocketEvent(WStype_TEXT, payload)
  └─ deserialise JSON: {"cmd":"move","left":<int>,"right":<int>}
                    or {"cmd":"stop"}
  └─ format: "$M,<left>,<right>\n"  or "$S\n"
  └─ pRemoteCharacteristic->writeValue(payload, length)
```

### WebSocket Server

```cpp
WebSocketsServer webSocket(81);   // Listens on port 81
```

JessicaManager connects here. Multiple clients are supported; telemetry is broadcast to all.

### Main Loop

```cpp
void loop() {
    webSocket.loop();   // process incoming WebSocket frames
    delay(2);
}
```

## Building & Flashing

```bash
# Install PlatformIO CLI if needed
pip install platformio

cd Robot_Gateway_ESP32

# Build
pio run

# Build + upload (robot must be on COM3)
pio run --target upload

# Serial monitor
pio device monitor
```

Or use the PlatformIO IDE extension in VS Code.

## Configuration to Change Before Deploying

| Constant | File | Purpose |
|---|---|---|
| `ssid` / `password` | `src/main.cpp` | WiFi network credentials |
| `macAddress` | `src/main.cpp` | BLE MAC of the STM32 robot module |
| `upload_port` | `platformio.ini` | Serial port for flashing |

## Debugging

Serial output (115200 baud) logs:
- WiFi connection status
- BLE connection events
- Parsed telemetry values
- Received WebSocket commands

Use `pio device monitor` or any serial terminal.
