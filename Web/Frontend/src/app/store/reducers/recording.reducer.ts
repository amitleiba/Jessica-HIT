import { createFeature, createReducer, on } from '@ngrx/store';
import * as RecordingActions from '../actions/recording.actions';
import { RecordingSummary } from '../../shared/models/recording.model';

/**
 * Recording State
 *
 * recordings     — cached list of the user's recordings (summaries)
 * isLoadingList  — true while fetching the list from the backend
 * isRecording    — true while a recording session is in progress (local capture)
 * isReplaying    — true while replaying a recording
 * replayRecordingId — ID of the recording currently being replayed
 * isLoopEnabled  — whether replay repeats the full forward+reverse cycle forever
 * error          — last error message (null = no error)
 */
export interface RecordingState {
    recordings: RecordingSummary[];
    isLoadingList: boolean;
    isRecording: boolean;
    isReplaying: boolean;
    replayRecordingId: string | null;
    isLoopEnabled: boolean;
    error: string | null;
}

export const initialRecordingState: RecordingState = {
    recordings: [],
    isLoadingList: false,
    isRecording: false,
    isReplaying: false,
    replayRecordingId: null,
    isLoopEnabled: false,
    error: null,
};

export const recordingReducer = createReducer(
    initialRecordingState,

    // ── Load list ──

    on(RecordingActions.loadRecordings, (state) => ({
        ...state, isLoadingList: true, error: null
    })),

    on(RecordingActions.loadRecordingsSuccess, (state, { recordings }) => ({
        ...state, recordings, isLoadingList: false
    })),

    on(RecordingActions.loadRecordingsFailure, (state, { error }) => ({
        ...state, isLoadingList: false, error
    })),

    // ── Create recording ──

    on(RecordingActions.startRecording, (state) => ({
        ...state, isRecording: true, error: null
    })),

    on(RecordingActions.stopRecording, (state) => ({
        ...state, isRecording: false
    })),

    on(RecordingActions.saveRecordingSuccess, (state, { recording }) => ({
        ...state,
        recordings: [recording, ...state.recordings],
    })),

    on(RecordingActions.saveRecordingFailure, (state, { error }) => ({
        ...state, error
    })),

    // ── Delete recording ──

    on(RecordingActions.deleteRecordingSuccess, (state, { id }) => ({
        ...state,
        recordings: state.recordings.filter((r) => r.id !== id),
    })),

    on(RecordingActions.deleteRecordingFailure, (state, { error }) => ({
        ...state, error
    })),

    // ── Replay ──

    on(RecordingActions.startReplay, (state, { recordingId, loop }) => ({
        ...state,
        isReplaying: true,
        replayRecordingId: recordingId,
        isLoopEnabled: loop,
    })),

    on(RecordingActions.stopReplay, (state) => ({
        ...state,
        isReplaying: false,
        replayRecordingId: null,
    })),

    on(RecordingActions.toggleLoop, (state) => ({
        ...state, isLoopEnabled: !state.isLoopEnabled
    }))
);

/**
 * Recording Feature — auto-generates selectors:
 *   recordingFeature.selectRecordings
 *   recordingFeature.selectIsLoadingList
 *   recordingFeature.selectIsRecording
 *   recordingFeature.selectIsReplaying
 *   recordingFeature.selectReplayRecordingId
 *   recordingFeature.selectIsLoopEnabled
 *   recordingFeature.selectError
 *   recordingFeature.selectRecordingState  (entire slice)
 */
export const recordingFeature = createFeature({
    name: 'recording',
    reducer: recordingReducer,
});

