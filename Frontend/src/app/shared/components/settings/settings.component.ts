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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible) {
      this.loadCurrentSettings();
    }
  }

  loadCurrentSettings(): void {
    this.gatewayUrl = this.configService.getApiUrl();
    this.cameraUrl = this.configService.getCameraUrl();
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

    const wasConnected = this.signalManager.isConnected;

    // Save configuration (updates localStorage and signals)
    this.configService.updateConfig(this.gatewayUrl, this.cameraUrl);

    this.alertService.success('Connection settings updated successfully!');

    // Reconnect SignalR dynamically if it was previously connected
    if (wasConnected) {
      console.log('[SettingsComponent] Reconnecting SignalR with new Gateway URL...');
      this.signalManager.disconnect();
      setTimeout(() => {
        this.signalManager.connect();
      }, 150);
    }

    this.onClose();
  }
}
