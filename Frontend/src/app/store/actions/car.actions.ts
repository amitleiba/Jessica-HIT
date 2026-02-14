import { createAction, props } from '@ngrx/store';
import { CarSensorData } from '../../shared/models/car-sensor-data.model';

/**
 * Car Actions
 *
 * Direction values follow the control-panel convention:
 *   single  → "up", "down", "left", "right"
 *   combo   → "left-right", "down-up", "down-left", etc.  (sorted alphabetically, hyphen-joined)
 */

// ── Direction ──

export const changeDirection = createAction(
    '[Car] Change Direction',
    props<{ direction: string }>()
);

export const clearDirection = createAction(
    '[Car] Clear Direction'
);

// ── Running state ──

export const startCar = createAction('[Car] Start');

export const stopCar = createAction('[Car] Stop');

// ── Speed ──

/** User changed the speed dial */
export const changeSpeed = createAction(
    '[Car] Change Speed',
    props<{ speed: number }>()
);

// ── Sensor data ──

/** Dispatched when a new sensor data snapshot arrives from SignalR */
export const sensorDataReceived = createAction(
    '[Car] Sensor Data Received',
    props<{ sensorData: CarSensorData }>()
);

/** Clear all sensor data (e.g. on disconnect) */
export const clearSensorData = createAction(
    '[Car] Clear Sensor Data'
);

