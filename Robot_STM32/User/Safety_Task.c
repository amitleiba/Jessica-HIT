#include "Safety_Task.h"
#include "PWM_Motor.h"
#include "Ultrasonic_NonBlocking.h"

/**
 * @brief Initialize the Safety module hardware (Buzzer on PB1).
 */
void Safety_Init(void)
{
    // Enable GPIOB clock, which handles the buzzer
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOB, ENABLE);
    
    // Configure PB1 as Push-Pull Output for Buzzer
    GPIO_InitTypeDef GPIO_InitStructure;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_Out_PP;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;
    GPIO_InitStructure.GPIO_Pin = GPIO_Pin_1;
    GPIO_Init(GPIOB, &GPIO_InitStructure);
    
    // Initialize Buzzer to OFF (Active-Low: HIGH = OFF)
    GPIO_SetBits(GPIOB, GPIO_Pin_1);
}

/**
 * @brief Periodically checks the current ultrasonic distance for safety overrides.
 *        Uses hysteresis to prevent jitter around the threshold:
 *          - Enter OBSTACLE state:  dist > 0 && dist < 15
 *          - Exit  OBSTACLE state:  dist > 20
 *          - Dead zone (15-20cm) and noise (dist == 0): maintain previous state
 *        Edge-triggered motor stop fires ONCE on the CLEAR -> OBSTACLE transition.
 * @return SafetyState_t Current safety state after hysteresis filtering.
 */
SafetyState_t Safety_Task_Update(void)
{
    // Persistent state for edge detection and hysteresis
    static SafetyState_t previous_state = SAFETY_STATE_CLEAR;
    
    // Use the completely non-blocking global variable maintained by the EXTI interrupt
    uint16_t dist = current_distance_cm;
    
    // Start with previous state (hysteresis default: hold current state)
    SafetyState_t current_state = previous_state;
    
    // --- Hysteresis Logic ---
    if (dist > 0 && dist < 15)
    {
        // Definite obstacle: transition to OBSTACLE
        current_state = SAFETY_STATE_OBSTACLE_DETECTED;
    }
    else if (dist > 20)
    {
        // Definite clearance: transition to CLEAR
        current_state = SAFETY_STATE_CLEAR;
    }
    // else: dist == 0 (noise) OR 15 <= dist <= 20 (dead zone)
    //       -> maintain previous_state, no transition
    
    // --- Edge Trigger: one-shot emergency brake on CLEAR -> OBSTACLE ---
    if (current_state == SAFETY_STATE_OBSTACLE_DETECTED &&
        previous_state == SAFETY_STATE_CLEAR)
    {
        Motor_SetSpeed(0, 0);  // Fire EXACTLY ONCE
    }
    
    // --- Buzzer follows the maintained state ---
    if (current_state == SAFETY_STATE_OBSTACLE_DETECTED)
    {
        GPIO_ResetBits(GPIOB, GPIO_Pin_1);    // Turn ON buzzer (Active-Low: PB1 Low)
    }
    else
    {
        GPIO_SetBits(GPIOB, GPIO_Pin_1);      // Turn OFF buzzer (Active-Low: PB1 High)
    }
    
    // Update state for next iteration
    previous_state = current_state;
    
    return current_state;
}

