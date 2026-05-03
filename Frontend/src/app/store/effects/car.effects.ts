import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { map, tap, withLatestFrom } from 'rxjs/operators';
import { SignalManagerService } from '../../core/services/signal-manager.service';
import { AlertService } from '../../core/services/alert.service';
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
 *       → this effect sends 'CarDirectionChange' to hub
 *
 * Hub method names use PascalCase to match C# SignalR hub conventions.
 */
@Injectable()
export class CarEffects {
    private readonly actions$ = inject(Actions);
    private readonly store = inject(Store);
    private readonly signalManager = inject(SignalManagerService);
    private readonly alertService = inject(AlertService);
    private lastSafety: number | null = null;

    /**
     * Direction Change → always send via hub.
     *
     * Dedup is already handled by the reducer (returns same state ref if unchanged)
     * and by the ControlPanel (only emits on actual transitions).
     */
    directionChange$ = createEffect(
        () =>
            this.actions$.pipe(
                ofType(CarActions.changeDirection),
                withLatestFrom(
                    this.store.select(carFeature.selectSpeed)
                ),
                tap(([action, speed]) => {
                    console.log(
                        `[CarEffect] ✅ Sending "${action.direction}" @ speed ${speed} to hub`
                    );
                    this.signalManager.send('CarDirectionChange', {
                        direction: action.direction,
                        speed
                    });
                })
            ),
        { dispatch: false }
    );

    /**
     * Speed Change → always send via hub.
     */
    speedChange$ = createEffect(
        () =>
            this.actions$.pipe(
                ofType(CarActions.changeSpeed),
                tap((action) => {
                    console.log(
                        `[CarEffect] ✅ Sending speed ${action.speed} to hub`
                    );
                    this.signalManager.send('CarSpeedChange', { speed: action.speed });
                })
            ),
        { dispatch: false }
    );

    /**
     * Robot status stream from Gateway hub → update telemetry in store.
     */
    robotStatusUpdated$ = createEffect(() =>
        this.signalManager.on<{
            distance: number;
            safety: number;
            mode: number;
            battery: number;
            receivedAtUtc: string;
        }>('RobotStatusUpdated').pipe(
            tap((status) => {
                if (status.safety === 1 && this.lastSafety !== 1) {
                    this.alertService.danger('Something is in front of the car.', 'DANGER');
                }
                this.lastSafety = status.safety;
            }),
            map((status) =>
                CarActions.sensorDataReceived({
                    sensorData: {
                        distance: status.distance,
                        safety: status.safety,
                        mode: status.mode,
                        battery: status.battery,
                        receivedAtUtc: status.receivedAtUtc
                    }
                })
            )
        )
    );
}
