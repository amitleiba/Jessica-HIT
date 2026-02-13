/**
 * Car Custom Selectors
 *
 * Base selectors (selectCurrentDirection, selectIsRunning) are auto-generated
 * by carFeature in car.reducer.ts.
 *
 * Add derived / composite selectors here when needed.
 */

import { createSelector } from '@ngrx/store';
import { carFeature } from '../reducers/car.reducer';

/**
 * Can we send direction commands?
 * True only when the car is running AND a direction is set.
 */
export const selectCanSendDirection = createSelector(
    carFeature.selectIsRunning,
    carFeature.selectCurrentDirection,
    (isRunning, direction) => isRunning && direction !== null
);

