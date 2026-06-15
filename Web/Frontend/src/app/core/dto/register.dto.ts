/**
 * Registration DTOs
 * Request and response types for user registration
 */

/**
 * Registration request payload
 */
export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

/**
 * Registration response from backend
 */
export interface RegisterResponse {
  message: string;
  userId?: string;
}
