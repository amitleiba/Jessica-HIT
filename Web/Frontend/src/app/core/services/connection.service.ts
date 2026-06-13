import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConfigService } from './config.service';

export interface RefreshResult {
  success: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ConnectionService {
  private readonly http = inject(HttpClient);
  private readonly configService = inject(ConfigService);

  /**
   * Asks the backend to force-reconnect to the ESP32 Gateway WebSocket and robot.
   */
  triggerRefresh(): Observable<RefreshResult> {
    const url = `${this.configService.getApiUrl()}/api/jessica/connection/refresh`;
    return this.http.post<RefreshResult>(url, {});
  }
}
