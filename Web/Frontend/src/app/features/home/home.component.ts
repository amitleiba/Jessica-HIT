import { Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";
import { Store } from "@ngrx/store";
import { combineLatest } from "rxjs";
import { map } from "rxjs/operators";
import { ButtonModule } from "primeng/button";
import { AppRoutes } from "../../core/constants/routes";
import { authFeature } from "../../store/reducers/auth.reducer";
import { carFeature } from "../../store/reducers/car.reducer";
import { selectIsAdmin, selectIsOperator } from "../../store/selectors/auth.selectors";
import { SignalManagerService } from "../../core/services/signal-manager.service";
import { ConnectionService } from "../../core/services/connection.service";
import { AlertService } from "../../core/services/alert.service";

@Component({
  selector: "app-home",
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonModule],
  templateUrl: "./home.component.html",
  styleUrl: "./home.component.scss",
})
export class HomeComponent {
  private readonly store = inject(Store);
  private readonly signalManager = inject(SignalManagerService);
  private readonly connectionService = inject(ConnectionService);
  private readonly alertService = inject(AlertService);

  readonly routes = AppRoutes;
  readonly currentYear = new Date().getFullYear();

  isAuthenticated$ = this.store.select(authFeature.selectIsAuthenticated);
  connectionState$ = this.signalManager.connectionState$;
  isAdmin$ = this.store.select(selectIsAdmin);
  isOperator$ = this.store.select(selectIsOperator);

  isRefreshing = false;

  robotConnectionState$ = combineLatest([
    this.signalManager.connectionState$,
    this.store.select(carFeature.selectSensorData)
  ]).pipe(
    map(([connState, sensorData]) => {
      if (connState !== 'connected') {
        return connState;
      }
      return sensorData?.available ? 'connected' : 'disconnected';
    })
  );

  onRefresh(): void {
    if (this.isRefreshing) return;
    this.isRefreshing = true;

    this.connectionService.triggerRefresh().subscribe({
      next: () => {
        this.alertService.success('Reconnection triggered. The system is attempting to reconnect to the ESP32 Gateway and robot.');
        this.isRefreshing = false;
      },
      error: (err) => {
        console.error('[HomeComponent] Refresh failed:', err);
        this.alertService.danger('Failed to trigger reconnection. Make sure the server is reachable.');
        this.isRefreshing = false;
      }
    });
  }
}
