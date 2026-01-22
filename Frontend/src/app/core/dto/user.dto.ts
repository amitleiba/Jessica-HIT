/**
 * User DTOs
 * User-related data transfer objects
 */

/**
 * User entity
 */
export interface User {
  id: string;
  email: string;
  name: string;
}

/**
 * User info response from backend
 * Contains authentication status and user claims
 */
export interface UserInfoResponse {
  isAuthenticated: boolean;
  username: string | null;
  authenticationType: string | null;
  claims: ClaimDto[];
}

/**
 * User claim DTO
 * Represents a single claim (key-value pair)
 */
export interface ClaimDto {
  type: string;
  value: string;
}
