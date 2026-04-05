import { Injectable, inject, NgZone } from '@angular/core';
import { Store } from '@ngrx/store';
import { Recording } from '../../shared/models/recording.model';
import * as CarActions from '../../store/actions/car.actions';

export interface ReplaySchedulerHandlers {
  /** ~10/sec during a cycle */
  onProgressTick: (progressPct: number, elapsedLabel: string) => void;
  /** Forward / reverse event position for display */
  onEventIndex: (index: number) => void;
  /** After each full ping-pong when loop restarts (1-based). */
  onLooped?: (cycleCount: number) => void;
  /** Non-loop natural end (caller dispatches stop). */
  onNaturalComplete?: () => void;
  shouldLoop: () => boolean;
}

type ScheduledJob = { at: number; seq: number; run: () => void };

/**
 * Imperative ping-pong replay scheduling (setTimeout). Supports pause/resume by
 * rescheduling only jobs after the paused timestamp.
 */
@Injectable({ providedIn: 'root' })
export class RecordingReplaySchedulerService {
  private readonly store = inject(Store);
  private readonly zone = inject(NgZone);

  private replayTimeouts: ReturnType<typeof setTimeout>[] = [];
  private replayStartTime = 0;
  private progressInterval: ReturnType<typeof setInterval> | null = null;
  private readonly replayEndBufferMs = 100;

  private handlers: ReplaySchedulerHandlers | null = null;
  private activeRecording: Recording | null = null;
  private loopCycleCount = 0;

  /** True while timers are cleared and replay is frozen. */
  private paused = false;
  private savedElapsedForResume = 0;

  get isPaused(): boolean {
    return this.paused;
  }

  /** True if a replay was started and not fully torn down (may be paused). */
  get isActive(): boolean {
    return this.handlers !== null && this.activeRecording !== null;
  }

  /** Clears all timeouts/intervals; does not dispatch NgRx actions. */
  stopTimers(): void {
    this.clearTimeouts();
    this.stopProgressTracker();
    this.handlers = null;
    this.activeRecording = null;
    this.paused = false;
    this.savedElapsedForResume = 0;
  }

  resetLoopCount(): void {
    this.loopCycleCount = 0;
  }

  /**
   * Begins scheduling forward + inverted reverse leg. Caller must dispatch
   * startReplay, changeSpeed, and startCar before calling this.
   */
  start(recording: Recording, handlers: ReplaySchedulerHandlers): void {
    this.stopTimers();
    this.activeRecording = recording;
    this.handlers = handlers;
    this.paused = false;
    this.scheduleCycleFromOffset(recording, handlers, 0);
  }

  /** Freeze timers; car keeps last command until resume or teardown. */
  pause(): void {
    if (!this.activeRecording || !this.handlers || this.paused) {
      return;
    }
    const totalCycleMs = this.getTotalCycleMs(this.activeRecording);
    let elapsed = performance.now() - this.replayStartTime;
    elapsed = Math.min(elapsed, totalCycleMs - 0.001);
    this.savedElapsedForResume = Math.max(0, elapsed);
    this.clearTimeouts();
    this.stopProgressTracker();
    this.paused = true;
    console.log(`[ReplayScheduler] Paused at ${this.savedElapsedForResume.toFixed(0)}ms`);
  }

  /** Continue from saved position in the current cycle. */
  resume(): void {
    if (!this.activeRecording || !this.handlers || !this.paused) {
      return;
    }
    this.paused = false;
    console.log(
      `[ReplayScheduler] Resuming from ${this.savedElapsedForResume.toFixed(0)}ms`
    );
    this.scheduleCycleFromOffset(
      this.activeRecording,
      this.handlers,
      this.savedElapsedForResume
    );
  }

  private getTotalCycleMs(recording: Recording): number {
    const dur = recording.durationMs;
    const buf = this.replayEndBufferMs;
    return 2 * (dur + buf);
  }

  private scheduleCycleFromOffset(
    recording: Recording,
    handlers: ReplaySchedulerHandlers,
    fromElapsed: number
  ): void {
    this.clearTimeouts();
    this.stopProgressTracker();

    this.replayStartTime = performance.now() - fromElapsed;

    const dur = recording.durationMs;
    const buf = this.replayEndBufferMs;
    const forwardEnd = dur + buf;
    const totalCycleMs = this.getTotalCycleMs(recording);

    console.log(
      `[ReplayScheduler] Cycle from offset ${fromElapsed.toFixed(0)}ms — ` +
        `dur=${dur}ms, totalCycle=${totalCycleMs}ms, loop=${handlers.shouldLoop()}`
    );

    if (fromElapsed <= 0) {
      this.zone.run(() => {
        handlers.onProgressTick(0, '00:00');
        handlers.onEventIndex(0);
      });
    }

    this.startProgressTracker(totalCycleMs, handlers);

    const jobs = this.buildCycleJobs(
      recording,
      handlers,
      forwardEnd,
      dur,
      totalCycleMs
    );

    const scheduleAt = (delayMs: number, fn: () => void) => {
      const id = setTimeout(() => this.zone.run(fn), delayMs);
      this.replayTimeouts.push(id);
    };

    for (const job of jobs) {
      if (job.at > fromElapsed) {
        scheduleAt(job.at - fromElapsed, job.run);
      }
    }
  }

  private buildCycleJobs(
    recording: Recording,
    handlers: ReplaySchedulerHandlers,
    forwardEnd: number,
    dur: number,
    totalCycleMs: number
  ): ScheduledJob[] {
    const events = recording.events;
    const jobs: ScheduledJob[] = [];
    let seq = 0;

    events.forEach((event, index) => {
      jobs.push({
        at: event.offsetMs,
        seq: seq++,
        run: () => {
          handlers.onEventIndex(index + 1);
          console.log(
            `[ReplayScheduler] ▶ Fwd ${index + 1}/${events.length}: "${event.direction}" @ ${event.offsetMs}ms`
          );
          this.store.dispatch(
            CarActions.changeDirection({ direction: event.direction })
          );
        },
      });
    });

    if (events.length > 0) {
      const last = events[events.length - 1].direction;
      const invertedLast = this.invertDirection(last);
      jobs.push({
        at: forwardEnd,
        seq: seq++,
        run: () => {
          console.log(
            `[ReplayScheduler] ◀ Rev start invert("${last}") = "${invertedLast}" @ ${forwardEnd}ms`
          );
          this.store.dispatch(
            CarActions.changeDirection({ direction: invertedLast })
          );
        },
      });
    }

    for (let i = events.length - 1; i >= 0; i--) {
      const at = forwardEnd + (dur - events[i].offsetMs);
      const segmentDir = i > 0 ? events[i - 1].direction : 'idle';
      const direction = this.invertDirection(segmentDir);
      jobs.push({
        at,
        seq: seq++,
        run: () => {
          console.log(
            `[ReplayScheduler] ◀ Rev i=${i} invert("${segmentDir}") → "${direction}" @ ${at}ms`
          );
          this.store.dispatch(CarActions.changeDirection({ direction }));
          handlers.onEventIndex(i);
        },
      });
    }

    jobs.push({
      at: totalCycleMs,
      seq: seq++,
      run: () => {
        if (handlers.shouldLoop()) {
          this.loopCycleCount++;
          handlers.onLooped?.(this.loopCycleCount);
          console.log(`[ReplayScheduler] 🔄 Cycle ${this.loopCycleCount} — restarting`);
          this.scheduleCycleFromOffset(recording, handlers, 0);
        } else {
          console.log('[ReplayScheduler] ✅ Ping-pong complete');
          handlers.onNaturalComplete?.();
        }
      },
    });

    jobs.sort((a, b) => a.at - b.at || a.seq - b.seq);
    return jobs;
  }

  private startProgressTracker(
    durationMs: number,
    handlers: ReplaySchedulerHandlers
  ): void {
    this.stopProgressTracker();
    this.progressInterval = setInterval(() => {
      const elapsed = performance.now() - this.replayStartTime;
      const progress = Math.min(100, (elapsed / durationMs) * 100);
      this.zone.run(() => {
        handlers.onProgressTick(Math.round(progress), this.formatMs(elapsed));
      });
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

  private formatMs(ms: number): string {
    const totalSeconds = Math.floor(ms / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  }

  private invertDirection(direction: string): string {
    if (!direction || direction === 'idle') {
      return 'idle';
    }
    const flip: Record<string, string> = {
      up: 'down',
      down: 'up',
      left: 'right',
      right: 'left',
    };
    const parts = direction.split('-').map((p) => flip[p] ?? p);
    parts.sort();
    return parts.join('-');
  }
}
