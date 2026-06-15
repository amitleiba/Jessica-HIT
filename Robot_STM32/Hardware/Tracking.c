//Macro definitions: pins can be chosen according to STM32 pin definition table
#include "stm32f10x.h"                  // Device header
#include "Tracking.h"

#define track_left  GPIO_Pin_13               //Define left sensor module connected to C13
#define track_middle GPIO_Pin_14              //Define middle sensor module connected to C14   
#define track_right  GPIO_Pin_15              //Define right sensor module connected to C15
 
//----------Initialize line tracking module-------- 
void Track_Init(void)
{
	//Structure initialization
	RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOC,ENABLE);              //Enable GPIOC peripheral clock
	GPIO_InitTypeDef GPIO_InitTypeStructure;
 
	GPIO_InitTypeStructure.GPIO_Mode=GPIO_Mode_IPU;                   //Use pull-up input mode
	GPIO_InitTypeStructure.GPIO_Pin=track_left|track_middle|track_right ;               //Bind pins
	GPIO_InitTypeStructure.GPIO_Speed=GPIO_Speed_50MHz;               //Set IO port speed
	GPIO_Init(GPIOC,&GPIO_InitTypeStructure);                         //Bind to GPIOC
}

//----------Read high/low level based on black/white line detection-------- 
int Get_State(uint16_t choice)
{
	uint16_t get=0;                                           
	switch(choice)
	{
		case(1): get= GPIO_ReadInputDataBit(GPIOC,track_left);break;    //Get left sensor level
		case(2): get=GPIO_ReadInputDataBit(GPIOC,track_middle);break;   //Get middle sensor level
		case(3): get=GPIO_ReadInputDataBit(GPIOC,track_right);break;    //Get right sensor level
	}
	return get;
}
