#include "PWM_Motor.h"

/**
 * @brief Initialize the Motor Control hardware.
 *        - Direction Pins: PA4, PB5 (Left), PB8, PB9 (Right)
 *        - PWM Pins: TIM4 CH2 (PB7 - Left), TIM4 CH1 (PB6 - Right)
 */
void Motor_Init(void)
{
    // 1. Enable Clocks for GPIOA, GPIOB, AFIO and TIM4
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOA | RCC_APB2Periph_GPIOB | RCC_APB2Periph_AFIO, ENABLE);
    RCC_APB1PeriphClockCmd(RCC_APB1Periph_TIM4, ENABLE);
    
    // 2. (PB4 remap removed — PB4 is no longer used for motor direction)
    
    // 3. Configure Direction Pins as Push-Pull Outputs
    GPIO_InitTypeDef GPIO_InitStructure;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_Out_PP;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;
    
    // Left Motor Direction: PA4
    GPIO_InitStructure.GPIO_Pin = GPIO_Pin_4;
    GPIO_Init(GPIOA, &GPIO_InitStructure);
    
    // Left Motor Direction: PB5 | Right Motor Direction: PB8, PB9
    GPIO_InitStructure.GPIO_Pin = GPIO_Pin_5 | GPIO_Pin_8 | GPIO_Pin_9;
    GPIO_Init(GPIOB, &GPIO_InitStructure);
    
    // 4. Configure PWM Pins (PB6, PB7) as Alternate Function Push-Pull
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_AF_PP;
    GPIO_InitStructure.GPIO_Pin = GPIO_Pin_6 | GPIO_Pin_7;
    GPIO_Init(GPIOB, &GPIO_InitStructure);
    
    // 5. Configure TIM4 TimeBase
    // SystemCoreClock is typical 72MHz.
    // Prescaler = 72-1 (1MHz counter clock). 
    // Period (ARR) = 100-1 to allow directly matching 0-100% duty cycle logic.
    // PWM Frequency = 72MHz / 72 / 100 = 10 kHz
    TIM_TimeBaseInitTypeDef TIM_TimeBaseStructure;
    TIM_TimeBaseStructure.TIM_Period = 100 - 1;       // 100 steps (0 to 99)
    TIM_TimeBaseStructure.TIM_Prescaler = 72 - 1;     // 1 MHz timer clock
    TIM_TimeBaseStructure.TIM_ClockDivision = TIM_CKD_DIV1;
    TIM_TimeBaseStructure.TIM_CounterMode = TIM_CounterMode_Up;
    TIM_TimeBaseInit(TIM4, &TIM_TimeBaseStructure);
    
    // 6. Configure TIM4 Output Compare (PWM Mode 1) for CH1 and CH2
    TIM_OCInitTypeDef TIM_OCInitStructure;
    TIM_OCStructInit(&TIM_OCInitStructure); // Set defaults
    TIM_OCInitStructure.TIM_OCMode = TIM_OCMode_PWM1;
    TIM_OCInitStructure.TIM_OutputState = TIM_OutputState_Enable;
    TIM_OCInitStructure.TIM_Pulse = 0; // Initialize with 0 duty cycle
    TIM_OCInitStructure.TIM_OCPolarity = TIM_OCPolarity_High;
    
    // CH1 (Right Motor PWM -> PB6)
    TIM_OC1Init(TIM4, &TIM_OCInitStructure);
    TIM_OC1PreloadConfig(TIM4, TIM_OCPreload_Enable);
    
    // CH2 (Left Motor PWM -> PB7)
    TIM_OC2Init(TIM4, &TIM_OCInitStructure);
    TIM_OC2PreloadConfig(TIM4, TIM_OCPreload_Enable);
    
    // 7. Enable TIM4 counter
    TIM_Cmd(TIM4, ENABLE);
    
    // Initialize outputs to STOP
    Motor_SetSpeed(0, 0);
}

/**
 * @brief Set the speed and direction for both motors.
 * @param leftSpeed -100 (Full Reverse) to 100 (Full Forward)
 * @param rightSpeed -100 (Full Reverse) to 100 (Full Forward)
 */
void Motor_SetSpeed(int16_t leftSpeed, int16_t rightSpeed)
{
    // Hard limit the speeds safely between -100 and 100
    if(leftSpeed > 100) leftSpeed = 100;
    if(leftSpeed < -100) leftSpeed = -100;
    if(rightSpeed > 100) rightSpeed = 100;
    if(rightSpeed < -100) rightSpeed = -100;
    
    // --- Left Motor Logic (PA4, PB5, TIM4_CH2) ---
    if(leftSpeed > 0)
    {
        // Forward
        GPIO_ResetBits(GPIOA, GPIO_Pin_4);
        GPIO_SetBits(GPIOB, GPIO_Pin_5);
        TIM_SetCompare2(TIM4, leftSpeed);
    }
    else if(leftSpeed < 0)
    {
        // Reverse
        GPIO_SetBits(GPIOA, GPIO_Pin_4);
        GPIO_ResetBits(GPIOB, GPIO_Pin_5);
        TIM_SetCompare2(TIM4, -leftSpeed);
    }
    else
    {
        // Stop: Set PA4 Low, PB5 Low
        GPIO_ResetBits(GPIOA, GPIO_Pin_4);
        GPIO_ResetBits(GPIOB, GPIO_Pin_5);
        TIM_SetCompare2(TIM4, 0);
    }

    // --- Right Motor Logic (PB8, PB9, TIM4_CH1) ---
    if(rightSpeed > 0)
    {
        // Forward
        GPIO_SetBits(GPIOB, GPIO_Pin_8);
        GPIO_ResetBits(GPIOB, GPIO_Pin_9);
        TIM_SetCompare1(TIM4, rightSpeed);
    }
    else if(rightSpeed < 0)
    {
        // Reverse
        GPIO_ResetBits(GPIOB, GPIO_Pin_8);
        GPIO_SetBits(GPIOB, GPIO_Pin_9);
        TIM_SetCompare1(TIM4, -rightSpeed);
    }
    else
    {
        // Stop: Set PB8 Low, PB9 Low
        GPIO_ResetBits(GPIOB, GPIO_Pin_8 | GPIO_Pin_9);
        TIM_SetCompare1(TIM4, 0);
    }
}
