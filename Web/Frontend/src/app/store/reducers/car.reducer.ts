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
 * sensorData — latest raw telemetry snapshot from the car
 */
export interface CarState {
    currentDirection: string;
    speed: number;
    sensorData: CarSensorData;
}

export const initialCarState: CarState = {
    currentDirection: 'idle',
    speed: 50,
    sensorData: EMPTY_SENSOR_DATA,
};

export const carReducer = createReducer(
    initialCarState,

    on(CarActions.changeDirection, (state, { direction }) => {
        if (state.currentDirection === direction) {
            return state; // Same direction — no state change
        }
        return {
            ...state,
            currentDirection: direction,
        };
    }),

    on(CarActions.clearDirection, (state) => {
        if (state.currentDirection === 'idle') {
            return state;
        }
        return {
            ...state,
            currentDirection: 'idle',
        };
    }),

    on(CarActions.changeSpeed, (state, { speed }) => {
        if (state.speed === speed) {
            return state;
        }
        return {
            ...state,
            speed,
        };
    }),

    on(CarActions.sensorDataReceived, (state, { sensorData }) => ({
        ...state,
        sensorData: { ...state.sensorData, ...sensorData },
    })),

    on(CarActions.clearSensorData, (state) => ({
        ...state,
        sensorData: EMPTY_SENSOR_DATA,
    }))
);

/**
 * Car Feature — auto-generates selectors:
 *   carFeature.selectCurrentDirection
 *   carFeature.selectSpeed
 *   carFeature.selectSensorData
 *   carFeature.selectCarState  (entire slice)
 */
export const carFeature = createFeature({
    name: 'car',
    reducer: carReducer,
});
