import { Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";
import { Store } from "@ngrx/store";
import { AppRoutes } from "../../core/constants/routes";
import { authFeature } from "../../store/reducers/auth.reducer";
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
}
