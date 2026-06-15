import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { Store } from '@ngrx/store';
import { map, take, filter } from 'rxjs/operators';
import { authFeature } from '../../store/reducers/auth.reducer';
import { AppRoutes } from '../constants/routes';
import { AlertService } from '../services/alert.service';

/**
 * Role Guard
 * Restricts route access to specific user roles.
 * Expects the route data to have an 'expectedRoles' array of strings.
 */
export const roleGuard: CanActivateFn = (route, state) => {
  const store = inject(Store);
  const router = inject(Router);
  const alertService = inject(AlertService);

  const expectedRoles: string[] = route.data['expectedRoles'] || [];

  return store.select(authFeature.selectAuthState).pipe(
    filter(authState => authState.isInitialized),
    take(1),
    map((authState) => {
      if (!authState.isAuthenticated) {
        router.navigate([AppRoutes.LOGIN]);
        return false;
      }

      // If no roles specified, allow any authenticated user
      if (expectedRoles.length === 0) {
        return true;
      }

      const userRoles = authState.user?.roles || [];
      const hasRole = expectedRoles.some(role => userRoles.includes(role));

      if (hasRole) {
        return true;
      }

      alertService.danger('You do not have permission to access this page.', 'Access Denied');
      router.navigate([AppRoutes.HOME]);
      return false;
    })
  );
};
