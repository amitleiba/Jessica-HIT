import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ControlPanelComponent } from '../../../../shared/components/control-panel/control-panel.component';
import { MediaDisplayComponent } from '../../../../shared/components/media-display/media-display.component';
import { SensorDataPanelComponent } from '../../../../shared/components/sensor-data-panel/sensor-data-panel.component';
import { RecordingService } from '../../../../core/services/recording.service';
import { recordingFeature } from '../../../../store/reducers/recording.reducer';
import { carFeature } from '../../../../store/reducers/car.reducer';
import * as RecordingActions from '../../../../store/actions/recording.actions';
import * as CarActions from '../../../../store/actions/car.actions';

/**
 * CreateRecordingComponent — "Create" tab inside the Recorder Panel.
 *
 * Flow:
 *   1. User fills in: recording name + speed (slider)
 *   2. User clicks "Start Recording"
 *      → local capture begins (RecordingService.startCapture)
 *      → car starts (speed set + CarStart dispatched)
 *      → Full driving dashboard appears (camera + D-pad + sensor panel)
 *   3. User drives with the D-pad — every direction change is:
 *      a) Dispatched to the Car store (sends to SignalR hub → real car moves)
 *      b) Captured locally with ms timestamp (RecordingService.captureDirection)
 *   4. User clicks "Stop Recording"
 *      → capture stops, car stops
 *      → POST to backend to persist the recording
 */
@Component({
    selector: 'app-create-recording',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ButtonModule,
        InputTextModule,
        ControlPanelComponent,
        MediaDisplayComponent,
        SensorDataPanelComponent,
    ],
    templateUrl: './create-recording.component.html',
    styleUrl: './create-recording.component.scss',
})
export class CreateRecordingComponent implements OnInit, OnDestroy {
    private readonly store = inject(Store);
    readonly recordingService = inject(RecordingService);
    private subscriptions = new Subscription();

    // ── Form fields (pre-recording) ──
    recordingName = '';
    recordingSpeed = 50;

    // ── State ──
    isRecording = false;
    eventCount = 0;
    currentSpeed = 0;

    // ── Camera feed ──
    mediaData: string | null = null;
    mediaType: 'image' | 'video' = 'image';

    // ── Timer display ──
    elapsedDisplay = '00:00';
    private timerInterval: ReturnType<typeof setInterval> | null = null;

    // ── Validation ──
    get canStart(): boolean {
        return this.recordingName.trim().length > 0 && !this.isRecording;
    }

    // ── Lifecycle ──

    ngOnInit(): void {
        this.subscriptions.add(
            this.store.select(recordingFeature.selectIsRecording).subscribe((isRec) => {
                this.isRecording = isRec;

                if (isRec) {
                    this.startTimer();
                } else {
                    this.stopTimer();
                }
            })
        );

        this.subscriptions.add(
            this.store.select(carFeature.selectSpeed).subscribe((speed) => {
                this.currentSpeed = speed;
            })
        );

        console.log('[CreateRecording] Initialized');
    }

    ngOnDestroy(): void {
        this.stopTimer();
        this.subscriptions.unsubscribe();

        // Safety: if user navigates away while recording, stop it
        if (this.isRecording) {
            this.store.dispatch(RecordingActions.stopRecording());
            this.store.dispatch(CarActions.stopCar());
        }

        console.log('[CreateRecording] Destroyed');
    }

    // ── Actions ──

    onStartRecording(): void {
        if (!this.canStart) return;

        console.log(
            `[CreateRecording] ▶ Starting recording "${this.recordingName}" at speed ${this.recordingSpeed}`
        );

        this.store.dispatch(
            RecordingActions.startRecording({
                name: this.recordingName.trim(),
                speed: this.recordingSpeed,
            })
        );
    }

    onStopRecording(): void {
        console.log('[CreateRecording] ⏹ Stopping recording');
        this.store.dispatch(RecordingActions.stopRecording());

        // Reset form for next recording
        this.recordingName = '';
        this.recordingSpeed = 50;
        this.eventCount = 0;
    }

    /**
     * Direction change from the D-pad during recording.
     * We do TWO things:
     *   1. Dispatch to Car store → sends to SignalR hub (real car moves)
     *   2. Capture in RecordingService with ms timestamp (local buffer)
     */
    onDirectionChange(direction: string): void {
        // 1. Normal car control flow
        this.store.dispatch(CarActions.changeDirection({ direction }));

        // 2. Capture for recording
        this.recordingService.captureDirection(direction);
        this.eventCount = this.recordingService.eventCount;
    }

    onSpeedInput(event: Event): void {
        this.recordingSpeed = +(event.target as HTMLInputElement).value;
    }

    onSpeedChange(speed: number): void {
        console.log(`[CreateRecording] Speed dial → ${speed}`);
        this.store.dispatch(CarActions.changeSpeed({ speed }));
    }

    // ── Timer helpers ──

    private startTimer(): void {
        this.stopTimer();
        this.timerInterval = setInterval(() => {
            const ms = this.recordingService.elapsedMs;
            this.elapsedDisplay = this.formatMs(ms);
            this.eventCount = this.recordingService.eventCount;
        }, 100);
    }

    private stopTimer(): void {
        if (this.timerInterval) {
            clearInterval(this.timerInterval);
            this.timerInterval = null;
        }
    }

    private formatMs(ms: number): string {
        const totalSeconds = Math.floor(ms / 1000);
        const minutes = Math.floor(totalSeconds / 60);
        const seconds = totalSeconds % 60;
        return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
    }
}
