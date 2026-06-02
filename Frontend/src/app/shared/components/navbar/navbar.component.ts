import { Component, inject } from "@angular/core";
import { ThemeToggleComponent } from "../theme-toggle/theme-toggle.component";
import { CommonModule } from "@angular/common";
import { Router, RouterModule } from "@angular/router";
import { ButtonModule } from "primeng/button";
import { Store } from "@ngrx/store";
import { combineLatest } from "rxjs";
import { map } from "rxjs/operators";
import { authFeature } from "../../../store/reducers/auth.reducer";
import { carFeature } from "../../../store/reducers/car.reducer";
import { AppRoutes } from "../../../core/constants/routes";
import * as AuthActions from "../../../store/actions/auth.actions";
import { SignalManagerService } from "../../../core/services/signal-manager.service";

@Component({
  selector: "app-navbar",
  standalone: true,
  imports: [CommonModule, RouterModule, ThemeToggleComponent, ButtonModule],
  templateUrl: "./navbar.component.html",
  styleUrl: "./navbar.component.scss",
})
export class NavbarComponent {
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly signalManager = inject(SignalManagerService);

  // Observable state from store (using auto-generated feature selectors)
  isAuthenticated$ = this.store.select(authFeature.selectIsAuthenticated);
  user$ = this.store.select(authFeature.selectUser);
  connectionState$ = this.signalManager.connectionState$;

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

  // Route constants for template
  readonly routes = AppRoutes;

  navigateToLogin(): void {
    this.router.navigate([AppRoutes.LOGIN]);
  }

  navigateToRegister(): void {
    this.router.navigate([AppRoutes.REGISTER]);
  }

  logout(): void {
    this.store.dispatch(AuthActions.logout());
  }
}
