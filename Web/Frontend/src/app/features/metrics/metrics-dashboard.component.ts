import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { Subject, Subscription, timer, forkJoin } from 'rxjs';
import { switchMap, takeUntil } from 'rxjs/operators';
import { MetricsService, MetricEntry, MetricsStats } from '../../core/services/metrics.service';
import { ReportService } from '../../core/services/report.service';
import { HistoricalChartComponent } from './components/historical-chart.component';

@Component({
  selector: 'app-metrics-dashboard',
  standalone: true,
  providers: [ConfirmationService],
  imports: [CommonModule, FormsModule, ButtonModule, ConfirmDialogModule, HistoricalChartComponent],
  templateUrl: './metrics-dashboard.component.html',
  styleUrl: './metrics-dashboard.component.scss'
})
export class MetricsDashboardComponent implements OnInit, OnDestroy {
  private readonly metricsService = inject(MetricsService);
  private readonly reportService = inject(ReportService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly destroy$ = new Subject<void>();
  private refreshSubscription?: Subscription;

  isClearing = false;
  isExportingPdf = false;

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

    // Fetch history and stats together; only clear the loading flag when both complete
    forkJoin([
      this.metricsService.getHistory(this.selectedRange),
      this.metricsService.getStats(this.selectedRange)
    ])
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ([history, stats]) => {
          this.history = history;
          this.formatChartData();
          this.stats = stats;
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load metrics', err);
          this.isLoading = false;
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

    // Refresh every 3 seconds — use forkJoin inside switchMap so both requests
    // are cancelled automatically when a new tick fires or the subscription stops.
    this.refreshSubscription = timer(3000, 3000)
      .pipe(
        switchMap(() => forkJoin([
          this.metricsService.getHistory(this.selectedRange),
          this.metricsService.getStats(this.selectedRange)
        ])),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: ([history, stats]) => {
          this.history = history;
          this.formatChartData();
          this.stats = stats;
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

  exportCsv(): void {
    this.reportService.exportCsv(this.history, this.getRangeLabel());
  }

  exportPdf(): void {
    this.isExportingPdf = true;
    try {
      this.reportService.exportPdf(this.history, this.stats, this.getRangeLabel());
    } finally {
      this.isExportingPdf = false;
    }
  }

  private getRangeLabel(): string {
    return this.timeRanges.find(r => r.value === this.selectedRange)?.label ?? `${this.selectedRange}min`;
  }

  clearDatabase(): void {
    this.confirmationService.confirm({
      message: 'This will permanently delete all telemetry records from the database. Are you sure?',
      header: 'Clear Metrics Database',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.isClearing = true;
        this.metricsService.clearAll()
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: () => {
              this.history = [];
              this.stats = null;
              this.voltageChartData = [];
              this.distanceChartData = [];
              this.isClearing = false;
            },
            error: () => { this.isClearing = false; }
          });
      }
    });
  }
}
