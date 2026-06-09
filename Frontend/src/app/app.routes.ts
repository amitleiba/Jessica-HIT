import { Routes } from "@angular/router";
import { AppRoutes, RouteSegments } from "./core/constants/routes";
import { authGuard } from "./core/guards/auth.guard";
import { roleGuard } from "./core/guards/role.guard";

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
    path: RouteSegments.MANUAL_CONTROLLER,
    loadComponent: () =>
      import("./features/manual-controller/manual-controller.component").then((m) => m.ManualControllerComponent),
    canActivate: [authGuard, roleGuard],
    data: { expectedRoles: ["Operator", "Admin"] },
  },
  {
    path: RouteSegments.LIVE_FEED,
    loadComponent: () =>
      import("./features/live-feed/live-feed.component").then((m) => m.LiveFeedComponent),
    canActivate: [authGuard, roleGuard],
    data: { expectedRoles: ["Viewer", "Operator", "Admin"] },
  },
  {
    path: "recorder/replay/:recordingId",
    loadComponent: () =>
      import("./features/recorder/replay-session/replay-session.component").then(
        (m) => m.ReplaySessionComponent
      ),
    canActivate: [authGuard, roleGuard],
    data: { expectedRoles: ["Operator", "Admin"] },
  },
  {
    path: RouteSegments.RECORDER,
    loadComponent: () =>
      import("./features/recorder/recorder-panel.component").then((m) => m.RecorderPanelComponent),
    canActivate: [authGuard, roleGuard],
    data: { expectedRoles: ["Operator", "Admin"] },
  },
  {
    path: RouteSegments.USER_MANAGEMENT,
    loadComponent: () =>
      import("./features/user-management/user-management.component").then((m) => m.UserManagementComponent),
    canActivate: [authGuard, roleGuard],
    data: { expectedRoles: ["Admin"] },
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
