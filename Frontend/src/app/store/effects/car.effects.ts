import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { tap, withLatestFrom } from 'rxjs/operators';
import { SignalManagerService } from '../../core/services/signal-manager.service';
import { carFeature } from '../reducers/car.reducer';
import * as CarActions from '../actions/car.actions';

/**
 * Car Effects
 *
 * Side-effect bridge between the Car store slice and the SignalR hub.
 *
 * Flow:
 *   ControlPanel emits directionChange("up" | "idle" | "left-right" | …)
 *     → JessicaController dispatches [Car] Change Direction
 *       → this effect checks:
 *           1. Has the direction actually changed? (safety dedup)
 *           2. Is the car running?
 *         → if BOTH true → send 'CarDirectionChange' to hub
 *         → otherwise    → skip
 *
 * Hub method names use PascalCase to match C# SignalR hub conventions.
 */
@Injectable()
export class CarEffects {
    private readonly actions$ = inject(Actions);
    private readonly store = inject(Store);
    private readonly signalManager = inject(SignalManagerService);

    /**
     * Direction Change → send via hub only when the car is running.
     *
     * Dedup is already handled by the reducer (returns same state ref if unchanged)
     * and by the ControlPanel (only emits on actual transitions).
     */
    directionChange$ = createEffect(
        () =>
            this.actions$.pipe(
                ofType(CarActions.changeDirection),
                withLatestFrom(
                    this.store.select(carFeature.selectIsRunning)
                ),
                tap(([action, isRunning]) => {
                    if (isRunning) {
                        console.log(
                            `[CarEffect] ✅ Running → sending "${action.direction}" to hub`
                        );
                        this.signalManager.send('CarDirectionChange', { direction: action.direction });
                    } else {
                        console.log(
                            `[CarEffect] ⛔ STOPPED → "${action.direction}" NOT sent`
                        );
                    }
                })
            ),
        { dispatch: false }
    );

    /**
     * Speed Change → send via hub only when the car is running.
     */
    speedChange$ = createEffect(
        () =>
            this.actions$.pipe(
                ofType(CarActions.changeSpeed),
                withLatestFrom(
                    this.store.select(carFeature.selectIsRunning)
                ),
                tap(([action, isRunning]) => {
                    if (isRunning) {
                        console.log(
                            `[CarEffect] ✅ Running → sending speed ${action.speed} to hub`
                        );
                        this.signalManager.send('CarSpeedChange', { speed: action.speed });
                    } else {
                        console.log(
                            `[CarEffect] ⛔ STOPPED → speed ${action.speed} NOT sent`
                        );
                    }
                })
            ),
        { dispatch: false }
    );

    /**
     * Start Car → notify backend via hub.
     */
    startCar$ = createEffect(
        () =>
            this.actions$.pipe(
                ofType(CarActions.startCar),
                tap(() => {
                    console.log('[CarEffect] ▶ Car started → sending CarStart to hub');
                    this.signalManager.send('CarStart', {});
                })
            ),
        { dispatch: false }
    );

    /**
     * Stop Car → notify backend via hub + send idle direction.
     */
    stopCar$ = createEffect(
        () =>
            this.actions$.pipe(
                ofType(CarActions.stopCar),
                tap(() => {
                    console.log('[CarEffect] ⏹ Car stopped → sending CarStop + idle to hub');
                    this.signalManager.send('CarDirectionChange', { direction: 'idle' });
                    this.signalManager.send('CarStop', {});
                })
            ),
        { dispatch: false }
    );
}
