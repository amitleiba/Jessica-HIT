import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-telemetry-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './telemetry-chart.component.html',
  styleUrl: './telemetry-chart.component.scss'
})
export class TelemetryChartComponent implements OnChanges {
  @Input() value: number | null = null;
  @Input() label: string = '';
  @Input() min: number = 0;
  @Input() max: number = 100;
  @Input() color: string = 'var(--primary-color)';
  @Input() unit: string = '';
  @Input() capacity: number = 30;

  history: number[] = [];
  linePath: string = '';
  areaPath: string = '';
  
  // Unique ID for the area fill gradient to prevent collisions between instances
  readonly gradientId = 'chart-gradient-' + Math.random().toString(36).substring(2, 9);
  
  // SVG viewbox dimension constants
  readonly width = 300;
  readonly height = 90;
  
  // Percentages for rendering grid lines
  readonly gridLevels = [0.25, 0.5, 0.75];

  private get effectiveCapacity(): number {
    return Math.max(2, Math.floor(this.capacity || 0));
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['value']) {
      this.addValueToHistory(this.value);
      return;
    }

    if (changes['capacity']) {
      const cap = this.effectiveCapacity;
      if (this.history.length > cap) {
        this.history = this.history.slice(-cap);
      }
    }

    if (changes['min'] || changes['max'] || changes['capacity']) {
      this.calculatePaths();
    }
  }

  private addValueToHistory(val: number | null): void {
    if (val === null || val === undefined) {
      return;
    }
    
    this.history.push(val);
    
    if (this.history.length > this.effectiveCapacity) {
      this.history.shift();
    }
    
    this.calculatePaths();
  }

  private calculatePaths(): void {
    const len = this.history.length;
    if (len < 2) {
      this.linePath = '';
      this.areaPath = '';
      return;
    }

    const points = this.history.map((v, i) => {
      // Draw from left to right, filling up the space
      const x = (i / (this.effectiveCapacity - 1)) * this.width;
      
      const clamped = Math.max(this.min, Math.min(this.max, v));
      const range = this.max - this.min;
      const y = range <= 0 
        ? this.height / 2 
        : this.height - ((clamped - this.min) / range) * this.height;
        
      return { x, y };
    });

    this.linePath = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');

    const firstPoint = points[0];
    const lastPoint = points[points.length - 1];
    this.areaPath = `${this.linePath} L ${lastPoint.x.toFixed(1)} ${this.height} L ${firstPoint.x.toFixed(1)} ${this.height} Z`;
  }

  get lastX(): number {
    if (this.history.length === 0) return 0;
    return ((this.history.length - 1) / (this.effectiveCapacity - 1)) * this.width;
  }

  get lastY(): number {
    if (this.history.length === 0) return this.height;
    const lastVal = this.history[this.history.length - 1];
    const clamped = Math.max(this.min, Math.min(this.max, lastVal));
    const range = this.max - this.min;
    return range <= 0 
      ? this.height / 2 
      : this.height - ((clamped - this.min) / range) * this.height;
  }
}
