#ifndef __WS2812B_H
#define __WS2812B_H	 
#include "sys.h"

#define RGB_PIN PBout(0)	        // PB0

void RGB_Init(void);                //Initialize RGB LEDs
void Right_arrow(void);             //Right turn arrow, with headlights
void Left_arrow(void);              //Left turn arrow, with headlights
void on_the_headlights(void);       //Turn off turn arrows, keep headlights
void OFF_RGB(void);                 //Turn off all lights
void Rainbow_mbp(void);             //Rainbow color mode
void Left_arrow_OFF_LIGHT(void);    //Left turn signal ON, headlights OFF
void Right_arrow_OFF_LIGHT(void);   //Right turn arrow ON, headlights OFF
void OA_mbp(void);                  //Obstacle avoidance mode, cyan lights
void tracking_mbp(void);            //Line tracking mode, orange lights
void Follow_mbp(void);              //Follow mode, pink lights

#endif
