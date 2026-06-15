import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { ToggleButtonModule } from 'primeng/togglebutton';
import { FormsModule } from '@angular/forms';
import { RecordingService } from '../../../../core/services/recording.service';
import { recordingFeature } from '../../../../store/reducers/recording.reducer';
import * as RecordingActions from '../../../../store/actions/recording.actions';
import { RecordingSummary, Recording } from '../../../../shared/models/recording.model';
import { AppRoutes } from '../../../../core/constants/routes';

/**
 * Replay list/detail inside the Recorder panel. Play opens the full-screen replay session
 * (camera + telemetry, no D-pad).
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
  private readonly router = inject(Router);
  private readonly recordingService = inject(RecordingService);
  private subscriptions = new Subscription();

  recordings: RecordingSummary[] = [];
  isLoading = false;
  isLoopEnabled = false;

  selectedRecording: Recording | null = null;
  isLoadingDetail = false;

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
      this.store.select(recordingFeature.selectIsLoopEnabled).subscribe((l) => {
        this.isLoopEnabled = l;
      })
    );

    console.log('[ReplayRecording] Initialized');
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    console.log('[ReplayRecording] Destroyed');
  }

  onSelectRecording(rec: RecordingSummary): void {
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

  onPlay(): void {
    if (!this.selectedRecording) return;

    const url = AppRoutes.recorderReplay(
      this.selectedRecording.id,
      this.isLoopEnabled
    );
    console.log(`[ReplayRecording] Navigating to replay session — ${url}`);
    this.router.navigateByUrl(url);
  }

  onToggleLoop(): void {
    this.store.dispatch(RecordingActions.toggleLoop());
  }

  formatDuration(ms: number): string {
    const totalSeconds = Math.floor(ms / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}m ${seconds}s`;
  }

}
