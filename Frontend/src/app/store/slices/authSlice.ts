import {
  createSlice,
  createAsyncThunk,
  type PayloadAction,
} from "@reduxjs/toolkit";

// Define credentials and registration data types
export type LoginCredentials = {
  email: string;
  password: string;
};

export type RegistrationData = {
  name: string;
  email: string;
  password: string;
};

// Enum for request status
export enum Status {
  Idle = "idle",
  Loading = "loading",
  Succeeded = "succeeded",
  Failed = "failed",
}

export interface User {
  id: string;
  name: string;
  email: string;
  profilePictureUrl?: string;
}

export interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  status: Status;
  error: string | null;
}

// Helper function to load state from localStorage
const loadInitialState = (): AuthState => {
  try {
    const serializedState = localStorage.getItem("authState");
    if (serializedState === null) {
      return {
        user: null,
        token: null,
        isAuthenticated: false,
        status: Status.Idle,
        error: null,
      };
    }
    const storedState = JSON.parse(serializedState);
    return {
      ...storedState,
      status: Status.Idle, // Reset status on load
      error: null,
    };
  } catch {
    return {
      user: null,
      token: null,
      isAuthenticated: false,
      status: Status.Idle,
      error: null,
    };
  }
};

const initialState: AuthState = loadInitialState();

// 2. New Async Thunks
// Note: Replace with your actual API calls
export const loginUser = createAsyncThunk(
  "auth/loginUser",
  async (loginData: LoginCredentials, { rejectWithValue }) => {
    try {
      // Mock API call
      await new Promise((resolve) => setTimeout(resolve, 1000));
      if (loginData.email === "fail@test.com") {
        throw new Error("Invalid Credentials");
      }
      const response = {
        token: "fake-jwt-token",
        user: {
          id: "1",
          name: "Test User",
          email: loginData.email,
        },
      };
      return response;
    } catch (error) {
      if (error instanceof Error) {
        return rejectWithValue(error.message);
      }
      return rejectWithValue("An unknown error occurred during login.");
    }
  }
);

export const registerUser = createAsyncThunk(
  "auth/registerUser",
  async (registerData: RegistrationData, { rejectWithValue }) => {
    try {
      // Mock API call
      await new Promise((resolve) => setTimeout(resolve, 1000));
      const response = {
        token: "fake-jwt-token-on-register",
        user: {
          id: "2",
          name: registerData.name,
          email: registerData.email,
        },
      };
      return response;
    } catch (error) {
      if (error instanceof Error) {
        return rejectWithValue(error.message);
      }
      return rejectWithValue("An unknown error occurred during registration.");
    }
  }
);

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    logout: (state) => {
      state.user = null;
      state.token = null;
      state.isAuthenticated = false;
      localStorage.removeItem("authState");
    },
  },
  extraReducers: (builder) => {
    builder
      // Handle Login
      .addCase(loginUser.pending, (state) => {
        state.status = Status.Loading;
        state.error = null;
      })
      .addCase(
        loginUser.fulfilled,
        (state, action: PayloadAction<{ user: User; token: string }>) => {
          state.status = Status.Succeeded;
          state.isAuthenticated = true;
          state.user = action.payload.user;
          state.token = action.payload.token;
          localStorage.setItem(
            "authState",
            JSON.stringify({
              user: state.user,
              token: state.token,
              isAuthenticated: true,
            })
          );
        }
      )
      .addCase(loginUser.rejected, (state, action) => {
        state.status = Status.Failed;
        state.error = action.payload as string;
      })
      // Handle Register
      .addCase(registerUser.pending, (state) => {
        state.status = Status.Loading;
        state.error = null;
      })
      .addCase(
        registerUser.fulfilled,
        (state, action: PayloadAction<{ user: User; token: string }>) => {
          state.status = Status.Succeeded;
          state.isAuthenticated = true;
          state.user = action.payload.user;
          state.token = action.payload.token;
          localStorage.setItem(
            "authState",
            JSON.stringify({
              user: state.user,
              token: state.token,
              isAuthenticated: true,
            })
          );
        }
      )
      .addCase(registerUser.rejected, (state, action) => {
        state.status = Status.Failed;
        state.error = action.payload as string;
      });
  },
});

export const { logout } = authSlice.actions;
export default authSlice.reducer;
