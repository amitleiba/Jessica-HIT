import { ActionReducerMap } from "@ngrx/store";
import { AppState } from "./app.state";
import { authFeature } from "./reducers/auth.reducer";
import { carFeature } from "./reducers/car.reducer";

// Register all feature reducers in the global store
export const appReducer: ActionReducerMap<AppState> = {
  [authFeature.name]: authFeature.reducer,
  [carFeature.name]: carFeature.reducer,
} as ActionReducerMap<AppState>;
