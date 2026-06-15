import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType, ROOT_EFFECTS_INIT } from '@ngrx/effects';
import { of } from 'rxjs';
import { map, catchError, exhaustMap, tap } from 'rxjs/operators';
import { AuthService } from '../../core/services/auth.service';
import { AppRoutes, RouteBuilders } from '../../core/constants/routes';
import { SignalManagerService } from '../../core/services/signal-manager.service';
import * as AuthActions from '../actions/auth.actions';

function authEffectErrorMessage(error: unknown): string {
  if (typeof error === 'string') {
    return error;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'An unexpected error occurred. Please try again.';
}

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
  private readonly signalManager = inject(SignalManagerService);

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
          catchError((error: unknown) => {
            const message = authEffectErrorMessage(error);
            console.error('Auth Effect: Login failed -', error);
            return of(AuthActions.loginFailure({ error: message }));
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
          catchError((error: unknown) => {
            const message = authEffectErrorMessage(error);
            console.error('Auth Effect: Token validation failed -', error);
            return of(AuthActions.loginFailure({ error: message }));
          })
        )
      )
    )
  );

  /**
   * Init Auth Effect (for restoring session on app initialization)
   * Listens for: ROOT_EFFECTS_INIT (automatically dispatched when effects start)
   * Calls: AuthService.validateToken() (reads from localStorage)
   * Dispatches: [Auth] Login Success | [Auth] Set Error (silent failure if no token)
   */
  initAuth$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ROOT_EFFECTS_INIT),
      tap(() => console.log('Auth Effect: Initializing auth from localStorage')),
      exhaustMap(() => {
        const token = localStorage.getItem('access_token');
        if (!token) {
          console.log('Auth Effect: No token found in localStorage');
          return of(AuthActions.setAuthState({ 
            user: null, 
            token: null, 
            isAuthenticated: false, 
            isInitialized: true,
            error: null 
          }));
        }

        return this.authService.validateToken().pipe(
          map((result) => {
            if (result) {
              console.log('Auth Effect: Auth restored from token');
              // Using setAuthState instead of loginSuccess so we can set isInitialized=true
              return AuthActions.setAuthState({
                user: result.user, 
                token: result.token,
                isAuthenticated: true,
                isLoading: false,
                isInitialized: true,
                error: null
              });
            } else {
              console.log('Auth Effect: Token validation failed');
              return AuthActions.setAuthState({ 
                user: null, 
                token: null, 
                isAuthenticated: false,
                isInitialized: true,
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
              isInitialized: true,
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
          catchError((error: unknown) => {
            const message = authEffectErrorMessage(error);
            console.error('Auth Effect: Registration failed -', error);
            return of(AuthActions.registerFailure({ error: message }));
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

  /**
   * Connect SignalR Effect
   * Listens for: [Auth] Login Success
   * Side effect: Connects the SignalR hub
   */
  connectSignalR$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.loginSuccess, AuthActions.setAuthState),
        tap((action) => {
          // If it's a loginSuccess, or a setAuthState that sets isAuthenticated to true
          if (action.type === AuthActions.loginSuccess.type || (action as any).isAuthenticated) {
            console.log('Auth Effect: Authenticated, connecting SignalR');
            this.signalManager.connect();
          }
        })
      ),
    { dispatch: false }
  );

  /**
   * Disconnect SignalR Effect
   * Listens for: [Auth] Logout
   * Side effect: Disconnects the SignalR hub
   */
  disconnectSignalR$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.logout),
        tap(() => {
          console.log('Auth Effect: Logout, disconnecting SignalR');
          this.signalManager.disconnect();
        })
      ),
    { dispatch: false }
  );
}
