import { createFeature, createReducer, on } from '@ngrx/store';
import * as CarActions from '../actions/car.actions';

/**
 * Car State
 *
 * currentDirection — the resolved direction string:
 *     "idle"        — no arrows active
 *     "up"          — single direction
 *     "left-right"  — combo (sorted alphabetically, hyphen-joined)
 *
 * isRunning — mirrors the Start/Stop toggle; only when true do we send hub events
 */
export interface CarState {
    currentDirection: string;
    isRunning: boolean;
}

export const initialCarState: CarState = {
    currentDirection: 'idle',
    isRunning: false,
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
    })
);

/**
 * Car Feature — auto-generates selectors:
 *   carFeature.selectCurrentDirection
 *   carFeature.selectIsRunning
 *   carFeature.selectCarState  (entire slice)
 */
export const carFeature = createFeature({
    name: 'car',
    reducer: carReducer,
});
