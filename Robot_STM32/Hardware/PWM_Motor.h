#ifndef __PWM_MOTOR_H
#define __PWM_MOTOR_H

#include "stm32f10x.h"

/*
 * Motor API
 * 
 * Hardware Connections:
 * TIM4 PWM:
 *   Left Motor PWM:  PB6 (TIM4_CH1)
 *   Right Motor PWM: PB7 (TIM4_CH2)
 * 
 * Direction GPIOs:
 *   Left Motor:  PA4, PA5
 *   Right Motor: PB4, PB5
 * 
 * Note: PB4 is NJTRST by default, requires AFIO remap to use as standard GPIO.
 */

// Initialize the motor PWM and Direction GPIOs
void Motor_Init(void);

// Set motor speeds (-100 to 100)
// Positive values for forward, negative for reverse, 0 for stop.
void Motor_SetSpeed(int16_t leftSpeed, int16_t rightSpeed);

#endif // __PWM_MOTOR_H
