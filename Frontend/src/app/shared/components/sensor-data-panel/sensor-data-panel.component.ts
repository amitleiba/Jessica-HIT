import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

/**
 * SensorDataPanelComponent
 *
 * Right-side panel with a speed dial (min → max slider)
 * and placeholder area for future sensor readouts.
 *
 * Outputs: speedChange — emitted when user adjusts the speed dial.
 */
@Component({
    selector: 'app-sensor-data-panel',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './sensor-data-panel.component.html',
    styleUrl: './sensor-data-panel.component.scss',
})
export class SensorDataPanelComponent {
    // ── Speed dial config ──
    @Input() speed: number = 0;
    @Input() minSpeed: number = 0;
    @Input() maxSpeed: number = 100;
    @Input() speedStep: number = 1;

    @Output() speedChange = new EventEmitter<number>();

    /** Percentage for the arc fill (0 → 100) */
    get speedPercent(): number {
        const range = this.maxSpeed - this.minSpeed;
        if (range <= 0) return 0;
        return ((this.speed - this.minSpeed) / range) * 100;
    }

    onSpeedInput(event: Event): void {
        const value = +(event.target as HTMLInputElement).value;
        console.log(`[SensorDataPanel] Speed dial → ${value}`);
        this.speedChange.emit(value);
    }
}
