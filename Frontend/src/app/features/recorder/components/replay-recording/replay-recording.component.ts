import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { ToggleButtonModule } from 'primeng/togglebutton';
import { FormsModule } from '@angular/forms';
import { RecordingService } from '../../../../core/services/recording.service';
import { recordingFeature } from '../../../../store/reducers/recording.reducer';
import * as RecordingActions from '../../../../store/actions/recording.actions';
import * as CarActions from '../../../../store/actions/car.actions';
import {
  RecordingSummary,
  Recording,
  RecordingEvent,
} from '../../../../shared/models/recording.model';

/**
 * ReplayRecordingComponent — "Replay" tab inside the Recorder Panel.
 *
 * Flow:
 *   1. User sees a list of their recordings.
 *   2. User selects a recording → details load (with events).
 *   3. User clicks "Play" → car starts at the recording's speed,
 *      and each event is dispatched at its offsetMs using setTimeout.
 *   4. User can toggle "Loop" to repeat the recording infinitely.
 *   5. User can click "Stop" at any time.
 *
 * The replay scheduler is managed locally in this component (not NgRx)
 * because setTimeout scheduling is inherently imperative.
 * NgRx only tracks high-level flags (isReplaying, replayRecordingId, isLoopEnabled).
 */
@Component({
  selector: 'app-replay-recording',
  standalone: true,
  imports: [CommonModule, ButtonModule, ToggleButtonModule, FormsModule],
  templateUrl: './replay-recording.component.html',
  styleUrl: './replay-recording.component.scss',
})
export class ReplayRecordingComponent implements OnInit, OnDestroy {
  private readonly store = inject(Store);
  private readonly recordingService = inject(RecordingService);
  private subscriptions = new Subscription();

  // ── State from store ──
  recordings: RecordingSummary[] = [];
  isLoading = false;
  isReplaying = false;
  isLoopEnabled = false;

  // ── Local replay state ──
  selectedRecording: Recording | null = null;
  isLoadingDetail = false;
  replayProgress = 0; // 0-100
  replayElapsed = '00:00';
  currentEventIndex = 0;
  loopCount = 0;

  // ── Replay scheduler ──
  private replayTimeouts: ReturnType<typeof setTimeout>[] = [];
  private replayStartTime = 0;
  private progressInterval: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.store.dispatch(RecordingActions.loadRecordings());

    this.subscriptions.add(
      this.store.select(recordingFeature.selectRecordings).subscribe((recs) => {
        this.recordings = recs;
      })
    );

    this.subscriptions.add(
      this.store.select(recordingFeature.selectIsLoadingList).subscribe((l) => {
        this.isLoading = l;
      })
    );

    this.subscriptions.add(
      this.store.select(recordingFeature.selectIsReplaying).subscribe((r) => {
        this.isReplaying = r;
      })
    );

    this.subscriptions.add(
      this.store.select(recordingFeature.selectIsLoopEnabled).subscribe((l) => {
        this.isLoopEnabled = l;
      })
    );

    console.log('[ReplayRecording] Initialized');
  }

  ngOnDestroy(): void {
    this.stopReplayScheduler();
    this.subscriptions.unsubscribe();

    // Safety: stop replay if navigating away
    if (this.isReplaying) {
      this.store.dispatch(RecordingActions.stopReplay());
      this.store.dispatch(CarActions.stopCar());
    }

    console.log('[ReplayRecording] Destroyed');
  }

  // ═══════════════════════════════════════════
  //  Selection
  // ═══════════════════════════════════════════

  onSelectRecording(rec: RecordingSummary): void {
    if (this.isReplaying) return; // Don't switch while replaying

    console.log(`[ReplayRecording] Selected recording: "${rec.name}" (${rec.id})`);
    this.isLoadingDetail = true;
    this.selectedRecording = null;

    this.recordingService.getRecording(rec.id).subscribe({
      next: (detail) => {
        this.selectedRecording = detail;
        this.isLoadingDetail = false;
        console.log(
          `[ReplayRecording] Loaded recording detail: ${detail.events.length} events, ${detail.durationMs}ms`
        );
      },
      error: (err) => {
        console.error('[ReplayRecording] Failed to load recording detail', err);
        this.isLoadingDetail = false;
      },
    });
  }

  // ═══════════════════════════════════════════
  //  Replay controls
  // ═══════════════════════════════════════════

  onPlay(): void {
    if (!this.selectedRecording) return;

    console.log(
      `[ReplayRecording] ▶ Playing "${this.selectedRecording.name}" — loop=${this.isLoopEnabled}`
    );

    this.store.dispatch(
      RecordingActions.startReplay({
        recordingId: this.selectedRecording.id,
        loop: this.isLoopEnabled,
      })
    );

    // Start the car at the recording's speed
    this.store.dispatch(
      CarActions.changeSpeed({ speed: this.selectedRecording.speed })
    );
    this.store.dispatch(CarActions.startCar());

    // Start scheduling events
    this.loopCount = 0;
    this.scheduleReplay(this.selectedRecording);
  }

  onStop(): void {
    console.log('[ReplayRecording] ⏹ Stopping replay');
    this.stopReplayScheduler();
    this.store.dispatch(RecordingActions.stopReplay());
    this.store.dispatch(CarActions.stopCar());
    this.store.dispatch(CarActions.clearDirection());
    this.replayProgress = 0;
    this.replayElapsed = '00:00';
    this.currentEventIndex = 0;
    this.loopCount = 0;
  }

  onToggleLoop(): void {
    this.store.dispatch(RecordingActions.toggleLoop());
  }

  // ═══════════════════════════════════════════
  //  Replay scheduler (setTimeout-based)
  // ═══════════════════════════════════════════

  private scheduleReplay(recording: Recording): void {
    this.clearTimeouts();
    this.currentEventIndex = 0;
    this.replayStartTime = performance.now();

    // Start progress tracker
    this.startProgressTracker(recording.durationMs);

    // Schedule each event
    recording.events.forEach((event, index) => {
      const timeout = setTimeout(() => {
        this.currentEventIndex = index + 1;

        console.log(
          `[ReplayRecording] 📌 Event ${index + 1}/${recording.events.length}: "${event.direction}" @ ${event.offsetMs}ms`
        );

        // Dispatch the direction change to the car
        this.store.dispatch(
          CarActions.changeDirection({ direction: event.direction })
        );
      }, event.offsetMs);

      this.replayTimeouts.push(timeout);
    });

    // Schedule end-of-recording
    const endTimeout = setTimeout(() => {
      this.loopCount++;

      if (this.isLoopEnabled) {
        console.log(
          `[ReplayRecording] 🔄 Loop iteration ${this.loopCount} complete — restarting`
        );
        this.scheduleReplay(recording); // Restart
      } else {
        console.log('[ReplayRecording] ✅ Replay complete');
        this.onStop();
      }
    }, recording.durationMs + 100); // Small buffer after last event

    this.replayTimeouts.push(endTimeout);
  }

  private startProgressTracker(durationMs: number): void {
    this.stopProgressTracker();
    this.progressInterval = setInterval(() => {
      const elapsed = performance.now() - this.replayStartTime;
      const progress = Math.min(100, (elapsed / durationMs) * 100);
      this.replayProgress = Math.round(progress);
      this.replayElapsed = this.formatMs(elapsed);
    }, 100);
  }

  private stopProgressTracker(): void {
    if (this.progressInterval) {
      clearInterval(this.progressInterval);
      this.progressInterval = null;
    }
  }

  private clearTimeouts(): void {
    this.replayTimeouts.forEach(clearTimeout);
    this.replayTimeouts = [];
  }

  private stopReplayScheduler(): void {
    this.clearTimeouts();
    this.stopProgressTracker();
  }

  // ═══════════════════════════════════════════
  //  Helpers
  // ═══════════════════════════════════════════

  formatDuration(ms: number): string {
    const totalSeconds = Math.floor(ms / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}m ${seconds}s`;
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  private formatMs(ms: number): string {
    const totalSeconds = Math.floor(ms / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  }
}
