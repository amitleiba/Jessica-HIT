import { createAction, props } from "@ngrx/store";
import { User } from "../../core/dto";

// Login flow actions
export const login = createAction(
  "[Auth] Login",
  props<{ username: string; password: string }>()
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

export const initAuth = createAction("[Auth] Init Auth");

// Register flow actions
export const register = createAction(
  "[Auth] Register",
  props<{ username: string; email: string; password: string; firstName: string; lastName: string }>()
);

export const registerSuccess = createAction(
  "[Auth] Register Success",
  props<{ message: string }>()
);

export const registerFailure = createAction(
  "[Auth] Register Failure",
  props<{ error: string }>()
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
