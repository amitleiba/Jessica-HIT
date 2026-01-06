import { createFeature, createReducer, on } from "@ngrx/store";
import * as AuthActions from "../actions/auth.actions";

// Auth State Interface
export interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

export interface User {
  id: string;
  email: string;
  name: string;
}

export const initialAuthState: AuthState = {
  user: null,
  token: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,
};

export const authReducer = createReducer(
  initialAuthState,

  // Login flow
  on(AuthActions.login, (state) => {
    console.log("[Auth Reducer] Login action - setting loading state");
    return {
      ...state,
      isLoading: true,
      error: null,
    };
  }),

  on(AuthActions.loginSuccess, (state, { user, token }) => {
    console.log(
      "[Auth Reducer] Login success - updating state with user:",
      user.email
    );
    return {
      ...state,
      user,
      token,
      isAuthenticated: true,
      isLoading: false,
      error: null,
    };
  }),

  on(AuthActions.loginFailure, (state, { error }) => {
    console.error("[Auth Reducer] Login failure - error:", error);
    return {
      ...state,
      isLoading: false,
      error,
      isAuthenticated: false,
    };
  }),

  on(AuthActions.logout, () => {
    console.log("[Auth Reducer] Logout - resetting to initial state");
    return initialAuthState;
  }),

  // Direct state setters
  on(AuthActions.setUser, (state, { user }) => {
    console.log("[Auth Reducer] Set user:", user?.email || "null");
    return {
      ...state,
      user,
    };
  }),

  on(AuthActions.setToken, (state, { token }) => {
    console.log("[Auth Reducer] Set token");
    return {
      ...state,
      token,
    };
  }),

  on(AuthActions.setAuthenticated, (state, { isAuthenticated }) => {
    console.log("[Auth Reducer] Set authenticated:", isAuthenticated);
    return {
      ...state,
      isAuthenticated,
    };
  }),

  on(AuthActions.setLoading, (state, { isLoading }) => {
    console.log("[Auth Reducer] Set loading:", isLoading);
    return {
      ...state,
      isLoading,
    };
  }),

  on(AuthActions.setError, (state, { error }) => {
    console.log("[Auth Reducer] Set error:", error);
    return {
      ...state,
      error,
    };
  }),

  on(AuthActions.setAuthState, (state, payload) => {
    console.log("[Auth Reducer] Set auth state with payload:", payload);
    return {
      ...state,
      ...payload,
    };
  })
);

// Create feature with automatic default selectors
// This automatically creates selectors for all state properties:
// authFeature.selectUser, authFeature.selectToken, authFeature.selectIsAuthenticated, etc.
export const authFeature = createFeature({
  name: "auth",
  reducer: authReducer,
});
