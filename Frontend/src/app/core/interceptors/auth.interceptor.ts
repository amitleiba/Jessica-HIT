import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Store } from '@ngrx/store';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import * as AuthActions from '../../store/actions/auth.actions';

let isRefreshing = false;

/**
 * Authentication Interceptor
 * 
 * 1. Attaches the Bearer token to all outgoing requests.
 * 2. Catches 401 Unauthorized responses.
 * 3. Attempts to silently refresh the token.
 * 4. Retries the failed request on success, or logs out on failure.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const store = inject(Store);
  
  const token = authService.getStoredToken();
  
  let authReq = req;
  if (token) {
    authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // If we get a 401 and we aren't already refreshing, and we have a refresh token
      if (error.status === 401 && !isRefreshing && authService.getStoredRefreshToken() && !req.url.includes('/api/Auth/login') && !req.url.includes('/api/Auth/refresh')) {
        isRefreshing = true;

        return authService.refreshToken().pipe(
          switchMap((response) => {
            isRefreshing = false;
            
            // Retry the original request with the new token
            const retryReq = req.clone({
              headers: req.headers.set('Authorization', `Bearer ${response.token}`)
            });
            
            return next(retryReq);
          }),
          catchError((refreshError) => {
            isRefreshing = false;
            // If refresh fails, log the user out
            console.error('[AuthInterceptor] Token refresh failed, logging out', refreshError);
            store.dispatch(AuthActions.logout());
            return throwError(() => refreshError);
          })
        );
      }
      
      return throwError(() => error);
    })
  );
};
