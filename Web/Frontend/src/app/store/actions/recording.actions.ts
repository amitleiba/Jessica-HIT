import { createAction, props } from '@ngrx/store';
import {
    RecordingSummary,
    CreateRecordingRequest,
} from '../../shared/models/recording.model';

// ── Load recordings list ──

export const loadRecordings = createAction('[Recording] Load Recordings');

export const loadRecordingsSuccess = createAction(
    '[Recording] Load Recordings Success',
    props<{ recordings: RecordingSummary[] }>()
);

export const loadRecordingsFailure = createAction(
    '[Recording] Load Recordings Failure',
    props<{ error: string }>()
);

// ── Create recording ──

/** User clicked "Start Recording" — begins local capture. */
export const startRecording = createAction(
    '[Recording] Start Recording',
    props<{ name: string; speed: number }>()
);

/** User clicked "Stop Recording" — stops capture and triggers save. */
export const stopRecording = createAction('[Recording] Stop Recording');

/** Backend confirmed the recording was saved. */
export const saveRecordingSuccess = createAction(
    '[Recording] Save Recording Success',
    props<{ recording: RecordingSummary }>()
);

export const saveRecordingFailure = createAction(
    '[Recording] Save Recording Failure',
    props<{ error: string }>()
);

// ── Delete recording ──

export const deleteRecording = createAction(
    '[Recording] Delete Recording',
    props<{ id: string }>()
);

export const deleteRecordingSuccess = createAction(
    '[Recording] Delete Recording Success',
    props<{ id: string }>()
);

export const deleteRecordingFailure = createAction(
    '[Recording] Delete Recording Failure',
    props<{ error: string }>()
);

// ── Replay ──

export const startReplay = createAction(
    '[Recording] Start Replay',
    props<{ recordingId: string; loop: boolean }>()
);

export const stopReplay = createAction('[Recording] Stop Replay');

export const toggleLoop = createAction('[Recording] Toggle Loop');

