#include "stm32f10x.h"
#include "PWM_Motor.h"
#include "Ultrasonic_NonBlocking.h"
#include "Safety_Task.h"
#include "UART_DMA.h"

/* ===========================================================================
 * System tick counter — incremented every 1 ms by SysTick_Handler
 * (defined in stm32f10x_it.c)
 * =========================================================================== */
volatile uint32_t system_ticks = 0;

/* ===========================================================================
 * Hardware Initialization
 * =========================================================================== */

/**
 * @brief Initialize every hardware subsystem and configure the SysTick timer
 *        to fire every 1 ms for the non-blocking FSM scheduler.
 */
static void Init_All(void)
{
    Motor_Init();
    Ultrasonic_Init();
    Safety_Init();
    UART_DMA_Init();
    Battery_ADC_Init();

    /*
     * SysTick: 1 ms period.
     * SystemCoreClock is typically 72 MHz on STM32F103.
     * 72 000 000 / 1000 = 72 000 ticks per millisecond.
     */
    SysTick_Config(SystemCoreClock / 1000);
}

/* ===========================================================================
 * Main entry point
 * =========================================================================== */

int main(void)
{
    Init_All();

    /* Non-blocking timer tracking variables (all in milliseconds) */
    uint32_t last_ultrasonic_trigger_time = 0;
    uint32_t last_telemetry_time          = 0;

    /* Cached telemetry values */
    float battery_v = 0.0f;

    /* -----------------------------------------------------------------------
     * Non-Blocking Finite State Machine (FSM) Super-Loop
     * ----------------------------------------------------------------------- */
    while (1)
    {
        /* ===================================================================
         * 1. Asynchronous Sensor Acquisition  (every 100 ms)
         * =================================================================== 
         * Issue an ultrasonic trigger pulse periodically.  The EXTI-based
         * echo measurement runs entirely in the background and updates the
         * global `current_distance_cm` variable automatically.
         */
        if (system_ticks - last_ultrasonic_trigger_time >= 100)
        {
            Ultrasonic_Trigger();
            last_ultrasonic_trigger_time = system_ticks;
        }

        /* ===================================================================
         * 2. Hardware Safety Supervisor  (continuous)
         * ===================================================================
         * Evaluates the latest ultrasonic distance reading.  If an obstacle
         * is dangerously close (< 15 cm), the supervisor immediately kills
         * motor power and activates the buzzer.  The returned state is
         * propagated to the command parser for wall-trap logic.
         */
        SafetyState_t current_safety_state = Safety_Task_Update();

        /* ===================================================================
         * 3. UART Command Processing  (continuous / unconditional)
         * ===================================================================
         * Always process incoming commands regardless of the safety state.
         * The wall-trap logic inside UART_ProcessCommand handles obstacle
         * safety by clamping forward speeds to zero while still permitting
         * reverse motion so the operator can back the robot away from a wall.
         */
        UART_ProcessCommand(current_safety_state);

        /* ===================================================================
         * 4. Telemetry Transmission  (every 250 ms)
         * ===================================================================
         * Read the battery ADC and transmit a complete status frame to the
         * host/controller at a fixed 4 Hz rate.
         */
        if (system_ticks - last_telemetry_time >= 250)
        {
            battery_v = Battery_Read_Voltage();

            UART_SendTelemetry(current_distance_cm,
                               (uint8_t)current_safety_state,
                               robot_mode,
                               battery_v);

            last_telemetry_time = system_ticks;
        }
    }
}
