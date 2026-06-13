#include "Ultrasonic_NonBlocking.h"

// Stores the latest distance reading. 
// Initialized to a safe default. 999 implies out-of-bounds or not read yet.
volatile uint16_t current_distance_cm = 999; 

/**
 * @brief Initializes the Ultrasonic HC-SR04 sensor hardware.
 *        - Trigger: PA0 (Output)
 *        - Echo: PA1 (Input Pull-Down mapped to EXTI 1)
 *        - Timer: TIM2 (1us ticks, no interrupts)
 */
void Ultrasonic_Init(void)
{
    // 1. Enable Clocks for GPIOA, AFIO, and TIM2
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOA | RCC_APB2Periph_AFIO, ENABLE);
    RCC_APB1PeriphClockCmd(RCC_APB1Periph_TIM2, ENABLE);

    // 2. Configure Trigger Pin (PA0) as Push-Pull Output
    GPIO_InitTypeDef GPIO_InitStructure;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_Out_PP;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;
    GPIO_InitStructure.GPIO_Pin = GPIO_Pin_0;
    GPIO_Init(GPIOA, &GPIO_InitStructure);
    
    // Ensure Trig is low initially
    GPIO_ResetBits(GPIOA, GPIO_Pin_0);

    // 3. Configure Echo Pin (PA1) as Input Pull-Down
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IPD; 
    GPIO_InitStructure.GPIO_Pin = GPIO_Pin_1;
    GPIO_Init(GPIOA, &GPIO_InitStructure);

    // 4. Configure TIM2 for 1us ticks (SystemCoreClock is 72MHz)
    // Prescaler = 72 - 1 -> 1 MHz timer clock (1us per tick)
    TIM_TimeBaseInitTypeDef TIM_TimeBaseStructure;
    TIM_TimeBaseStructure.TIM_Period = 0xFFFF; // Max 16-bit value (max measure ~65ms)
    TIM_TimeBaseStructure.TIM_Prescaler = 72 - 1;
    TIM_TimeBaseStructure.TIM_ClockDivision = TIM_CKD_DIV1;
    TIM_TimeBaseStructure.TIM_CounterMode = TIM_CounterMode_Up;
    TIM_TimeBaseInit(TIM2, &TIM_TimeBaseStructure);

    // *Do not enable the timer yet. It will be enabled in the ISR.*

    // 5. Configure EXTI Line 1 to PA1
    GPIO_EXTILineConfig(GPIO_PortSourceGPIOA, GPIO_PinSource1);

    EXTI_InitTypeDef EXTI_InitStructure;
    EXTI_InitStructure.EXTI_Line = EXTI_Line1;
    EXTI_InitStructure.EXTI_Mode = EXTI_Mode_Interrupt;
    EXTI_InitStructure.EXTI_Trigger = EXTI_Trigger_Rising_Falling; // Trigger on BOTH edges
    EXTI_InitStructure.EXTI_LineCmd = ENABLE;
    EXTI_Init(&EXTI_InitStructure);

    // 6. Configure NVIC for EXTI1_IRQn
    NVIC_InitTypeDef NVIC_InitStructure;
    NVIC_InitStructure.NVIC_IRQChannel = EXTI1_IRQn;
    NVIC_InitStructure.NVIC_IRQChannelPreemptionPriority = 1;
    NVIC_InitStructure.NVIC_IRQChannelSubPriority = 0;
    NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;
    NVIC_Init(&NVIC_InitStructure);
}

/**
 * @brief Generates a 10us pulse to trigger an ultrasonic reading.
 *        Designed to be called periodically from the main loop (e.g., 50ms).
 *        Includes a software watchdog to recover from stuck Echo states.
 */
void Ultrasonic_Trigger(void)
{
    // --- Software Watchdog: Detect stuck Echo pin or lost pulse ---
    uint8_t echoState = GPIO_ReadInputDataBit(GPIOA, GPIO_Pin_1);
    uint16_t timerValue = TIM_GetCounter(TIM2);
    
    if (echoState == Bit_SET || timerValue > 25000)
    {
        // Echo is stuck HIGH or timer exceeded 25ms -> pulse is lost.
        // Forcefully reset the measurement state machine.
        TIM_Cmd(TIM2, DISABLE);
        TIM_SetCounter(TIM2, 0);
        current_distance_cm = 999;                // Release state machine lock
        EXTI_ClearITPendingBit(EXTI_Line1);       // Prevent ghost interrupts
    }
    
    // --- Standard 10us Trigger pulse generation ---
    // Pull Trig HIGH
    GPIO_SetBits(GPIOA, GPIO_Pin_0);
    
    // 10us delay using tightly bound NOP loop.
    // System runs at 72MHz -> 1 cycle is ~13.8ns. 
    // 720 cycles is approximately 10 microseconds. 
    // This blocks the CPU for just 10 microseconds, which is negligible for the main loop.
    for (volatile uint32_t i = 0; i < 5000; i++) {
        __NOP();
    }
    
    // Pull Trig LOW
    GPIO_ResetBits(GPIOA, GPIO_Pin_0);
}

/**
 * @brief EXTI Interrupt Service Routine for Echo pin (PA1 connected to EXTI Line 1)
 */
void EXTI1_IRQHandler(void)
{
    // Check if EXTI Line 1 triggered
    if (EXTI_GetITStatus(EXTI_Line1) != RESET)
    {
        // Check current pin state to determine the edge type
        if (GPIO_ReadInputDataBit(GPIOA, GPIO_Pin_1) == Bit_SET)
        {
            // Rising Edge: Echo pulse has started
            TIM_Cmd(TIM2, DISABLE);  // Stop timer to safely manipulate counter
            TIM_SetCounter(TIM2, 0); // Reset timer to 0
            TIM_Cmd(TIM2, ENABLE);   // Start timer to count microseconds
        }
        else
        {
            // Falling Edge: Echo pulse has ended
            TIM_Cmd(TIM2, DISABLE); // Stop timer
            uint16_t time_us = TIM_GetCounter(TIM2);
            
            // Calculate distance: distance (cm) = time (us) / 58
            // 58 comes from: 1 us * (340 m/s) / 2 -> 0.017 cm/us -> 1/0.017 roughly equals 58
            
            // Add a bounds check. Valid range of HC-SR04 is 2cm to 400cm
            // 400cm * 58us/cm = 23200us
            // 2cm * 58us/cm = 116us
            if (time_us > 116 && time_us < 24000)
            {
                // Valid range: ~2cm to ~400cm
                current_distance_cm = time_us / 58;
            }
            else if (time_us <= 116 && time_us > 0)
            {
                // Very close object (< 2cm) — treat as 1cm so safety system detects it
                current_distance_cm = 1;
            }
            else
            {
                // Lost pulse or too far — out of bounds
                current_distance_cm = 999;
            }
        }
        
        // Acknowledge and Clear the EXTI Line 1 pending bit
        EXTI_ClearITPendingBit(EXTI_Line1);
    }
}
