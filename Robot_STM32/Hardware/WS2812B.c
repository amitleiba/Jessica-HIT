#include "WS2812B.h"
#include "Delay.h"

void RGB_Init(void)
{
 GPIO_InitTypeDef  GPIO_InitStructure;						//Instantiate object
 RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOB, ENABLE);	 	//Enable GPIOB port clock
 GPIO_InitStructure.GPIO_Pin = GPIO_Pin_0;				 	//LED0-->PB.0 port configuration
 GPIO_InitStructure.GPIO_Mode = GPIO_Mode_Out_PP; 		 	//Push-pull output
 GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;		 	//IO speed 50MHz
 GPIO_Init(GPIOB, &GPIO_InitStructure);					 	//Initialize GPIOB.0 with configured parameters
 GPIO_SetBits(GPIOB,GPIO_Pin_0);						 	//PB.0 output HIGH
}
/**
 * Color order is Green-Red-Blue: 0x00 00 00 (00~ff)
 * 
*/

//Rainbow color arrow
uint32_t Rainbow_data[40] = {
	0X000F00,0X030F00,0X0C0C00,0X0D0000,0X0A000A,0X00003F,0X000F0F,0X000F02,
	0X000F00,0X030F00,0X0C0C00,0X0D0000,0X0A000A,0X00003F,0X000F0F,0X000F02,
	0X000F00,0X030F00,0X0C0C00,0X0D0000,0X0A000A,0X00003F,0X000F0F,0X000F02,
	0X000F00,0X030F00,0X0C0C00,0X0D0000,0X0A000A,0X00003F,0X000F0F,0X000F02,
	0X000F00,0X030F00,0X0C0C00,0X0D0000,0X0A000A,0X00003F,0X000F0F,0X000F02,
};

//Right turn arrow, headlights ON
uint32_t Right_data[40] = {
	0X0F1F00,0X000000,0X000000,0X000000,0X000500,0X000000,0X000000,0X0F1F00,
	0X0F1F00,0X000000,0X000000,0X000000,0X000600,0X000600,0X000000,0X0F1F00,
	0X0F1F00,0X000500,0X000500,0X000500,0X000500,0X000500,0X000500,0X0F1F00,
	0X0F1F00,0X000000,0X000000,0X000000,0X000500,0X000500,0X000000,0X0F1F00,
	0X0F1F00,0X000000,0X000000,0X000000,0X000500,0X000000,0X000000,0X0F1F00 
};

//Right turn arrow, headlights OFF
uint32_t Right_data1[40] = {
	0X000000,0X000000,0X000000,0X000000,0X000500,0X000000,0X000000,0X000000,
	0X000000,0X000000,0X000000,0X000000,0X000600,0X000600,0X000000,0X000000,
	0X000000,0X000500,0X000500,0X000500,0X000500,0X000500,0X000500,0X000000,
	0X000000,0X000000,0X000000,0X000000,0X000500,0X000500,0X000000,0X000000,
	0X000000,0X000000,0X000000,0X000000,0X000500,0X000000,0X000000,0X000000 
};

//Turn off arrows, keep headlights ON
uint32_t OFF_arrow_data[40] = {
	0X0F1F00,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X0F1F00,
	0X0F1F00,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X0F1F00,
	0X0F1F00,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X0F1F00,
	0X0F1F00,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X0F1F00,
	0X0F1F00,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X0F1F00 
};

//Left turn arrow, headlights ON
uint32_t Left_data[40] = {
	0X0F1F00,0X000000,0X000000,0X000500,0X000000,0X000000,0X000000,0X0F1F00,
	0X0F1F00,0X000000,0X000500,0X000500,0X000000,0X000000,0X000000,0X0F1F00,
	0X0F1F00,0X000500,0X000500,0X000500,0X000500,0X000500,0X000500,0X0F1F00,
	0X0F1F00,0X000000,0X000500,0X000500,0X000000,0X000000,0X000000,0X0F1F00,
	0X0F1F00,0X000000,0X000000,0X000500,0X000000,0X000000,0X000000,0X0F1F00 
};
//Left turn arrow, headlights OFF
uint32_t Left_data1[40] = {
	0X000000,0X000000,0X000000,0X000500,0X000000,0X000000,0X000000,0X000000,
	0X000000,0X000000,0X000500,0X000500,0X000000,0X000000,0X000000,0X000000,
	0X000000,0X000500,0X000500,0X000500,0X000500,0X000500,0X000500,0X000000,
	0X000000,0X000000,0X000500,0X000500,0X000000,0X000000,0X000000,0X000000,
	0X000000,0X000000,0X000000,0X000500,0X000000,0X000000,0X000000,0X000000
};

//Turn off all lights
uint32_t OFF_data[40] = {
	0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,
	0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,
	0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,
	0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,
	0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000,0X000000 
};

//Obstacle avoidance mode, cyan lights
uint32_t cyan_data[40] = {
	0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,
	0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,
	0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,
	0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,
	0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A,0X0A000A 
};

//Line tracking mode, orange lights
uint32_t orange_data[40] = {
	0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,
	0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,
	0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,
	0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,
	0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00,0X030F00 
};

//Follow mode, pink lights
uint32_t Follow_data[40] = {
	0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,
	0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,
	0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,
	0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,
	0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02,0X000F02
};

void delay_us(u32 nus){        		//Delay function: when n=1, delay is approximately 370ns
	while(nus--);
}

void send_code(uint32_t *sdata){    //Send brightness data
	uint32_t n = 0,m = 0;
	for(uint8_t x = 0;x < 40;x++){  //Send 18 bytes of data; 18 = 3 bytes per LED * 6 LEDs
		n = sdata[x];
		for(uint8_t y = 0;y < 24;y++){
			m = ((n<<y)&0x800000);
			if(m){
				RGB_PIN = 1;        //Set LED control pin HIGH
				delay_us(7);        
				RGB_PIN = 0;
				delay_us(1);    	//Set LED control pin LOW
			}else{
				RGB_PIN = 1;
				delay_us(1);
				RGB_PIN = 0;
				delay_us(7);
			}
		}
	}
}

//Turn off all lights
void OFF_RGB(void)
{
	RGB_PIN = 0;
	Delay_us(310);	    			//Send reset signal (>300us) before data
	send_code(&OFF_data[0]);
	Delay_us(310);	    
	send_code(&OFF_data[0]);        //Send LED data twice to prevent interference
	RGB_PIN = 0;
	Delay_ms(310);        
	RGB_PIN = 1; 
}
 
//Left turn with headlights ON 
void Left_arrow(void)
{
	RGB_PIN = 0;
	Delay_us(310);	    //Send reset signal (>300us) before data
	send_code(&Left_data[0]);
	Delay_us(310);	    
	send_code(&Left_data[0]);        //Send LED data twice to prevent interference
	RGB_PIN = 0;
	Delay_ms(310);        
	RGB_PIN = 1; 
}

//Left turn with headlights OFF
void Left_arrow_OFF_LIGHT(void)
{
	RGB_PIN = 0;
	Delay_us(310);	    //Send reset signal (>300us) before data
	send_code(&Left_data1[0]);
	Delay_us(310);	    
	send_code(&Left_data1[0]);        //Send LED data twice to prevent interference
	RGB_PIN = 0;
	Delay_ms(310);        
	RGB_PIN = 1; 
}
 
//Right turn with headlights ON
void Right_arrow(void)
{
	RGB_PIN = 0;
	Delay_us(310);	    //Send reset signal (>300us) before data
	send_code(&Right_data[0]);
	Delay_us(310);	    
	send_code(&Right_data[0]);        //Send LED data twice to prevent interference
	RGB_PIN = 0;
	Delay_ms(310);        
	RGB_PIN = 1; 
}

//Right turn with headlights OFF
void Right_arrow_OFF_LIGHT(void)
{
	RGB_PIN = 0;
	Delay_us(310);	    //Send reset signal (>300us) before data
	send_code(&Right_data1[0]);
	Delay_us(310);	    
	send_code(&Right_data1[0]);        //Send LED data twice to prevent interference
	RGB_PIN = 0;
	Delay_ms(310);        
	RGB_PIN = 1; 
}

//Turn off turn signals, keep headlights ON
void on_the_headlights(void)	//Turn off direction arrows, keep headlights
{
	RGB_PIN = 0;
	Delay_us(310);	    //Send reset signal (>300us) before data
	send_code(&OFF_arrow_data[0]);
	Delay_us(310);	    
	send_code(&OFF_arrow_data[0]);        //Send LED data twice to prevent interference
	RGB_PIN = 0;
	Delay_ms(310);        
	RGB_PIN = 1; 
}

//Rainbow lights
void Rainbow_mbp(void)
{
	RGB_PIN = 0;
	Delay_us(310);	    //Send reset signal (>300us) before data
	send_code(&Rainbow_data[0]);
	Delay_us(310);	    
	send_code(&Rainbow_data[0]);        //Send LED data twice to prevent interference
	RGB_PIN = 0;
	Delay_ms(310);        
	RGB_PIN = 1; 
}

//Obstacle avoidance mode lights
void OA_mbp(void)
{
	RGB_PIN = 0;
	Delay_us(310);	    //Send reset signal (>300us) before data
	send_code(&cyan_data[0]);
	Delay_us(310);	    
	send_code(&cyan_data[0]);        //Send LED data twice to prevent interference
	RGB_PIN = 0;
	Delay_ms(310);        
	RGB_PIN = 1; 
}

//Line tracking mode lights
void tracking_mbp(void)
{
	RGB_PIN = 0;
	Delay_us(310);	    //Send reset signal (>300us) before data
	send_code(&orange_data[0]);
	Delay_us(310);	    
	send_code(&orange_data[0]);        //Send LED data twice to prevent interference
	RGB_PIN = 0;
	Delay_ms(310);        
	RGB_PIN = 1; 
}

//Follow mode lights
void Follow_mbp(void)
{
	RGB_PIN = 0;
	Delay_us(310);	    //Send reset signal (>300us) before data
	send_code(&Follow_data[0]);
	Delay_us(310);	    
	send_code(&Follow_data[0]);        //Send LED data twice to prevent interference
	RGB_PIN = 0;
	Delay_ms(310);        
	RGB_PIN = 1; 
}
