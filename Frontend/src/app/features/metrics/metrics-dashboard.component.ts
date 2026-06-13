import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { Subject, Subscription, timer } from 'rxjs';
import { switchMap, takeUntil } from 'rxjs/operators';
import { MetricsService, MetricEntry, MetricsStats } from '../../core/services/metrics.service';
import { HistoricalChartComponent } from './components/historical-chart.component';

@Component({
  selector: 'app-metrics-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, HistoricalChartComponent],
  templateUrl: './metrics-dashboard.component.html',
  styleUrl: './metrics-dashboard.component.scss'
})
export class MetricsDashboardComponent implements OnInit, OnDestroy {
  private readonly metricsService = inject(MetricsService);
  private readonly destroy$ = new Subject<void>();
  private refreshSubscription?: Subscription;

  // State variables
  selectedRange: number = 60; // default to 1 hour (60 minutes)
  autoRefresh: boolean = true;
  isLoading: boolean = false;

  history: MetricEntry[] = [];
  stats: MetricsStats | null = null;

  // Chart data formatted lists
  voltageChartData: { value: number; timestamp: Date }[] = [];
  distanceChartData: { value: number; timestamp: Date }[] = [];

  readonly timeRanges = [
    { label: '15 Mins', value: 15 },
    { label: '1 Hour', value: 60 },
    { label: '6 Hours', value: 360 },
    { label: '24 Hours', value: 1440 }
  ];

  ngOnInit(): void {
    this.loadData();
    this.setupRefreshCycle();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.refreshSubscription?.unsubscribe();
  }

  loadData(): void {
    if (this.isLoading) return;
    this.isLoading = true;

    // Fetch history and stats together
    this.metricsService.getHistory(this.selectedRange)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (history) => {
          this.history = history;
          this.formatChartData();
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load metrics history', err);
          this.isLoading = false;
        }
      });

    this.metricsService.getStats(this.selectedRange)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (stats) => {
          this.stats = stats;
        },
        error: (err) => {
          console.error('Failed to load metrics stats', err);
        }
      });
  }

  onRangeChange(minutes: number): void {
    this.selectedRange = minutes;
    this.loadData();
    this.setupRefreshCycle(); // Restart timer with new selection
  }

  toggleAutoRefresh(): void {
    this.autoRefresh = !this.autoRefresh;
    this.setupRefreshCycle();
  }

  private setupRefreshCycle(): void {
    this.refreshSubscription?.unsubscribe();
    
    if (!this.autoRefresh) return;

    // Refresh every 3 seconds
    this.refreshSubscription = timer(3000, 3000)
      .pipe(
        switchMap(() => {
          // Fetch updates silently in background (don't set isLoading = true to prevent flickering)
          return this.metricsService.getHistory(this.selectedRange);
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (history) => {
          this.history = history;
          this.formatChartData();
          
          // Also update statistics
          this.metricsService.getStats(this.selectedRange)
            .pipe(takeUntil(this.destroy$))
            .subscribe(stats => this.stats = stats);
        },
        error: (err) => {
          console.warn('Auto-refresh failed, retrying in next cycle', err);
        }
      });
  }

  private formatChartData(): void {
    this.voltageChartData = this.history.map(h => ({
      value: h.solarVoltage,
      timestamp: new Date(h.timestamp)
    }));

    this.distanceChartData = this.history.map(h => ({
      value: h.distance,
      timestamp: new Date(h.timestamp)
    }));
  }

  // Value formatting helpers
  getModeName(modeStr: string): string {
    const mode = parseInt(modeStr, 10);
    switch (mode) {
      case 0: return 'Idle';
      case 1: return 'Manual';
      case 2: return 'Autonomous';
      case 3: return 'Charging';
      default: return 'Unknown';
    }
  }

  getModeKeys(): string[] {
    if (!this.stats?.modeDistribution) return [];
    return Object.keys(this.stats.modeDistribution).sort();
  }

  getModePercentage(modeKey: string): number {
    if (!this.stats || !this.stats.modeDistribution || this.stats.totalCount === 0) return 0;
    const count = this.stats.modeDistribution[modeKey] || 0;
    return Math.round((count / this.stats.totalCount) * 100);
  }

  getModeColor(modeStr: string): string {
    const mode = parseInt(modeStr, 10);
    switch (mode) {
      case 0: return '#64748b'; // Idle - slate
      case 1: return '#3b82f6'; // Manual - blue
      case 2: return '#10b981'; // Autonomous - emerald
      case 3: return '#f59e0b'; // Charging - amber
      default: return '#94a3b8';
    }
  }

  getSafetyLabel(safety: number): string {
    switch (safety) {
      case 0: return 'Safe';
      case 1: return 'Warning';
      case 2: return 'Hazard';
      default: return 'Unknown';
    }
  }

  getSafetyClass(safety: number): string {
    switch (safety) {
      case 0: return 'safety-safe';
      case 1: return 'safety-warning';
      case 2: return 'safety-hazard';
      default: return 'safety-unknown';
    }
  }
}
