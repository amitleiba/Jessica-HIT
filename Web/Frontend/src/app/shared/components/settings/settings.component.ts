import { Component, Input, Output, EventEmitter, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ConfigService } from '../../../core/services/config.service';
import { SignalManagerService } from '../../../core/services/signal-manager.service';
import { AlertService } from '../../../core/services/alert.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, DialogModule, ButtonModule, InputTextModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnChanges {
  private readonly configService = inject(ConfigService);
  private readonly signalManager = inject(SignalManagerService);
  private readonly alertService = inject(AlertService);

  @Input() visible = false;
  @Output() visibleChange = new EventEmitter<boolean>();

  gatewayUrl = '';
  cameraUrl = '';
  esp32GatewayUrl = '';
  isSaving = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible) {
      this.loadCurrentSettings();
    }
  }

  loadCurrentSettings(): void {
    this.gatewayUrl = this.configService.getApiUrl();
    this.cameraUrl = this.configService.getCameraUrl();
    this.esp32GatewayUrl = this.configService.getEsp32GatewayUrl();
  }

  onClose(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  onSave(): void {
    if (!this.gatewayUrl.trim()) {
      this.alertService.danger('Gateway URL is required.');
      return;
    }
    if (!this.cameraUrl.trim()) {
      this.alertService.danger('ESP Camera URL is required.');
      return;
    }
    if (!this.esp32GatewayUrl.trim()) {
      this.alertService.danger('ESP32 Gateway URL is required.');
      return;
    }

    this.isSaving = true;

    // ── Backend-first for the Gateway IP ──────────────────────────────────
    // Only persist locally and update the signal after the backend confirms.
    this.configService.updateGatewayUrlOnServer(this.esp32GatewayUrl.trim()).subscribe({
      next: () => {
        // Backend confirmed → now safe to update local state.
        this.configService.confirmEsp32GatewayUrl(this.esp32GatewayUrl.trim());

        // Frontend-only configs (no server round-trip needed).
        const wasConnected = this.signalManager.isConnected;
        this.configService.updateLocalConfig(this.gatewayUrl, this.cameraUrl);

        this.alertService.success('Connection settings updated successfully!');

        // Reconnect SignalR dynamically if the Gateway API URL changed.
        if (wasConnected) {
          console.log('[SettingsComponent] Reconnecting SignalR with new Gateway URL...');
          this.signalManager.disconnect();
          setTimeout(() => {
            this.signalManager.connect();
          }, 150);
        }

        this.isSaving = false;
        this.onClose();
      },
      error: (err) => {
        console.error('[SettingsComponent] Failed to update Gateway IP on server:', err);
        this.alertService.danger(
          'Failed to update the ESP32 Gateway IP on the server. Other settings were not saved.'
        );
        this.isSaving = false;
      }
    });
  }
}
