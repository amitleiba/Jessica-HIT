import { createFeature, createReducer, createSelector, on } from "@ngrx/store";
import * as AuthActions from "../actions/auth.actions";
import { User } from "../../core/dto";

// Auth State Interface
export interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
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
    console.log('Auth Reducer: Login action');
    return {
      ...state,
      isLoading: true,
      error: null,
    };
  }),

  on(AuthActions.loginSuccess, (state, { user, token }) => {
    console.log('Auth Reducer: Login success');
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
    console.log('Auth Reducer: Login failure -', error);
    return {
      ...state,
      isLoading: false,
      error,
      isAuthenticated: false,
    };
  }),

  on(AuthActions.logout, () => {
    console.log('Auth Reducer: Logout');
    return initialAuthState;
  }),

  // Register flow
  on(AuthActions.register, (state) => {
    console.log('Auth Reducer: Register action');
    return {
      ...state,
      isLoading: true,
      error: null,
    };
  }),

  on(AuthActions.registerSuccess, (state, { message }) => {
    console.log('Auth Reducer: Register success');
    return {
      ...state,
      isLoading: false,
      error: null,
    };
  }),

  on(AuthActions.registerFailure, (state, { error }) => {
    console.log('Auth Reducer: Register failure -', error);
    return {
      ...state,
      isLoading: false,
      error,
    };
  }),

  // Direct state setters
  on(AuthActions.setUser, (state, { user }) => {
    return {
      ...state,
      user,
    };
  }),

  on(AuthActions.setToken, (state, { token }) => {
    return {
      ...state,
      token,
    };
  }),

  on(AuthActions.setAuthenticated, (state, { isAuthenticated }) => {
    return {
      ...state,
      isAuthenticated,
    };
  }),

  on(AuthActions.setLoading, (state, { isLoading }) => {
    return {
      ...state,
      isLoading,
    };
  }),

  on(AuthActions.setError, (state, { error }) => {
    return {
      ...state,
      error,
    };
  }),

  on(AuthActions.setAuthState, (state, payload) => {
    console.log('Auth Reducer: Setting auth state');
    return {
      ...state,
      ...payload,
    };
  })
);

/**
 * Auth Feature with Auto-Generated Selectors
 * 
 * createFeature automatically generates selectors for all state properties:
 * - authFeature.selectUser
 * - authFeature.selectToken
 * - authFeature.selectIsAuthenticated
 * - authFeature.selectIsLoading
 * - authFeature.selectError
 * - authFeature.selectAuthState (entire state)
 * 
 * Custom/derived selectors are defined in auth.selectors.ts for better organization
 */
export const authFeature = createFeature({
  name: "auth",
  reducer: authReducer,
});
