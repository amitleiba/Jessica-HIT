#include "stm32f10x.h"                  // Device header
#include "HCSR04.h"
#include "Delay.h"

uint16_t Time;

void Timer_Init(void)
{
	Time = 0;
	RCC_APB1PeriphClockCmd(RCC_APB1Periph_TIM2, ENABLE);	//Select Timer2 on APB1 bus
	TIM_InternalClockConfig(TIM2);		//TIM2 uses internal clock
	TIM_TimeBaseInitTypeDef TIM_TimeBaseInitStructure;
	TIM_TimeBaseInitStructure.TIM_ClockDivision = TIM_CKD_DIV1;
	TIM_TimeBaseInitStructure.TIM_CounterMode = TIM_CounterMode_Up;		//Counter mode: count up
	TIM_TimeBaseInitStructure.TIM_Period = 7199;		//ARR: 1 tick = 0.0001S
	TIM_TimeBaseInitStructure.TIM_Prescaler = 0;		//PSC
	TIM_TimeBaseInitStructure.TIM_RepetitionCounter = 0;		//Advanced timer feature: repetition counter
	TIM_TimeBaseInit(TIM2, &TIM_TimeBaseInitStructure);
	
	TIM_ClearFlag(TIM2, TIM_FLAG_Update);
	TIM_ITConfig(TIM2, TIM_IT_Update, ENABLE);		//Enable interrupt
	
	NVIC_PriorityGroupConfig(NVIC_PriorityGroup_2);
	
	NVIC_InitTypeDef NVIC_InitStructure;
	NVIC_InitStructure.NVIC_IRQChannel = TIM2_IRQn;		//Interrupt channel selection
	NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;
	NVIC_InitStructure.NVIC_IRQChannelPreemptionPriority = 2;
	NVIC_InitStructure.NVIC_IRQChannelSubPriority = 1;		//Sub-priority
	
	NVIC_Init(&NVIC_InitStructure);
	
	TIM_Cmd(TIM2, ENABLE);		//Enable timer
}

void TIM2_IRQHandler()		//Timer2 interrupt handler
{
	if(TIM_GetITStatus(TIM2, TIM_IT_Update) == SET)
	{

		if (GPIO_ReadInputDataBit(Echo_Port, Echo_Pin) == 1)
		{
			Time ++;
		}
		TIM_ClearITPendingBit(TIM2, TIM_IT_Update);		//Clear interrupt flag
	}
}

void HCSR04_Init(void)
{
	RCC_APB2PeriphClockCmd(Trig_RCC, ENABLE);
	
	GPIO_InitTypeDef GPIO_InitStruct;
	GPIO_InitStruct.GPIO_Mode = GPIO_Mode_Out_PP;	//Output mode: push-pull
	GPIO_InitStruct.GPIO_Pin = Trig_Pin;
	GPIO_InitStruct.GPIO_Speed = GPIO_Speed_50MHz;
	GPIO_Init(Trig_Port, &GPIO_InitStruct);

	GPIO_InitStruct.GPIO_Mode = GPIO_Mode_IPD;		//Input mode: pull-down
	GPIO_InitStruct.GPIO_Pin = Echo_Pin;
	GPIO_InitStruct.GPIO_Speed = GPIO_Speed_50MHz;
	GPIO_Init(Echo_Port, &GPIO_InitStruct);
	
	GPIO_ResetBits(Trig_Port, Trig_Pin);
}

void HCSR04_Start()
{
	GPIO_SetBits(Trig_Port, Trig_Pin);
	Delay_us(45);
	GPIO_ResetBits(Trig_Port, Trig_Pin);
	Timer_Init();
}

uint16_t HCSR04_GetValue(void)
{
	HCSR04_Start();
	Delay_ms(100);
	return ((Time * 0.0001) * 34000) / 2;
}
