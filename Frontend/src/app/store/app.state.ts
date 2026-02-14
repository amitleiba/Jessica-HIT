import { AuthState } from "./reducers/auth.reducer";
import { CarState } from "./reducers/car.reducer";
import { RecordingState } from "./reducers/recording.reducer";

// AppState interface - each key matches its feature name from createFeature
export interface AppState {
  auth: AuthState;
  car: CarState;
  recording: RecordingState;
}
