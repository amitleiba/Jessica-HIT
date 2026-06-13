import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Store } from '@ngrx/store';
import { EMPTY, Subscription, switchMap } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MediaDisplayComponent } from '../../../shared/components/media-display/media-display.component';
import { SensorDataPanelComponent } from '../../../shared/components/sensor-data-panel/sensor-data-panel.component';
import { RecordingService } from '../../../core/services/recording.service';
import { RecordingReplaySchedulerService } from '../../../core/services/recording-replay-scheduler.service';
import { AlertService } from '../../../core/services/alert.service';
import { ConfigService } from '../../../core/services/config.service';
import { AppRoutes } from '../../../core/constants/routes';
import { Recording } from '../../../shared/models/recording.model';
import { CarSensorData } from '../../../shared/models/car-sensor-data.model';
import { carFeature } from '../../../store/reducers/car.reducer';
import * as RecordingActions from '../../../store/actions/recording.actions';
import * as CarActions from '../../../store/actions/car.actions';

/**
 * Full-screen replay: camera + telemetry, no D-pad.
 * Back returns to recordings and stops the run. Pause/Resume freezes only the replay schedule.
 */
@Component({
  selector: 'app-replay-session',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonModule,
    CardModule,
    MediaDisplayComponent,
    SensorDataPanelComponent,
  ],
  templateUrl: './replay-session.component.html',
  styleUrl: './replay-session.component.scss',
})
export class ReplaySessionComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly store = inject(Store);
  private readonly recordingService = inject(RecordingService);
  private readonly replayScheduler = inject(RecordingReplaySchedulerService);
  private readonly alertService = inject(AlertService);
  private readonly configService = inject(ConfigService);
  private readonly subs = new Subscription();
  private dangerRetryTimeout: ReturnType<typeof setTimeout> | null = null;
  private dangerRecoveryActive = false;
  private dangerRetryCount = 0;
  private readonly dangerRetryMs = 2000;
  private readonly maxDangerRetries = 5;

  readonly routes = AppRoutes;

  loadError: string | null = null;
  recording: Recording | null = null;
  loopPlayback = false;

  replayProgress = 0;
  replayElapsed = '00:00';
  currentEventIndex = 0;
  loopCount = 0;

  speed = 0;
  currentDirection = 'idle';
  sensorData: CarSensorData = {};

  mediaType: 'image' | 'video' | 'stream' = 'stream';

  get mediaData(): string | null {
    return this.configService.getCameraUrl();
  }

  ngOnInit(): void {
    this.subs.add(
      this.route.paramMap
        .pipe(
          switchMap((params) => {
            const id = params.get('recordingId');
            if (!id) {
              this.loadError = 'Missing recording';
              return EMPTY;
            }
            this.loopPlayback =
              this.route.snapshot.queryParamMap.get('loop') === '1' ||
              this.route.snapshot.queryParamMap.get('loop') === 'true';
            return this.recordingService.getRecording(id);
          })
        )
        .subscribe({
          next: (rec) => {
            this.recording = rec;
            this.loadError = null;
            this.beginReplay(rec);
          },
          error: (err) => {
            console.error('[ReplaySession] Failed to load recording', err);
            this.loadError = err?.message ?? 'Could not load recording';
          },
        })
    );

    this.subs.add(
      this.store.select(carFeature.selectSpeed).subscribe((s) => {
        this.speed = s;
      })
    );
    this.subs.add(
      this.store.select(carFeature.selectCurrentDirection).subscribe((d) => {
        this.currentDirection = d;
      })
    );
    this.subs.add(
      this.store.select(carFeature.selectSensorData).subscribe((data) => {
        this.sensorData = data;
        this.handleSafetySignal(data.safety);
      })
    );

    console.log('[ReplaySession] Initialized');
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    this.teardownReplay(false);
    console.log('[ReplaySession] Destroyed');
  }

  stopAndExit(): void {
    this.teardownReplay(true);
  }

  togglePause(): void {
    if (this.replayScheduler.isPaused) {
      this.replayScheduler.resume();
    } else {
      this.replayScheduler.pause();
    }
  }

  isReplayPaused(): boolean {
    return this.replayScheduler.isPaused;
  }

  private beginReplay(rec: Recording): void {
    console.log(
      `[ReplaySession] ▶ Start replay "${rec.name}" — loop=${this.loopPlayback}`
    );

    this.store.dispatch(
      RecordingActions.startReplay({
        recordingId: rec.id,
        loop: this.loopPlayback,
      })
    );
    this.store.dispatch(CarActions.changeSpeed({ speed: rec.speed }));

    this.replayProgress = 0;
    this.replayElapsed = '00:00';
    this.currentEventIndex = 0;
    this.loopCount = 0;
    this.replayScheduler.resetLoopCount();

    this.replayScheduler.start(rec, {
      shouldLoop: () => this.loopPlayback,
      onProgressTick: (pct, elapsed) => {
        this.replayProgress = pct;
        this.replayElapsed = elapsed;
      },
      onEventIndex: (idx) => {
        this.currentEventIndex = idx;
      },
      onLooped: (n) => {
        this.loopCount = n;
      },
      onNaturalComplete: () => {
        if (!this.dangerRecoveryActive) {
          this.teardownReplay(true);
        }
      },
    });
  }

  private handleSafetySignal(safety: number | undefined): void {
    if (safety === 1) {
      this.startDangerRecoveryIfNeeded();
      return;
    }

    if (safety === 0 && this.dangerRecoveryActive) {
      this.dangerRecoveryActive = false;
      this.dangerRetryCount = 0;
      this.clearDangerRetryTimeout();
      if (this.recording) {
        this.beginReplay(this.recording);
      }
    }
  }

  private startDangerRecoveryIfNeeded(): void {
    if (this.dangerRecoveryActive || !this.recording) {
      return;
    }

    this.dangerRecoveryActive = true;
    this.dangerRetryCount = 0;
    this.alertService.danger('Obstacle detected. Restarting route from start until path is clear.', 'DANGER');
    this.replayScheduler.stopTimers();
    this.store.dispatch(CarActions.changeDirection({ direction: 'idle' }));
    this.store.dispatch(CarActions.clearDirection());
    this.scheduleDangerRetry();
  }

  private scheduleDangerRetry(): void {
    this.clearDangerRetryTimeout();

    this.dangerRetryTimeout = setTimeout(() => {
      if (!this.dangerRecoveryActive || !this.recording) {
        return;
      }

      this.dangerRetryCount++;

      if (this.dangerRetryCount >= this.maxDangerRetries) {
        this.alertService.danger('Path blocked permanently. Stopping replay.', 'REPLAY CANCELLED');
        this.teardownReplay(true);
        return;
      }

      this.beginReplay(this.recording);

      if (this.dangerRecoveryActive) {
        this.scheduleDangerRetry();
      }
    }, this.dangerRetryMs);
  }

  private clearDangerRetryTimeout(): void {
    if (this.dangerRetryTimeout) {
      clearTimeout(this.dangerRetryTimeout);
      this.dangerRetryTimeout = null;
    }
  }

  /**
   * Stops timers and car; optionally navigates back to the recorder list.
   */
  private teardownReplay(navigateToRecorder: boolean): void {
    this.dangerRecoveryActive = false;
    this.dangerRetryCount = 0;
    this.clearDangerRetryTimeout();
    this.replayScheduler.stopTimers();
    this.store.dispatch(RecordingActions.stopReplay());
    this.store.dispatch(CarActions.changeDirection({ direction: 'idle' }));
    this.store.dispatch(CarActions.clearDirection());
    this.replayProgress = 0;
    this.replayElapsed = '00:00';
    this.currentEventIndex = 0;
    this.loopCount = 0;
    if (navigateToRecorder) {
      this.router.navigate([AppRoutes.RECORDER]);
    }
  }
}
