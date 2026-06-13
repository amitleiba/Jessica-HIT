import { Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";
import { Store } from "@ngrx/store";
import { combineLatest } from "rxjs";
import { map } from "rxjs/operators";
import { AppRoutes } from "../../core/constants/routes";
import { authFeature } from "../../store/reducers/auth.reducer";
import { carFeature } from "../../store/reducers/car.reducer";
import { selectIsAdmin, selectIsOperator } from "../../store/selectors/auth.selectors";
import { SignalManagerService } from "../../core/services/signal-manager.service";

@Component({
  selector: "app-home",
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: "./home.component.html",
  styleUrl: "./home.component.scss",
})
export class HomeComponent {
  private readonly store = inject(Store);
  private readonly signalManager = inject(SignalManagerService);

  readonly routes = AppRoutes;
  readonly currentYear = new Date().getFullYear();

  isAuthenticated$ = this.store.select(authFeature.selectIsAuthenticated);
  connectionState$ = this.signalManager.connectionState$;
  isAdmin$ = this.store.select(selectIsAdmin);
  isOperator$ = this.store.select(selectIsOperator);

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
}
