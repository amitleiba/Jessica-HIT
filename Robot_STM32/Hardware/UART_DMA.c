#include "UART_DMA.h"
#include "PWM_Motor.h"
#include <stdio.h>    // sprintf, sscanf
#include <string.h>   // strncmp

/* ===========================================================================
 * Module-level globals
 * =========================================================================== */
volatile char    rx_buffer[UART_RX_BUFFER_SIZE];
volatile uint8_t new_command_ready = 0;
volatile uint8_t robot_mode       = 0;  // 0 = Manual, 1 = Auto

/* ===========================================================================
 * UART + DMA Initialization
 * =========================================================================== */

/**
 * @brief Initialize USART1 with DMA-based RX and IDLE line interrupt.
 *        - TX: PA9  (Alternate Function Push-Pull)
 *        - RX: PA10 (Input Floating)
 *        - DMA: DMA1_Channel5 (USART1_RX), Normal mode, High priority
 *        - Interrupt: USART1 IDLE line -> USART1_IRQn
 */
void UART_DMA_Init(void)
{
    /* 1. Enable peripheral clocks ----------------------------------------- */
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOA | RCC_APB2Periph_USART1, ENABLE);
    RCC_AHBPeriphClockCmd(RCC_AHBPeriph_DMA1, ENABLE);

    /* 2. GPIO configuration ----------------------------------------------- */
    GPIO_InitTypeDef GPIO_InitStructure;

    // TX (PA9) - Alternate Function Push-Pull, 50 MHz
    GPIO_InitStructure.GPIO_Pin   = GPIO_Pin_9;
    GPIO_InitStructure.GPIO_Mode  = GPIO_Mode_AF_PP;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;
    GPIO_Init(GPIOA, &GPIO_InitStructure);

    // RX (PA10) - Input Floating
    GPIO_InitStructure.GPIO_Pin  = GPIO_Pin_10;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IN_FLOATING;
    GPIO_Init(GPIOA, &GPIO_InitStructure);

    /* 3. USART1: 9600 Baud, 8N1, TX+RX ----------------------------------- */
    USART_InitTypeDef USART_InitStructure;
    USART_InitStructure.USART_BaudRate            = 9600;
    USART_InitStructure.USART_WordLength           = USART_WordLength_8b;
    USART_InitStructure.USART_StopBits             = USART_StopBits_1;
    USART_InitStructure.USART_Parity               = USART_Parity_No;
    USART_InitStructure.USART_HardwareFlowControl   = USART_HardwareFlowControl_None;
    USART_InitStructure.USART_Mode                 = USART_Mode_Tx | USART_Mode_Rx;
    USART_Init(USART1, &USART_InitStructure);

    /* 4. DMA1_Channel5 for USART1_RX -------------------------------------- */
    DMA_InitTypeDef DMA_InitStructure;
    DMA_DeInit(DMA1_Channel5);
    DMA_InitStructure.DMA_PeripheralBaseAddr = (uint32_t)&USART1->DR;
    DMA_InitStructure.DMA_MemoryBaseAddr     = (uint32_t)rx_buffer;
    DMA_InitStructure.DMA_DIR                = DMA_DIR_PeripheralSRC;
    DMA_InitStructure.DMA_BufferSize         = UART_RX_BUFFER_SIZE;
    DMA_InitStructure.DMA_PeripheralInc      = DMA_PeripheralInc_Disable;
    DMA_InitStructure.DMA_MemoryInc          = DMA_MemoryInc_Enable;
    DMA_InitStructure.DMA_PeripheralDataSize = DMA_PeripheralDataSize_Byte;
    DMA_InitStructure.DMA_MemoryDataSize     = DMA_MemoryDataSize_Byte;
    DMA_InitStructure.DMA_Mode               = DMA_Mode_Normal;
    DMA_InitStructure.DMA_Priority           = DMA_Priority_High;
    DMA_InitStructure.DMA_M2M                = DMA_M2M_Disable;
    DMA_Init(DMA1_Channel5, &DMA_InitStructure);

    DMA_Cmd(DMA1_Channel5, ENABLE);

    /* 5. Enable USART1 IDLE line interrupt -------------------------------- */
    USART_ITConfig(USART1, USART_IT_IDLE, ENABLE);

    /* 6. Enable USART1 DMA receive request -------------------------------- */
    USART_DMACmd(USART1, USART_DMAReq_Rx, ENABLE);

    /* 7. NVIC: enable USART1_IRQn ----------------------------------------- */
    NVIC_InitTypeDef NVIC_InitStructure;
    NVIC_InitStructure.NVIC_IRQChannel                   = USART1_IRQn;
    NVIC_InitStructure.NVIC_IRQChannelPreemptionPriority = 2;
    NVIC_InitStructure.NVIC_IRQChannelSubPriority        = 0;
    NVIC_InitStructure.NVIC_IRQChannelCmd                = ENABLE;
    NVIC_Init(&NVIC_InitStructure);

    /* 8. Enable USART1 ---------------------------------------------------- */
    USART_Cmd(USART1, ENABLE);
}

/* ===========================================================================
 * USART1 IDLE Line ISR
 * =========================================================================== */

/**
 * @brief USART1 ISR — triggered on IDLE line detection (end of message).
 *        Reads the received byte count from DMA, null-terminates the buffer,
 *        sets the ready flag, and reloads DMA for next reception.
 */
void USART1_IRQHandler(void)
{
    if (USART_GetITStatus(USART1, USART_IT_IDLE) != RESET)
    {
        /* Clear IDLE flag by sequential read of SR then DR (RM0008 §27.6.1) */
        volatile uint32_t tmp;
        tmp = USART1->SR;
        tmp = USART1->DR;
        (void)tmp;

        /* Pause DMA to safely read the data counter */
        DMA_Cmd(DMA1_Channel5, DISABLE);

        /* Calculate number of bytes received in this frame */
        uint16_t len = UART_RX_BUFFER_SIZE - DMA_GetCurrDataCounter(DMA1_Channel5);

        /* Null-terminate so the buffer can be treated as a C string */
        if (len < UART_RX_BUFFER_SIZE)
        {
            rx_buffer[len] = '\0';
        }
        else
        {
            rx_buffer[UART_RX_BUFFER_SIZE - 1] = '\0';
        }

        /* Signal main loop that a complete command is ready for parsing */
        new_command_ready = 1;

        /* Reload DMA transfer counter for next reception */
        DMA1_Channel5->CNDTR = UART_RX_BUFFER_SIZE;
        DMA_Cmd(DMA1_Channel5, ENABLE);
    }
}

/* ===========================================================================
 * Command Parser
 * =========================================================================== */

/**
 * @brief Parse and execute a received UART command.
 *        Protocol:
 *          $S             -> Emergency Stop
 *          $A,mode        -> Set robot_mode (0=Manual, 1=Auto)
 *          $M,left,right  -> Move (with wall-trap safety override)
 *        Called unconditionally from main loop so the robot never ignores
 *        commands, even when the safety supervisor is active.
 * @param current_safety Current safety state from Safety_Task_Update()
 */
void UART_ProcessCommand(SafetyState_t current_safety)
{
    if (new_command_ready != 1)
    {
        return;  // No new data — nothing to do
    }

    /* Take a local snapshot of the volatile DMA buffer to avoid
     * race conditions during parsing (ISR could fire mid-parse). */
    char cmd_buf[UART_RX_BUFFER_SIZE];
    for (uint8_t i = 0; i < UART_RX_BUFFER_SIZE; i++)
    {
        cmd_buf[i] = rx_buffer[i];
        if (rx_buffer[i] == '\0') break;
    }

    /* --- Parse: Emergency Stop ($S) ------------------------------------ */
    if (strncmp(cmd_buf, "$S", 2) == 0)
    {
        Motor_SetSpeed(0, 0);
    }
    /* --- Parse: Auto/Manual Mode ($A,mode) ----------------------------- */
    else if (strncmp(cmd_buf, "$A,", 3) == 0)
    {
        int mode_val = 0;
        if (sscanf(cmd_buf, "$A,%d", &mode_val) == 1)
        {
            robot_mode = (uint8_t)mode_val;
        }
    }
    /* --- Parse: Move Command ($M,left,right) --------------------------- */
    else if (strncmp(cmd_buf, "$M,", 3) == 0)
    {
        int16_t leftSpeed  = 0;
        int16_t rightSpeed = 0;

        if (sscanf(cmd_buf, "$M,%hd,%hd", &leftSpeed, &rightSpeed) == 2)
        {
            /*
             * Wall-Trap Safety Logic:
             * If Safety_Task reports an obstacle, block any positive (forward)
             * speed commands but allow negative (reverse) so the operator can
             * always back the robot away from a wall.
             */
            if (current_safety == SAFETY_STATE_OBSTACLE_DETECTED)
            {
                if (leftSpeed  > 0) leftSpeed  = 0;
                if (rightSpeed > 0) rightSpeed = 0;
            }

            Motor_SetSpeed(leftSpeed, rightSpeed);
        }
    }

    /* Command consumed — clear the flag for the next reception */
    new_command_ready = 0;
}

/* ===========================================================================
 * Telemetry Transmitter (blocking / polling)
 * =========================================================================== */

/**
 * @brief Transmit a formatted telemetry string over USART1.
 *        Format: "$STATUS,distance,safety_state,mode,battery_v\n"
 *        Uses TXE polling per byte and waits for TC after the last byte.
 *        Battery voltage is formatted as "X.XX" using integer arithmetic
 *        to avoid pulling in the full printf float support.
 * @param distance      Current ultrasonic distance in cm
 * @param safety_state  Current safety state (0=CLEAR, 1=OBSTACLE)
 * @param mode          Current robot mode (0=Manual, 1=Auto)
 * @param battery_v     Battery voltage in volts (float)
 */
void UART_SendTelemetry(uint16_t distance, uint8_t safety_state,
                        uint8_t mode, float battery_v)
{
    /* Split float voltage into integer + 2-decimal-place fractional parts
     * to avoid %f which requires heavyweight float formatting support. */
    int volt_int  = (int)battery_v;
    int volt_frac = (int)((battery_v - (float)volt_int) * 100.0f);
    if (volt_frac < 0) volt_frac = -volt_frac;  /* safety: keep fraction positive */

    char tx_buf[64];
    int len = sprintf(tx_buf, "$STATUS,%u,%u,%u,%d.%02d\n",
                      (unsigned)distance,
                      (unsigned)safety_state,
                      (unsigned)mode,
                      volt_int, volt_frac);

    for (int i = 0; i < len; i++)
    {
        /* Wait until Transmit Data Register is empty */
        while (USART_GetFlagStatus(USART1, USART_FLAG_TXE) == RESET);
        USART_SendData(USART1, (uint8_t)tx_buf[i]);
    }

    /* Wait until Transmission Complete to guarantee last byte is fully sent */
    while (USART_GetFlagStatus(USART1, USART_FLAG_TC) == RESET);
}

/* ===========================================================================
 * Solar Panel ADC (ADC1, Channel 2, PA2)
 * =========================================================================== */

/**
 * @brief Initialize ADC1 on PA2 (Channel 2) for single software-triggered
 *        conversion.  Uses independent mode, single-channel, right-aligned
 *        12-bit output.  Performs the mandatory ADC self-calibration after
 *        power-on.
 */
void Battery_ADC_Init(void)
{
    /* 1. Enable clocks for GPIOA and ADC1 --------------------------------- */
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOA | RCC_APB2Periph_ADC1, ENABLE);

    /* ADC clock prescaler: PCLK2 / 6 = 12 MHz (must be <= 14 MHz) */
    RCC_ADCCLKConfig(RCC_PCLK2_Div6);

    /* 2. Configure PA2 as Analog Input ------------------------------------ */
    GPIO_InitTypeDef GPIO_InitStructure;
    GPIO_InitStructure.GPIO_Pin  = GPIO_Pin_2;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_AIN;
    GPIO_Init(GPIOA, &GPIO_InitStructure);

    /* 3. Configure ADC1 --------------------------------------------------- */
    ADC_InitTypeDef ADC_InitStructure;
    ADC_InitStructure.ADC_Mode               = ADC_Mode_Independent;
    ADC_InitStructure.ADC_ScanConvMode       = DISABLE;           // Single channel
    ADC_InitStructure.ADC_ContinuousConvMode = DISABLE;           // Single conversion
    ADC_InitStructure.ADC_ExternalTrigConv   = ADC_ExternalTrigConv_None;
    ADC_InitStructure.ADC_DataAlign          = ADC_DataAlign_Right;
    ADC_InitStructure.ADC_NbrOfChannel       = 1;
    ADC_Init(ADC1, &ADC_InitStructure);

    /* Configure Channel 2 (PA2), 239.5-cycle sample time for stability */
    ADC_RegularChannelConfig(ADC1, ADC_Channel_2, 1, ADC_SampleTime_239Cycles5);

    /* 4. Enable ADC1 ------------------------------------------------------ */
    ADC_Cmd(ADC1, ENABLE);

    /* 5. ADC self-calibration (mandatory after power-on) ------------------ */
    ADC_ResetCalibration(ADC1);
    while (ADC_GetResetCalibrationStatus(ADC1));

    ADC_StartCalibration(ADC1);
    while (ADC_GetCalibrationStatus(ADC1));
}

/**
 * @brief Trigger a single software ADC conversion on Channel 2, block until
 *        End-Of-Conversion, and return the true solar panel voltage in volts.
 *        Assumes Vref+ = 3.3 V, 12-bit resolution (0–4095), and an external
 *        1:3 resistive voltage divider on the panel output (multiply by 3).
 * @return Solar panel voltage in volts (float).  Range 0.0 – 9.9 V.
 */
float Battery_Read_Voltage(void)
{
    /* Start software conversion */
    ADC_SoftwareStartConvCmd(ADC1, ENABLE);

    /* Wait for End Of Conversion flag */
    while (ADC_GetFlagStatus(ADC1, ADC_FLAG_EOC) == RESET);

    /* Read the 12-bit result */
    uint16_t raw = ADC_GetConversionValue(ADC1);

    /* Convert to pin voltage: Vpin = raw * 3.3 / 4095 */
    float voltage = (float)raw * 3.3f / 4095.0f;

    /* Account for 1:3 voltage divider: Vpanel = Vpin * 3 */
    voltage *= 3.0f;

    return voltage;
}
