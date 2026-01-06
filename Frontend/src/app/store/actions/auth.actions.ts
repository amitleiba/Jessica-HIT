import { createAction, props } from "@ngrx/store";
import { User } from "../reducers/auth.reducer";

// Login flow actions
export const login = createAction(
  "[Auth] Login",
  props<{ email: string; password: string }>()
);

export const loginSuccess = createAction(
  "[Auth] Login Success",
  props<{ user: User; token: string }>()
);

export const loginFailure = createAction(
  "[Auth] Login Failure",
  props<{ error: string }>()
);

export const logout = createAction("[Auth] Logout");

export const loadUser = createAction(
  "[Auth] Load User",
  props<{ token: string }>()
);

// Direct state setters
export const setUser = createAction(
  "[Auth] Set User",
  props<{ user: User | null }>()
);

export const setToken = createAction(
  "[Auth] Set Token",
  props<{ token: string | null }>()
);

export const setAuthenticated = createAction(
  "[Auth] Set Authenticated",
  props<{ isAuthenticated: boolean }>()
);

export const setLoading = createAction(
  "[Auth] Set Loading",
  props<{ isLoading: boolean }>()
);

export const setError = createAction(
  "[Auth] Set Error",
  props<{ error: string | null }>()
);

export const setAuthState = createAction(
  "[Auth] Set Auth State",
  props<{
    user?: User | null;
    token?: string | null;
    isAuthenticated?: boolean;
    isLoading?: boolean;
    error?: string | null;
  }>()
);
