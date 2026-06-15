import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private readonly http: HttpClient;

  // Signals to allow reactive updates in templates/components
  private gatewayUrlSignal = signal<string>(this.loadGatewayUrl());
  private cameraUrlSignal = signal<string>(this.loadCameraUrl());
  private esp32GatewayUrlSignal = signal<string>(this.loadEsp32GatewayUrl());

  gatewayUrl = this.gatewayUrlSignal.asReadonly();
  cameraUrl = this.cameraUrlSignal.asReadonly();
  esp32GatewayUrl = this.esp32GatewayUrlSignal.asReadonly();

  constructor(http: HttpClient) {
    this.http = http;
  }

  private loadGatewayUrl(): string {
    return localStorage.getItem('gateway_url') ?? environment.apiUrl;
  }

  private loadCameraUrl(): string {
    return localStorage.getItem('camera_url') ?? environment.cameraUrl;
  }

  private loadEsp32GatewayUrl(): string {
    return localStorage.getItem('esp32_gateway_url') ?? 'ws://192.168.1.215:81';
  }

  getApiUrl(): string {
    return this.gatewayUrlSignal();
  }

  getSignalRUrl(): string {
    return `${this.getApiUrl()}/hubs/jessica`;
  }

  getCameraUrl(): string {
    return this.cameraUrlSignal();
  }

  getEsp32GatewayUrl(): string {
    return this.esp32GatewayUrlSignal();
  }

  /**
   * Updates the Server API URL and Camera URL locally (these are frontend-only configs).
   */
  updateLocalConfig(gatewayUrl: string, cameraUrl: string): void {
    const formattedGateway = this.formatUrl(gatewayUrl);
    const formattedCamera = this.formatUrl(cameraUrl, 'http://', true);

    localStorage.setItem('gateway_url', formattedGateway);
    localStorage.setItem('camera_url', formattedCamera);

    this.gatewayUrlSignal.set(formattedGateway);
    this.cameraUrlSignal.set(formattedCamera);
  }

  /**
   * Sends the new ESP32 Gateway IP to the backend.
   * Only call this as part of the backend-first save flow in SettingsComponent.
   * On 200 OK the caller is responsible for calling confirmEsp32GatewayIp().
   */
  updateGatewayUrlOnServer(url: string): Observable<{ success: boolean; url: string }> {
    const apiUrl = `${this.getApiUrl()}/api/jessica/connection/gateway-ip`;
    return this.http.put<{ success: boolean; url: string }>(apiUrl, { url });
  }

  /**
   * Persists the new Gateway URL locally after backend confirmation.
   * Must only be called after a successful 200 OK from updateGatewayUrlOnServer().
   */
  confirmEsp32GatewayUrl(url: string): void {
    localStorage.setItem('esp32_gateway_url', url);
    this.esp32GatewayUrlSignal.set(url);
  }

  // Legacy alias kept for backward compatibility with existing callers.
  updateConfig(gatewayUrl: string, cameraUrl: string): void {
    this.updateLocalConfig(gatewayUrl, cameraUrl);
  }

  /**
   * Helper to format entered URL. Prefixes http:// if missing.
   * Optionally appends trailing slash.
   */
  private formatUrl(url: string, defaultProtocol = 'http://', appendSlash = false): string {
    let trimmed = url.trim();
    if (!trimmed) return trimmed;

    if (!/^https?:\/\//i.test(trimmed)) {
      trimmed = defaultProtocol + trimmed;
    }

    if (appendSlash && !trimmed.endsWith('/')) {
      trimmed += '/';
    }

    return trimmed;
  }
}
