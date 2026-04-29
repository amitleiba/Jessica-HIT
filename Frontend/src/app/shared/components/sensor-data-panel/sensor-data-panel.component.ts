import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CarSensorData } from '../../models/car-sensor-data.model';

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
    @Input() maxSpeed: number = 10;
    @Input() speedStep: number = 1;
    /** When true, speed slider is display-only (e.g. replay / telemetry view). */
    @Input() readOnly = false;
    @Input() sensorData: CarSensorData = {};
    @Input() currentDirection = 'idle';

    @Output() speedChange = new EventEmitter<number>();

    /** Percentage for the arc fill (0 → 100) */
    get speedPercent(): number {
        const range = this.maxSpeed - this.minSpeed;
        if (range <= 0) return 0;
        return ((this.speed - this.minSpeed) / range) * 100;
    }

    onSpeedInput(event: Event): void {
        if (this.readOnly) return;
        const value = +(event.target as HTMLInputElement).value;
        console.log(`[SensorDataPanel] Speed dial → ${value}`);
        this.speedChange.emit(value);
    }

    get safetyLabel(): string {
        if (this.sensorData.safety == null) return 'N/A';
        return this.sensorData.safety === 1 ? 'DANGER' : 'SAFE';
    }
}
