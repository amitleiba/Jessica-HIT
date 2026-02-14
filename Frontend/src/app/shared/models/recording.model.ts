/**
 * Recording Models — data structures for the recording/replay feature.
 *
 * A Recording captures a user's car-driving session:
 *   - A fixed speed (set once before recording starts)
 *   - A list of direction-change events with ms-precision timestamps
 *
 * Recordings are per-user (tied to the authenticated user's ID).
 */

// ── List / Summary ──

/** Lightweight summary shown in list views (no events payload). */
export interface RecordingSummary {
    /** Server-generated GUID */
    id: string;

    /** User-given display name */
    name: string;

    /** Fixed speed for the entire recording (0–100) */
    speed: number;

    /** Total recording duration in milliseconds */
    durationMs: number;

    /** ISO-8601 creation timestamp */
    createdAt: string;
}

// ── Events ──

/** A single captured direction-change during a recording session. */
export interface RecordingEvent {
    /** Milliseconds elapsed since the recording started */
    offsetMs: number;

    /** Direction string: "up", "down", "left", "right", "left-right", "idle", etc. */
    direction: string;
}

// ── Full Recording (with events) ──

/** Complete recording including the event timeline — used for replay. */
export interface Recording extends RecordingSummary {
    events: RecordingEvent[];
}

// ── Requests ──

/** Payload sent to POST /api/recordings to persist a new recording. */
export interface CreateRecordingRequest {
    name: string;
    speed: number;
    durationMs: number;
    events: RecordingEvent[];
}

