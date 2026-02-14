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
 * isLoopEnabled  — whether replay should loop at the end
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

    on(RecordingActions.loadRecordings, (state) => {
        console.log('[Recording Reducer] Loading recordings…');
        return { ...state, isLoadingList: true, error: null };
    }),

    on(RecordingActions.loadRecordingsSuccess, (state, { recordings }) => {
        console.log(`[Recording Reducer] Loaded ${recordings.length} recordings`);
        return { ...state, recordings, isLoadingList: false };
    }),

    on(RecordingActions.loadRecordingsFailure, (state, { error }) => {
        console.log('[Recording Reducer] Load failed:', error);
        return { ...state, isLoadingList: false, error };
    }),

    // ── Create recording ──

    on(RecordingActions.startRecording, (state) => {
        console.log('[Recording Reducer] Recording STARTED');
        return { ...state, isRecording: true, error: null };
    }),

    on(RecordingActions.stopRecording, (state) => {
        console.log('[Recording Reducer] Recording STOPPED — saving…');
        return { ...state, isRecording: false };
    }),

    on(RecordingActions.saveRecordingSuccess, (state, { recording }) => {
        console.log(`[Recording Reducer] Saved: "${recording.name}"`);
        return {
            ...state,
            recordings: [recording, ...state.recordings],
        };
    }),

    on(RecordingActions.saveRecordingFailure, (state, { error }) => {
        console.log('[Recording Reducer] Save failed:', error);
        return { ...state, error };
    }),

    // ── Delete recording ──

    on(RecordingActions.deleteRecordingSuccess, (state, { id }) => {
        console.log(`[Recording Reducer] Deleted recording ${id}`);
        return {
            ...state,
            recordings: state.recordings.filter((r) => r.id !== id),
        };
    }),

    on(RecordingActions.deleteRecordingFailure, (state, { error }) => {
        console.log('[Recording Reducer] Delete failed:', error);
        return { ...state, error };
    }),

    // ── Replay ──

    on(RecordingActions.startReplay, (state, { recordingId, loop }) => {
        console.log(`[Recording Reducer] Replay STARTED — ${recordingId}, loop=${loop}`);
        return {
            ...state,
            isReplaying: true,
            replayRecordingId: recordingId,
            isLoopEnabled: loop,
        };
    }),

    on(RecordingActions.stopReplay, (state) => {
        console.log('[Recording Reducer] Replay STOPPED');
        return {
            ...state,
            isReplaying: false,
            replayRecordingId: null,
        };
    }),

    on(RecordingActions.toggleLoop, (state) => {
        console.log(`[Recording Reducer] Loop toggled → ${!state.isLoopEnabled}`);
        return { ...state, isLoopEnabled: !state.isLoopEnabled };
    })
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

