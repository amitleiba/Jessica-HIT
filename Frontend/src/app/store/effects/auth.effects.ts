import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { map, catchError, exhaustMap, tap } from 'rxjs/operators';
import { AuthService } from '../../core/services/auth.service';
import { AppRoutes, RouteBuilders } from '../../core/constants/routes';
import * as AuthActions from '../actions/auth.actions';

/**
 * Auth Effects
 * Handles side effects for authentication actions (HTTP requests, navigation, storage)
 * Pattern: Listen for action → Call service → Dispatch success/failure action
 */
@Injectable()
export class AuthEffects {
  private readonly actions$ = inject(Actions);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  /**
   * Login Effect
   * Listens for: [Auth] Login
   * Calls: AuthService.login()
   * Dispatches: [Auth] Login Success | [Auth] Login Failure
   */
  login$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.login),
      tap((action) => console.log('Auth Effect: Login initiated for', action.username)),
      exhaustMap((action) =>
        this.authService.login(action.username, action.password).pipe(
          map(({ user, token }) => {
            console.log('Auth Effect: Login successful');
            return AuthActions.loginSuccess({ user, token });
          }),
          catchError((error: string) => {
            console.error('Auth Effect: Login failed -', error);
            return of(AuthActions.loginFailure({ error }));
          })
        )
      )
    )
  );

  /**
   * Login Success Effect (Side effect: Navigate to home)
   * Listens for: [Auth] Login Success
   * Side effect: Navigate to home page
   */
  loginSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.loginSuccess),
        tap(() => {
          console.log('Auth Effect: Navigating to home');
          this.router.navigate([AppRoutes.HOME]);
        })
      ),
    { dispatch: false }
  );

  /**
   * Logout Effect
   * Listens for: [Auth] Logout
   * Calls: AuthService.logout()
   * Side effect: Navigate to login page
   */
  logout$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.logout),
        tap(() => console.log('Auth Effect: Logout initiated')),
        exhaustMap(() =>
          this.authService.logout().pipe(
            tap(() => {
              console.log('Auth Effect: Logout successful, navigating to login');
              this.router.navigate([AppRoutes.LOGIN]);
            }),
            catchError(() => {
              console.warn('Auth Effect: Logout error, navigating to login anyway');
              this.router.navigate([AppRoutes.LOGIN]);
              return of(void 0);
            })
          )
        )
      ),
    { dispatch: false }
  );

  /**
   * Load User Effect (for restoring session with explicit token)
   * Listens for: [Auth] Load User
   * Calls: AuthService.validateToken()
   * Dispatches: [Auth] Login Success | [Auth] Login Failure
   */
  loadUser$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.loadUser),
      tap(() => console.log('Auth Effect: Loading user from token')),
      exhaustMap((action) =>
        this.authService.validateToken().pipe(
          map((result) => {
            if (result) {
              console.log('Auth Effect: User restored from token');
              return AuthActions.loginSuccess({ user: result.user, token: result.token });
            } else {
              console.log('Auth Effect: No valid token found');
              return AuthActions.loginFailure({ error: 'No valid session found' });
            }
          }),
          catchError((error: string) => {
            console.error('Auth Effect: Token validation failed -', error);
            return of(AuthActions.loginFailure({ error }));
          })
        )
      )
    )
  );

  /**
   * Init Auth Effect (for restoring session on app initialization)
   * Listens for: [Auth] Init Auth
   * Calls: AuthService.validateToken() (reads from localStorage)
   * Dispatches: [Auth] Login Success | [Auth] Set Error (silent failure if no token)
   */
  initAuth$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.initAuth),
      tap(() => console.log('Auth Effect: Initializing auth from localStorage')),
      exhaustMap(() => {
        const token = localStorage.getItem('access_token');
        if (!token) {
          console.log('Auth Effect: No token found in localStorage');
          return of(AuthActions.setAuthState({ 
            user: null, 
            token: null, 
            isAuthenticated: false, 
            error: null 
          }));
        }

        return this.authService.validateToken().pipe(
          map((result) => {
            if (result) {
              console.log('Auth Effect: Auth restored from token');
              return AuthActions.loginSuccess({ user: result.user, token: result.token });
            } else {
              console.log('Auth Effect: Token validation failed');
              return AuthActions.setAuthState({ 
                user: null, 
                token: null, 
                isAuthenticated: false, 
                error: null 
              });
            }
          }),
          catchError(() => {
            console.error('Auth Effect: Auth initialization failed');
            return of(AuthActions.setAuthState({ 
              user: null, 
              token: null, 
              isAuthenticated: false, 
              error: null 
            }));
          })
        );
      })
    )
  );

  /**
   * Register Effect
   * Listens for: [Auth] Register
   * Calls: AuthService.register()
   * Dispatches: [Auth] Register Success | [Auth] Register Failure
   */
  register$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.register),
      tap((action) => console.log('Auth Effect: Registration initiated for', action.username)),
      exhaustMap((action) =>
        this.authService.register({
          username: action.username,
          email: action.email,
          password: action.password,
          firstName: action.firstName,
          lastName: action.lastName
        }).pipe(
          map((response) => {
            console.log('Auth Effect: Registration successful');
            return AuthActions.registerSuccess({ message: response.message });
          }),
          catchError((error: string) => {
            console.error('Auth Effect: Registration failed -', error);
            return of(AuthActions.registerFailure({ error }));
          })
        )
      )
    )
  );

  /**
   * Register Success Effect (Side effect: Navigate to login)
   * Listens for: [Auth] Register Success
   * Side effect: Navigate to login page with success message
   */
  registerSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.registerSuccess),
        tap(({ message }) => {
          console.log('Auth Effect: Registration success, navigating to login');
          const route = RouteBuilders.loginWithRegistrationSuccess(message);
          this.router.navigate([route.path], { queryParams: route.queryParams });
        })
      ),
    { dispatch: false }
  );
}
