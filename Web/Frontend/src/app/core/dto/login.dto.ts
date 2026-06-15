/**
 * Login DTOs
 * Request and response types for authentication login
 */

import { UserInfoResponse } from './user.dto';

/**
 * Login request payload
 * Sent to backend (backend proxies to Keycloak)
 */
export interface LoginRequest {
  username: string;
  password: string;
}

/**
 * Login response from backend
 * Backend returns token + user info in one response
 */
export interface BackendLoginResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  refreshToken?: string;
  userInfo: UserInfoResponse;
}

/**
 * Legacy: Direct Keycloak token response
 * @deprecated Use BackendLoginResponse instead (more secure - backend proxy)
 */
export interface LoginResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  refresh_token?: string;
}
