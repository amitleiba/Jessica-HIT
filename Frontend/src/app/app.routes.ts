import { Routes } from "@angular/router";
import { AppRoutes, RouteSegments } from "./core/constants/routes";
import { authGuard } from "./core/guards/auth.guard";

/**
 * Application Routes Configuration
 * Uses centralized route constants for maintainability
 * 
 * RouteSegments: Used for path property (route definition)
 * AppRoutes: Used for redirectTo (navigation)
 * 
 * Example: Change 'login' to 'signin' in RouteSegments → updates everywhere!
 */
export const routes: Routes = [
  {
    path: "",
    redirectTo: AppRoutes.HOME,
    pathMatch: "full",
  },
  {
    path: RouteSegments.HOME,
    loadComponent: () =>
      import("./features/home/home.component").then((m) => m.HomeComponent),
    canActivate: [authGuard],
  },
  {
    path: RouteSegments.LOGIN,
    loadComponent: () =>
      import("./features/auth/auth-screen.component").then((m) => m.AuthScreenComponent),
  },
  {
    path: RouteSegments.REGISTER,
    loadComponent: () =>
      import("./features/auth/auth-screen.component").then((m) => m.AuthScreenComponent),
  },
  {
    path: "**",
    redirectTo: AppRoutes.HOME,
  },
];
