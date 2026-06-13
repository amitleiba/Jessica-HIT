#ifndef __ULTRASONIC_NONBLOCKING_H
#define __ULTRASONIC_NONBLOCKING_H

#include "stm32f10x.h"

// Global distance value in cm. Updated automatically via EXTI mechanism.
// A distance of 999 implies out-of-range or invalid reading.
extern volatile uint16_t current_distance_cm;

void Ultrasonic_Init(void);
void Ultrasonic_Trigger(void);

#endif // __ULTRASONIC_NONBLOCKING_H
