import { Component, Input, OnChanges, SimpleChanges, ElementRef, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';

interface ChartPoint {
  x: number;
  y: number;
  value: number;
  timestamp: Date;
  rawTimeStr: string;
}

@Component({
  selector: 'app-historical-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './historical-chart.component.html',
  styleUrl: './historical-chart.component.scss'
})
export class HistoricalChartComponent implements OnChanges {
  @Input() data: { value: number; timestamp: Date | string }[] = [];
  @Input() label: string = '';
  @Input() min: number = 0;
  @Input() max: number = 100;
  @Input() color: string = '#3b82f6'; // fallback color
  @Input() unit: string = '';
  @Input() chartHeight: number = 220;

  @ViewChild('chartSvg', { static: false }) chartSvg!: ElementRef<SVGElement>;

  points: ChartPoint[] = [];
  linePath: string = '';
  areaPath: string = '';
  gridLines: { y: number; value: number }[] = [];
  
  // Hover state variables
  hoveredPoint: ChartPoint | null = null;
  hoverX: number = 0;
  isHovered: boolean = false;
  tooltipLeft: number = 0;
  tooltipTop: number = 0;

  readonly width = 600;
  readonly height = 220;
  readonly padding = { top: 20, right: 20, bottom: 30, left: 50 };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['data'] || changes['min'] || changes['max']) {
      this.calculateChart();
    }
  }

  private calculateChart(): void {
    if (!this.data || this.data.length === 0) {
      this.points = [];
      this.linePath = '';
      this.areaPath = '';
      this.gridLines = [];
      return;
    }

    // Convert string timestamps to Date objects
    const parsedData = this.data.map(d => ({
      value: d.value,
      timestamp: d.timestamp instanceof Date ? d.timestamp : new Date(d.timestamp)
    })).sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime());

    // Always auto-scale to the actual data range so the chart fills the space
    const values = parsedData.map(d => d.value);
    const maxVal = Math.max(...values);
    const currentMin = 0;
    let currentMax = maxVal > 0 ? maxVal * 1.2 : (this.max || 10);
    // Round up to a clean number for readable grid labels
    const magnitude = Math.pow(10, Math.floor(Math.log10(currentMax)));
    currentMax = Math.ceil(currentMax / magnitude) * magnitude;

    const valueRange = currentMax - currentMin;
    const startTime = parsedData[0].timestamp.getTime();
    const endTime = parsedData[parsedData.length - 1].timestamp.getTime();
    const timeRange = Math.max(1, endTime - startTime);

    // SVG graph bounds
    const graphWidth = this.width - this.padding.left - this.padding.right;
    const graphHeight = this.height - this.padding.top - this.padding.bottom;

    // Calculate screen points
    this.points = parsedData.map(d => {
      const timePercent = (d.timestamp.getTime() - startTime) / timeRange;
      const x = this.padding.left + timePercent * graphWidth;

      const clampedValue = Math.max(currentMin, Math.min(currentMax, d.value));
      const valuePercent = (clampedValue - currentMin) / valueRange;
      const y = this.padding.top + (1 - valuePercent) * graphHeight;

      return {
        x,
        y,
        value: d.value,
        timestamp: d.timestamp,
        rawTimeStr: d.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })
      };
    });

    // Generate paths
    if (this.points.length >= 2) {
      this.linePath = this.points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
      
      const first = this.points[0];
      const last = this.points[this.points.length - 1];
      const bottomY = this.height - this.padding.bottom;
      this.areaPath = `${this.linePath} L ${last.x.toFixed(1)} ${bottomY} L ${first.x.toFixed(1)} ${bottomY} Z`;
    } else if (this.points.length === 1) {
      const p = this.points[0];
      this.linePath = `M ${p.x} ${p.y} L ${p.x + 10} ${p.y}`;
      this.areaPath = '';
    }

    // Generate horizontal grid lines
    this.gridLines = [];
    const step = valueRange / 4;
    for (let i = 0; i <= 4; i++) {
      const val = currentMin + i * step;
      const percent = i / 4;
      const y = this.padding.top + (1 - percent) * graphHeight;
      this.gridLines.push({ y, value: Math.round(val * 10) / 10 });
    }
  }

  onMouseMove(event: MouseEvent): void {
    if (this.points.length === 0 || !this.chartSvg) return;

    const svgElement = this.chartSvg.nativeElement;
    const rect = svgElement.getBoundingClientRect();
    
    // Get mouse position relative to the SVG viewBox
    const clientX = event.clientX - rect.left;
    const clientY = event.clientY - rect.top;
    
    const scaleX = this.width / rect.width;
    const scaleY = this.height / rect.height;
    
    const svgX = clientX * scaleX;
    const svgY = clientY * scaleY;

    // Constrain search to graph area
    if (svgX < this.padding.left || svgX > this.width - this.padding.right) {
      this.onMouseLeave();
      return;
    }

    // Find the closest point horizontally
    let closestPoint = this.points[0];
    let minDiff = Math.abs(this.points[0].x - svgX);

    for (let i = 1; i < this.points.length; i++) {
      const diff = Math.abs(this.points[i].x - svgX);
      if (diff < minDiff) {
        minDiff = diff;
        closestPoint = this.points[i];
      }
    }

    this.hoveredPoint = closestPoint;
    this.hoverX = closestPoint.x;
    this.isHovered = true;

    // Position tooltip relative to container (in client pixels)
    // Shift tooltip to avoid clipping off-screen
    const tooltipWidth = 140;
    const tooltipHeight = 75;
    
    let leftPos = event.clientX - rect.left + 15;
    if (leftPos + tooltipWidth > rect.width) {
      leftPos = event.clientX - rect.left - tooltipWidth - 15;
    }
    
    let topPos = event.clientY - rect.top - tooltipHeight / 2;
    if (topPos < 0) topPos = 10;
    if (topPos + tooltipHeight > rect.height) topPos = rect.height - tooltipHeight - 10;

    this.tooltipLeft = leftPos;
    this.tooltipTop = topPos;
  }

  onMouseLeave(): void {
    this.isHovered = false;
    this.hoveredPoint = null;
  }

  get formattedMinTime(): string {
    if (this.points.length === 0) return '';
    return this.points[0].timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  get formattedMaxTime(): string {
    if (this.points.length === 0) return '';
    return this.points[this.points.length - 1].timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
}
