import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConfigService } from './config.service';

export interface MetricEntry {
  distance: number;
  safety: number;
  mode: number;
  solarVoltage: number;
  timestamp: string;
}

export interface MetricsStats {
  totalCount: number;
  averageSolarVoltage: number;
  maxDistance: number;
  minDistance: number;
  averageDistance: number;
  safetyIncidentCount: number;
  modeDistribution: { [key: string]: number };
}

@Injectable({ providedIn: 'root' })
export class MetricsService {
  private readonly http = inject(HttpClient);
  private readonly configService = inject(ConfigService);

  private get apiUrl(): string {
    return `${this.configService.getApiUrl()}/api/metrics`;
  }

  /**
   * Fetch historical telemetry entries for the specified duration.
   * @param durationMinutes Time range in minutes.
   */
  getHistory(durationMinutes: number): Observable<MetricEntry[]> {
    const params = new HttpParams().set('durationMinutes', durationMinutes.toString());
    return this.http.get<MetricEntry[]>(`${this.apiUrl}/history`, { params });
  }

  /**
   * Fetch statistical aggregates for the specified duration.
   * @param durationMinutes Time range in minutes.
   */
  getStats(durationMinutes: number): Observable<MetricsStats> {
    const params = new HttpParams().set('durationMinutes', durationMinutes.toString());
    return this.http.get<MetricsStats>(`${this.apiUrl}/stats`, { params });
  }
}
