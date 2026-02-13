import { createAction, props } from '@ngrx/store';

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

