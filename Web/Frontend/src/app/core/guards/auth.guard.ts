import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { Store } from '@ngrx/store';
import { map, take, filter } from 'rxjs/operators';
import { authFeature } from '../../store/reducers/auth.reducer';
import { AppRoutes } from '../constants/routes';

/**
 * Auth Guard
 * Protects routes that require authentication
 * Redirects to login if user is not authenticated
 */
export const authGuard: CanActivateFn = (route, state) => {
  const store = inject(Store);
  const router = inject(Router);

  return store.select(authFeature.selectAuthState).pipe(
    filter(state => state.isInitialized),
    take(1),
    map((state) => {
      if (state.isAuthenticated) {
        return true;
      } else {
        router.navigate([AppRoutes.LOGIN]);
        return false;
      }
    })
  );
};
