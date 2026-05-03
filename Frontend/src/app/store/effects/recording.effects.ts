import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { tap, switchMap, map, catchError } from 'rxjs/operators';
import { RecordingService } from '../../core/services/recording.service';
import { SignalManagerService } from '../../core/services/signal-manager.service';
import * as RecordingActions from '../actions/recording.actions';
import * as CarActions from '../actions/car.actions';
import { Store } from '@ngrx/store';

/**
 * Recording Effects
 *
 * Side-effect bridge between the Recording store slice, the RecordingService,
 * and the Car store (for starting/stopping the car during record/replay).
 */
@Injectable()
export class RecordingEffects {
    private readonly actions$ = inject(Actions);
    private readonly store = inject(Store);
    private readonly recordingService = inject(RecordingService);
    private readonly signalManager = inject(SignalManagerService);

    // ═══════════════════════════════════════════
    //  Load recordings list
    // ═══════════════════════════════════════════

    loadRecordings$ = createEffect(() =>
        this.actions$.pipe(
            ofType(RecordingActions.loadRecordings),
            switchMap(() =>
                this.recordingService.getRecordings().pipe(
                    map((recordings) =>
                        RecordingActions.loadRecordingsSuccess({ recordings })
                    ),
                    catchError((error) =>
                        of(
                            RecordingActions.loadRecordingsFailure({
                                error: error?.message ?? 'Failed to load recordings',
                            })
                        )
                    )
                )
            )
        )
    );

    // ═══════════════════════════════════════════
    //  Start recording — begin local capture + start car
    // ═══════════════════════════════════════════

    startRecording$ = createEffect(
        () =>
            this.actions$.pipe(
                ofType(RecordingActions.startRecording),
                tap(({ name, speed }) => {
                    console.log(
                        `[RecordingEffect] 🔴 Starting recording "${name}" at speed ${speed}`
                    );

                    // 1. Start local event capture
                    this.recordingService.startCapture(name, speed);

                    // 2. Set speed so direction commands use the correct speed
                    this.store.dispatch(CarActions.changeSpeed({ speed }));
                })
            ),
        { dispatch: false }
    );

    // ═══════════════════════════════════════════
    //  Stop recording — stop capture + stop car + save to backend
    // ═══════════════════════════════════════════

    stopRecording$ = createEffect(() =>
        this.actions$.pipe(
            ofType(RecordingActions.stopRecording),
            switchMap(() => {
                // 1. Send idle direction to stop motion
                this.store.dispatch(CarActions.changeDirection({ direction: 'idle' }));

                // 2. Collect the captured data
                const request = this.recordingService.stopCapture();

                console.log(
                    `[RecordingEffect] ⏹ Saving recording "${request.name}" — ${request.events.length} events`
                );

                // 3. POST to backend
                return this.recordingService.createRecording(request).pipe(
                    map((recording) =>
                        RecordingActions.saveRecordingSuccess({ recording })
                    ),
                    catchError((error) =>
                        of(
                            RecordingActions.saveRecordingFailure({
                                error: error?.message ?? 'Failed to save recording',
                            })
                        )
                    )
                );
            })
        )
    );

    // ═══════════════════════════════════════════
    //  Delete recording
    // ═══════════════════════════════════════════

    deleteRecording$ = createEffect(() =>
        this.actions$.pipe(
            ofType(RecordingActions.deleteRecording),
            switchMap(({ id }) =>
                this.recordingService.deleteRecording(id).pipe(
                    map(() => RecordingActions.deleteRecordingSuccess({ id })),
                    catchError((error) =>
                        of(
                            RecordingActions.deleteRecordingFailure({
                                error: error?.message ?? 'Failed to delete recording',
                            })
                        )
                    )
                )
            )
        )
    );
}

