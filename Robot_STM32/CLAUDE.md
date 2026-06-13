# Robot_STM32 — Robot Firmware

Bare-metal C firmware for the STM32F103C8 (ARM Cortex-M3) that runs the physical robot: motor control, obstacle detection, safety supervision, battery monitoring, and serial telemetry.

## Target Hardware

| Item | Detail |
|---|---|
| MCU | STM32F103C8 ("Blue Pill") |
| Core | ARM Cortex-M3 @ 72 MHz |
| Flash | 64 KB (0x08000000) |
| RAM | 20 KB (0x20000000) |
| Toolchain | Keil MDK-ARM v5, ARM Compiler v5.06 |
| Project file | `Project.uvprojx` |

## Directory Layout

```
Robot_STM32/
├── User/                         # Application layer
│   ├── main.c / main.h           # FSM super-loop, entry point
│   ├── Safety_Task.c / .h        # Obstacle safety supervisor
│   └── stm32f10x_it.c / .h      # Interrupt service routines (SysTick, EXTIs)
├── Hardware/                     # Peripheral drivers
│   ├── PWM_Motor.c / .h          # Dual H-bridge motor control (left/right PWM)
│   ├── Servo.c / .h              # Servo motor
│   ├── UART_DMA.c / .h           # UART with DMA for command rx and telemetry tx
│   ├── Ultrasonic_NonBlocking.c / .h  # HC-SR04 non-blocking distance sensor
│   ├── WS2812B.c / .h            # RGB LED strip
│   └── Tracking.c / .h          # Line tracking sensor
├── System/
│   ├── sys.c / .h                # System clock, GPIO init helpers
│   └── Delay.c / .h             # Millisecond delay utilities
├── Start/                        # Startup files & CMSIS
│   ├── startup_stm32f10x_md.s    # Vector table & reset handler
│   ├── system_stm32f10x.c / .h  # SystemInit, clock configuration
│   └── core_cm3.c / .h          # ARM Cortex-M3 CMSIS core
├── Library/                      # STM32F10x Standard Peripheral Library (SPL)
│   └── stm32f10x_*.c / .h       # GPIO, USART, TIM, ADC, EXTI, …
├── Objects/                      # Build output (hex, map, axf)
└── Listings/                     # Assembly listings
```

## Architecture

The firmware runs a **non-blocking FSM super-loop** driven by a 1 ms SysTick timer. There is no RTOS.

```c
// Simplified main loop
Init_All();
while (1) {
    // 1. Trigger ultrasonic every 100 ms
    if (system_ticks - last_ultrasonic_trigger_time >= 100)
        Ultrasonic_Trigger();

    // 2. Safety supervisor — kills motors if obstacle < 15 cm
    SafetyState_t state = Safety_Task_Update();

    // 3. Parse incoming UART commands (DMA ring buffer)
    UART_ProcessCommand(state);

    // 4. Transmit telemetry every 250 ms (4 Hz)
    if (system_ticks - last_telemetry_time >= 250) {
        battery_v = Battery_Read_Voltage();
        UART_SendTelemetry(distance, safety, mode, battery_v);
    }
}
```

## Serial Protocol (UART to ESP32)

UART parameters are defined in `UART_DMA.h`. Communication is with the ESP32 gateway over a wired UART connection.

### Telemetry (STM32 → ESP32) — 4 Hz

```
$STATUS,<distance_cm>,<safety_state>,<mode>,<battery_mv>
```

Example: `$STATUS,25,1,2,3300`

| Field | Description |
|---|---|
| distance_cm | HC-SR04 reading in centimetres |
| safety_state | 0 = clear, 1 = warning, 2 = critical stop |
| mode | Current drive mode |
| battery_mv | Battery voltage in millivolts (ADC reading) |

### Commands (ESP32 → STM32)

| Command | Format | Description |
|---|---|---|
| Move | `$M,<left>,<right>\n` | Set left/right motor speeds (signed int) |
| Stop | `$S\n` | Emergency stop both motors |

## Key Drivers

### `UART_DMA.c`
- Receives robot commands via DMA ring buffer (non-blocking)
- `UART_ProcessCommand(SafetyState_t)` parses `$M` / `$S` frames and drives motors (unless safety is CRITICAL)
- `UART_SendTelemetry(...)` formats and transmits `$STATUS` frame

### `Ultrasonic_NonBlocking.c`
- HC-SR04 using EXTI echo interrupt — no blocking delays
- `Ultrasonic_Trigger()` fires the trigger pulse
- Echo rising/falling edge timestamps captured in ISR → `current_distance_cm` updated asynchronously

### `Safety_Task.c`
- Called every loop iteration
- Returns `SAFETY_CLEAR`, `SAFETY_WARNING`, or `SAFETY_CRITICAL`
- On CRITICAL: cuts motor PWM regardless of incoming commands

### `PWM_Motor.c`
- Configures TIM channel PWM for dual H-bridge
- Separate left/right speed setters with forward/reverse polarity

## Building & Flashing

1. Open `Project.uvprojx` in **Keil uVision 5**
2. Build (F7) — output hex at `Objects/Project.hex`
3. Flash via ST-Link using Keil's built-in download or `STM32CubeProgrammer`

No Makefile is provided; the project is Keil-only.
