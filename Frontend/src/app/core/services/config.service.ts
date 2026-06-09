import { Injectable, signal } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  // Signals to allow reactive updates in templates/components
  private gatewayUrlSignal = signal<string>(this.loadGatewayUrl());
  private cameraUrlSignal = signal<string>(this.loadCameraUrl());

  gatewayUrl = this.gatewayUrlSignal.asReadonly();
  cameraUrl = this.cameraUrlSignal.asReadonly();

  private loadGatewayUrl(): string {
    return localStorage.getItem('gateway_url') ?? environment.apiUrl;
  }

  private loadCameraUrl(): string {
    return localStorage.getItem('camera_url') ?? environment.cameraUrl;
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

  updateConfig(gatewayUrl: string, cameraUrl: string): void {
    const formattedGateway = this.formatUrl(gatewayUrl);
    const formattedCamera = this.formatUrl(cameraUrl, 'http://', true);

    localStorage.setItem('gateway_url', formattedGateway);
    localStorage.setItem('camera_url', formattedCamera);
    
    this.gatewayUrlSignal.set(formattedGateway);
    this.cameraUrlSignal.set(formattedCamera);
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
