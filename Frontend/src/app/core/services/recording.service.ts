import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ConfigService } from './config.service';
import {
    RecordingSummary,
    Recording,
    RecordingEvent,
    CreateRecordingRequest,
} from '../../shared/models/recording.model';

/**
 * RecordingService — handles both HTTP calls to the RecordingManager backend
 * AND local event capture during a recording session.
 *
 * Local capture uses performance.now() for millisecond-precision timestamps.
 * Events are accumulated in memory and flushed to the backend on stopCapture().
 *
 * HTTP routes (through Gateway YARP proxy):
 *   GET    /api/recordings          → list user's recordings
 *   GET    /api/recordings/{id}     → get recording with events
 *   POST   /api/recordings          → save a new recording
 *   DELETE /api/recordings/{id}     → delete a recording
 */
@Injectable({ providedIn: 'root' })
export class RecordingService {
    private readonly http = inject(HttpClient);
    private readonly configService = inject(ConfigService);

    private get apiUrl(): string {
        return `${this.configService.getApiUrl()}/api/recordings`;
    }

    // ═══════════════════════════════════════════
    //  Local capture state
    // ═══════════════════════════════════════════

    private _isRecording = false;
    private recordingName = '';
    private recordingSpeed = 0;
    private startTime = 0;
    private capturedEvents: RecordingEvent[] = [];

    /** Whether a recording session is currently active. */
    get isRecording(): boolean {
        return this._isRecording;
    }

    /** Number of events captured so far in the current session. */
    get eventCount(): number {
        return this.capturedEvents.length;
    }

    /** Elapsed time (ms) since recording started. Returns 0 if not recording. */
    get elapsedMs(): number {
        if (!this._isRecording) return 0;
        return Math.round(performance.now() - this.startTime);
    }

    // ═══════════════════════════════════════════
    //  Capture: Start / Record / Stop
    // ═══════════════════════════════════════════

    /**
     * Begin a new recording session.
     * Resets all local state, stores name + speed, and marks the start time.
     */
    startCapture(name: string, speed: number): void {
        this.recordingName = name;
        this.recordingSpeed = speed;
        this.capturedEvents = [];
        this.startTime = performance.now();
        this._isRecording = true;

        console.log(
            `[RecordingService] 🔴 Recording started — "${name}", speed=${speed}`
        );
    }

    /**
     * Capture a direction change during the active recording.
     * Called by the create-recording component whenever the user's
     * direction changes (same event that also dispatches to the Car store).
     */
    captureDirection(direction: string): void {
        if (!this._isRecording) return;

        const offsetMs = Math.round(performance.now() - this.startTime);
        this.capturedEvents.push({ offsetMs, direction });

        console.log(
            `[RecordingService] 📌 Event #${this.capturedEvents.length}: "${direction}" @ ${offsetMs}ms`
        );
    }

    /**
     * Stop the recording and return the payload ready for the backend.
     * Does NOT automatically POST — the caller (NgRx effect) does that.
     */
    stopCapture(): CreateRecordingRequest {
        const durationMs = Math.round(performance.now() - this.startTime);
        this._isRecording = false;

        const request: CreateRecordingRequest = {
            name: this.recordingName,
            speed: this.recordingSpeed,
            durationMs,
            events: [...this.capturedEvents],
        };

        console.log(
            `[RecordingService] ⏹ Recording stopped — "${request.name}", ` +
            `duration=${durationMs}ms, events=${request.events.length}`
        );

        // Clear local state
        this.capturedEvents = [];
        this.recordingName = '';
        this.recordingSpeed = 0;
        this.startTime = 0;

        return request;
    }

    // ═══════════════════════════════════════════
    //  HTTP: CRUD
    // ═══════════════════════════════════════════

    /** GET /api/recordings — list all recordings for the current user. */
    getRecordings(): Observable<RecordingSummary[]> {
        console.log('[RecordingService] HTTP GET all recordings');
        return this.http.get<RecordingSummary[]>(this.apiUrl);
    }

    /** GET /api/recordings/{id} — get a single recording with events (for replay). */
    getRecording(id: string): Observable<Recording> {
        console.log(`[RecordingService] HTTP GET recording ${id}`);
        return this.http.get<Recording>(`${this.apiUrl}/${id}`);
    }

    /** POST /api/recordings — save a new recording. */
    createRecording(request: CreateRecordingRequest): Observable<RecordingSummary> {
        console.log(
            `[RecordingService] HTTP POST recording "${request.name}" (${request.events.length} events)`
        );
        return this.http.post<RecordingSummary>(this.apiUrl, request);
    }

    /** DELETE /api/recordings/{id} — delete a recording. */
    deleteRecording(id: string): Observable<void> {
        console.log(`[RecordingService] HTTP DELETE recording ${id}`);
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}

