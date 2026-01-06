import { AuthState } from "./reducers/auth.reducer";

// AppState interface - the 'auth' key matches the feature name from createFeature
export interface AppState {
  auth: AuthState;
}
