import { Component, inject } from "@angular/core";
import { ThemeToggleComponent } from "../theme-toggle/theme-toggle.component";
import { CommonModule } from "@angular/common";
import { Router, RouterModule } from "@angular/router";
import { ButtonModule } from "primeng/button";
import { Store } from "@ngrx/store";
import { authFeature } from "../../../store/reducers/auth.reducer";
import { AppRoutes } from "../../../core/constants/routes";
import * as AuthActions from "../../../store/actions/auth.actions";

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

  // Observable state from store (using auto-generated feature selectors)
  isAuthenticated$ = this.store.select(authFeature.selectIsAuthenticated);
  user$ = this.store.select(authFeature.selectUser);

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
