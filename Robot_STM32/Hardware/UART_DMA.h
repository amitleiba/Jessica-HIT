#ifndef __UART_DMA_H
#define __UART_DMA_H

#include "stm32f10x.h"
#include "Safety_Task.h"

#define UART_RX_BUFFER_SIZE  50

/* ---------------------------------------------------------------------------
 * Global variables (defined in UART_DMA.c, shared with main loop and ISR)
 * --------------------------------------------------------------------------- */

// Flag set by USART1 IDLE line ISR when a complete message has been received
extern volatile uint8_t new_command_ready;

// Raw DMA receive buffer populated by DMA1_Channel5
extern volatile char rx_buffer[UART_RX_BUFFER_SIZE];

// Robot operating mode: 0 = Idle, 1 = Moving
extern volatile uint8_t robot_mode;

/* ---------------------------------------------------------------------------
 * UART + DMA API
 * --------------------------------------------------------------------------- */

/**
 * @brief Initialize USART1 with DMA1_Channel5 for non-blocking RX.
 *        TX = PA9 (AF Push-Pull), RX = PA10 (Input Floating).
 *        9600 Baud, 8N1.  IDLE line interrupt detects end-of-message.
 */
void UART_DMA_Init(void);

/**
 * @brief Process a received UART command from the main loop.
 *        Text protocol:
 *          $S             -> Emergency Stop (Motor_SetSpeed(0,0))
 *          $M,left,right  -> Move command with wall-trap safety logic
 *        Must be called unconditionally every loop iteration.
 * @param current_safety Current safety state from Safety_Task_Update()
 */
void UART_ProcessCommand(SafetyState_t current_safety);

/**
 * @brief Transmit a telemetry status string over USART1 (blocking/polling).
 *        Format: "$STATUS,distance,safety_state,mode,battery_v\n"
 * @param distance      Current ultrasonic distance in cm
 * @param safety_state  Current safety state (0=CLEAR, 1=OBSTACLE)
 * @param mode          Current robot mode (0=Idle, 1=Moving)
 * @param battery_v     Battery voltage in volts (float)
 */
void UART_SendTelemetry(uint16_t distance, uint8_t safety_state,
                        uint8_t mode, float battery_v);

/* ---------------------------------------------------------------------------
 * Battery ADC API
 * --------------------------------------------------------------------------- */

/**
 * @brief Initialize ADC1 on PB0 (Channel 8) for single-conversion battery
 *        voltage measurement.  3.3 V reference, 12-bit resolution.
 */
void Battery_ADC_Init(void);

/**
 * @brief Trigger a software ADC conversion, wait for EOC,
 *        and return the solar panel voltage in volts.
 *        No external voltage divider — direct ADC pin voltage.
 * @return Solar panel voltage in volts (0.0 – 3.3 V)
 */
float Battery_Read_Voltage(void);

#endif /* __UART_DMA_H */
