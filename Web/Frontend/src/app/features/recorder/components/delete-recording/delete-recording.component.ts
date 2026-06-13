import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { recordingFeature } from '../../../../store/reducers/recording.reducer';
import * as RecordingActions from '../../../../store/actions/recording.actions';
import { RecordingSummary } from '../../../../shared/models/recording.model';

/**
 * DeleteRecordingComponent — "Delete" tab inside the Recorder Panel.
 *
 * Displays all the user's recordings in a list. Each item shows
 * the name, speed, duration, and creation date with a delete button.
 * Clicking delete dispatches the deleteRecording action.
 */
@Component({
  selector: 'app-delete-recording',
  standalone: true,
  imports: [CommonModule, ButtonModule],
  templateUrl: './delete-recording.component.html',
  styleUrl: './delete-recording.component.scss',
})
export class DeleteRecordingComponent implements OnInit, OnDestroy {
  private readonly store = inject(Store);
  private subscriptions = new Subscription();

  recordings: RecordingSummary[] = [];
  isLoading = false;

  /** ID of the recording pending delete confirmation (null = none) */
  confirmDeleteId: string | null = null;

  ngOnInit(): void {
    // Load recordings when tab opens
    this.store.dispatch(RecordingActions.loadRecordings());

    this.subscriptions.add(
      this.store.select(recordingFeature.selectRecordings).subscribe((recs) => {
        this.recordings = recs;
      })
    );

    this.subscriptions.add(
      this.store.select(recordingFeature.selectIsLoadingList).subscribe((loading) => {
        this.isLoading = loading;
      })
    );

    console.log('[DeleteRecording] Initialized — loading recordings');
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    console.log('[DeleteRecording] Destroyed');
  }

  // ── Actions ──

  /** First click — show confirm state */
  onDeleteClick(id: string): void {
    this.confirmDeleteId = id;
    console.log(`[DeleteRecording] Confirm delete for ${id}`);
  }

  /** Second click — actually delete */
  onConfirmDelete(id: string): void {
    console.log(`[DeleteRecording] Deleting recording ${id}`);
    this.store.dispatch(RecordingActions.deleteRecording({ id }));
    this.confirmDeleteId = null;
  }

  /** Cancel delete confirmation */
  onCancelDelete(): void {
    this.confirmDeleteId = null;
  }

  // ── Helpers ──

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
}
