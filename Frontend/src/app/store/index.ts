import { ActionReducerMap } from "@ngrx/store";
import { AppState } from "./app.state";
import { authFeature } from "./reducers/auth.reducer";

// Use the feature's name and reducer from createFeature
// This registers the auth feature in the global store
// The feature name is 'auth', so this creates: { auth: authReducer }
export const appReducer: ActionReducerMap<AppState> = {
  [authFeature.name]: authFeature.reducer,
} as ActionReducerMap<AppState>;
