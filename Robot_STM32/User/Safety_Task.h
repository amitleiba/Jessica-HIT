#ifndef __SAFETY_TASK_H
#define __SAFETY_TASK_H

#include "stm32f10x.h"

/**
 * @brief Return flags for the Safety Task
 */
typedef enum {
    SAFETY_STATE_CLEAR = 0,
    SAFETY_STATE_OBSTACLE_DETECTED = 1
} SafetyState_t;

// Initializes the safety hardware peripherals (e.g. buzzer)
void Safety_Init(void);

// Non-blocking update function to be called from the main loop
SafetyState_t Safety_Task_Update(void);

#endif // __SAFETY_TASK_H
