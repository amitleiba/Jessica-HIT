import { Component, Input, Output, EventEmitter, HostListener, OnDestroy } from "@angular/core";
import { CircularButtonComponent } from "../circular-button/circular-button.component";
import { CommonModule } from "@angular/common";

export type Direction = "up" | "down" | "left" | "right";

@Component({
  selector: "app-control-panel",
  standalone: true,
  imports: [CircularButtonComponent, CommonModule],
  templateUrl: "./control-panel.component.html",
  styleUrl: "./control-panel.component.scss",
})
export class ControlPanelComponent implements OnDestroy {
  // ── Inputs ──

  /** Show/hide individual direction buttons */
  @Input() showUp: boolean = true;
  @Input() showDown: boolean = true;
  @Input() showLeft: boolean = true;
  @Input() showRight: boolean = true;

  /**
   * Time window (ms) to wait for additional key presses before evaluating.
   * Two keys pressed/released within this window are batched together.
   */
  @Input() comboWindow: number = 80;

  // ── Output ──

  /**
   * Emits the resolved direction string whenever it **changes**.
   *
   * Values:
   *   "idle"        — no arrows active
   *   "up"          — single direction
   *   "left-right"  — combo (sorted alphabetically, hyphen-joined)
   *
   * Deduplication: if the user holds "up" and presses "up" again,
   * this will NOT re-emit. Only actual transitions trigger an emit.
   */
  @Output() directionChange = new EventEmitter<string>();

  // ── Internal state ──
  activeDirections = new Set<Direction>();
  private evalTimer: ReturnType<typeof setTimeout> | null = null;
  private lastResolved: string = "idle";

  private readonly keyMap: Record<string, Direction> = {
    ArrowUp: "up",
    ArrowDown: "down",
    ArrowLeft: "left",
    ArrowRight: "right",
    w: "up",
    W: "up",
    s: "down",
    S: "down",
    a: "left",
    A: "left",
    d: "right",
    D: "right",
  };

  /**
   * Helper to determine if the user is typing in an input field
   */
  private isInputEvent(event: KeyboardEvent): boolean {
    const target = event.target as HTMLElement;
    if (!target) return false;
    const tagName = target.tagName.toLowerCase();
    return tagName === 'input' || tagName === 'textarea' || target.isContentEditable;
  }

  // ────────────────────────────────────────────────────────────
  //  Keyboard listeners (window-level so it works without focus)
  // ────────────────────────────────────────────────────────────

  @HostListener("window:keydown", ["$event"])
  handleKeyDown(event: KeyboardEvent): void {
    if (this.isInputEvent(event)) return;
    const direction = this.keyMap[event.key];
    if (!direction || event.repeat) return;

    event.preventDefault();
    this.activeDirections.add(direction);
    this.scheduleEvaluation();
  }

  @HostListener("window:keyup", ["$event"])
  handleKeyUp(event: KeyboardEvent): void {
    if (this.isInputEvent(event)) return;
    const direction = this.keyMap[event.key];
    if (!direction) return;

    event.preventDefault();
    this.activeDirections.delete(direction);
    this.scheduleEvaluation();
  }

  // ────────────────────────────────────────────────────────────
  //  Press and Release handlers (mouse and touch interactions)
  // ────────────────────────────────────────────────────────────

  onPress(direction: Direction, event?: Event): void {
    if (event) {
      event.preventDefault();
    }
    this.activeDirections.add(direction);
    this.scheduleEvaluation();
  }

  onRelease(direction: Direction, event?: Event): void {
    if (event) {
      event.preventDefault();
    }
    if (this.activeDirections.has(direction)) {
      this.activeDirections.delete(direction);
      this.scheduleEvaluation();
    }
  }



  // ────────────────────────────────────────────────────────────
  //  Unified evaluation with deduplication
  // ────────────────────────────────────────────────────────────

  /**
   * Schedule an evaluation after the combo window.
   * Every change to activeDirections restarts the timer so that
   * rapid additions/removals (combos, simultaneous release) are batched.
   */
  private scheduleEvaluation(): void {
    if (this.evalTimer) {
      clearTimeout(this.evalTimer);
    }
    this.evalTimer = setTimeout(() => this.evaluate(), this.comboWindow);
  }

  /**
   * Resolve the current set of active directions into a single string,
   * cancelling opposing axes, then compare with the last emitted value
   * and emit only on change.
   *
   * Opposing-axis cancellation:
   *   Left + Right  → cancel both  (can't go left and right simultaneously)
   *   Up   + Down   → cancel both  (can't go up and down simultaneously)
   *
   * Examples:
   *   { left, right, up } → left/right cancel → "up"
   *   { up, down }        → up/down cancel    → "idle"
   *   { right, up }       → no cancellation   → "right-up"
   *   { left, right, up, down } → both cancel → "idle"
   */
  private evaluate(): void {
    const effective = new Set<Direction>(this.activeDirections);

    // Cancel opposing horizontal axis
    if (effective.has("left") && effective.has("right")) {
      effective.delete("left");
      effective.delete("right");
    }

    // Cancel opposing vertical axis
    if (effective.has("up") && effective.has("down")) {
      effective.delete("up");
      effective.delete("down");
    }

    const directions = Array.from(effective).sort();
    const resolved = directions.length === 0 ? "idle" : directions.join("-");

    if (resolved === this.lastResolved) {
      return; // No change — deduplicated
    }

    console.log(`[ControlPanel] 🎮 Direction: "${this.lastResolved}" → "${resolved}"`);
    this.lastResolved = resolved;
    this.directionChange.emit(resolved);
  }

  // ────────────────────────────────────────────────────────────
  //  Helpers
  // ────────────────────────────────────────────────────────────

  /** Used by the template to highlight active buttons */
  isDirectionActive(direction: Direction): boolean {
    return this.activeDirections.has(direction);
  }

  ngOnDestroy(): void {
    if (this.evalTimer) {
      clearTimeout(this.evalTimer);
    }
  }
}
