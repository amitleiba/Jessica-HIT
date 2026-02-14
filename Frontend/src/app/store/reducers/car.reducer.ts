import { createFeature, createReducer, on } from '@ngrx/store';
import * as CarActions from '../actions/car.actions';
import { CarSensorData, EMPTY_SENSOR_DATA } from '../../shared/models/car-sensor-data.model';

/**
 * Car State
 *
 * currentDirection — the resolved direction string:
 *     "idle"        — no arrows active
 *     "up"          — single direction
 *     "left-right"  — combo (sorted alphabetically, hyphen-joined)
 *
 * isRunning — mirrors the Start/Stop toggle; only when true do we send hub events
 *
 * sensorData — latest raw telemetry snapshot from the car
 */
export interface CarState {
    currentDirection: string;
    isRunning: boolean;
    speed: number;
    sensorData: CarSensorData;
}

export const initialCarState: CarState = {
    currentDirection: 'idle',
    isRunning: false,
    speed: 0,
    sensorData: EMPTY_SENSOR_DATA,
};

export const carReducer = createReducer(
    initialCarState,

    on(CarActions.changeDirection, (state, { direction }) => {
        if (state.currentDirection === direction) {
            return state; // Same direction — no state change
        }
        console.log(`[Car Reducer] Direction: "${state.currentDirection}" → "${direction}"`);
        return {
            ...state,
            currentDirection: direction,
        };
    }),

    on(CarActions.clearDirection, (state) => {
        if (state.currentDirection === 'idle') {
            return state;
        }
        console.log('[Car Reducer] Direction cleared → idle');
        return {
            ...state,
            currentDirection: 'idle',
        };
    }),

    on(CarActions.startCar, (state) => {
        console.log('[Car Reducer] Car STARTED');
        return {
            ...state,
            isRunning: true,
        };
    }),

    on(CarActions.stopCar, (state) => {
        console.log('[Car Reducer] Car STOPPED → idle');
        return {
            ...state,
            isRunning: false,
            currentDirection: 'idle',
        };
    }),

    on(CarActions.changeSpeed, (state, { speed }) => {
        if (state.speed === speed) {
            return state;
        }
        console.log(`[Car Reducer] Speed: ${state.speed} → ${speed}`);
        return {
            ...state,
            speed,
        };
    }),

    on(CarActions.sensorDataReceived, (state, { sensorData }) => {
        console.log('[Car Reducer] Sensor data updated');
        return {
            ...state,
            sensorData: { ...state.sensorData, ...sensorData },
        };
    }),

    on(CarActions.clearSensorData, (state) => {
        console.log('[Car Reducer] Sensor data cleared');
        return {
            ...state,
            sensorData: EMPTY_SENSOR_DATA,
        };
    })
);

/**
 * Car Feature — auto-generates selectors:
 *   carFeature.selectCurrentDirection
 *   carFeature.selectIsRunning
 *   carFeature.selectSpeed
 *   carFeature.selectSensorData
 *   carFeature.selectCarState  (entire slice)
 */
export const carFeature = createFeature({
    name: 'car',
    reducer: carReducer,
});
