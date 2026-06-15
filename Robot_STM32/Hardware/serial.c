#include "stm32f10x.h"                  // Device header
#include <stdio.h>
#include <stdarg.h>
 
uint16_t Serial_RxData;
uint8_t  Serial_RxFlag;
 
/**
  * @brief  Serial initialization, communication protocol is USART
  * @param  None
  * @retval None
  */
void Serial_Init(void)
{
    //Enable clocks
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_USART1, ENABLE);
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOA, ENABLE);
    
    //Initialize TX communication pin
    GPIO_InitTypeDef GPIO_InitStructure;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_AF_PP;
    GPIO_InitStructure.GPIO_Pin = GPIO_Pin_9;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;
    GPIO_Init(GPIOA, &GPIO_InitStructure);
    //Initialize RX communication pin
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IPU;
    GPIO_InitStructure.GPIO_Pin = GPIO_Pin_10;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;
    GPIO_Init(GPIOA, &GPIO_InitStructure);
    //USART initialization
    USART_InitTypeDef USART_InitStructure;
    USART_InitStructure.USART_BaudRate = 9600;
    USART_InitStructure.USART_HardwareFlowControl = USART_HardwareFlowControl_None;
    USART_InitStructure.USART_Mode = USART_Mode_Tx | USART_Mode_Rx;
    USART_InitStructure.USART_Parity = USART_Parity_No;
    USART_InitStructure.USART_StopBits = USART_StopBits_1;
    USART_InitStructure.USART_WordLength = USART_WordLength_8b;
    USART_Init(USART1, &USART_InitStructure);
 
    //Enable USART1 interrupt
    USART_ITConfig(USART1, USART_IT_RXNE, ENABLE);
    //Interrupt priority grouping
    NVIC_PriorityGroupConfig(NVIC_PriorityGroup_2);
 
    //Interrupt initialization
    NVIC_InitTypeDef NVIC_InitStructure;
    NVIC_InitStructure.NVIC_IRQChannel = USART1_IRQn;
    NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;
    NVIC_InitStructure.NVIC_IRQChannelPreemptionPriority = 1;
    NVIC_InitStructure.NVIC_IRQChannelSubPriority = 1;
    NVIC_Init(&NVIC_InitStructure);
 
    //Enable USART1
    USART_Cmd(USART1, ENABLE);
}
 
/**
  * @brief  Send a single byte of data
  * @param  Byte The data byte to send
  * @retval None
  */
void Serial_SendByte(uint8_t Byte)
{
    USART_SendData(USART1, Byte);
    while (USART_GetFlagStatus(USART1, USART_FLAG_TXE) == RESET);
}
 
/**
  * @brief  Send an array of data
  * @param  Array The data array to send
  * @param  Length Array size
  * @retval None
  */
void Serial_SendArray(uint8_t *Array, uint16_t Length)
{
    uint16_t i;
    for (i = 0; i < Length; i ++)
    {
        Serial_SendByte(Array[i]);
    }
}
 
/**
  * @brief  Send a string
  * @param  String The string to send
  * @retval None
  */
void Serial_SendString(char *String)
{
    uint8_t i;
    for (i = 0; String[i] != '\0'; i++)
    {
        Serial_SendByte(String[i]);
    }
}
 
/**
  * @brief  Power function for Bluetooth
  * @retval Returns X raised to the power of Y
  */
uint32_t Serial_POW(uint32_t X,uint32_t Y)
{
    uint32_t Result = 1;
    while (Y--)
    {
        Result *= X;
    }
    return Result;
}
/**
  * @brief  Send a number
  * @param  Number The number to send
  * @param  Length Number of digits
  * @retval None
  */
void Serial_SendNumber(uint32_t Number, uint8_t Length)
{
    uint8_t i;
    for (i = 0; i < Length; i ++)
    {
        Serial_SendByte(Number / Serial_POW(10, Length - i - 1) % 10 + '0');
    }
}
 
/**
  * @brief  Redirect printf to Bluetooth serial output
  */
int fputc(int ch,FILE *f)
{
    Serial_SendByte(ch);
    return ch;
}
 
/**
  * @brief  Wrapper for sprintf, sends formatted string directly via Bluetooth serial
  */
void Serial_Printf(char *format, ...)
{
    char String[100];
    va_list arg;
    va_start(arg,format);
    vsprintf(String,format,arg);
    va_end(arg);
    Serial_SendString(String);
}
 
/**
  * @brief  Check receive response
  * @param  None
  * @retval 1 = data received successfully, 0 = no data received
  */
uint8_t Serial_GetRxFlag(void)
{
    if (Serial_RxFlag == 1)
    {
        Serial_RxFlag = 0;
        return 1;
    }
    return 0;
}
 
/**
  * @brief  Get received data
  * @param  None
  * @retval Serial_RxData Data received from Bluetooth
  */
uint8_t Serial_GetRxData(void)
{
    return Serial_RxData;
}
 
/**
  * @brief  Receive interrupt handler
  * @param  None
  * @retval None
  */
void USART1_IRQHandler(void)
{
    
    if (USART_GetITStatus(USART1, USART_IT_RXNE) == SET)
    {
        Serial_RxData = USART_ReceiveData(USART1);//Save received data, RXNE flag auto-cleared
        Serial_RxFlag = 1;
        USART_ClearITPendingBit(USART1, USART_IT_RXNE);
    }
            
}
