import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, throwError, of, TimeoutError } from 'rxjs';
import { map, catchError, tap, switchMap, timeout } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ConfigService } from './config.service';

// Import DTOs from centralized dto folder
import {
  LoginRequest,
  RegisterRequest,
  RegisterResponse,
  User,
  UserInfoResponse,
  BackendLoginResponse
} from '../dto';

/**
 * Authentication Service
 * Handles all HTTP communication with the Gateway/Keycloak for authentication
 * Used by: AuthEffects (via NgRx)
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly configService = inject(ConfigService);

  private get apiUrl(): string {
    return this.configService.getApiUrl();
  }

  /** Abort hung requests so NgRx can leave `isLoading` (login/register) and show an error. */
  private static readonly requestTimeoutMs = 30_000;

  /**
   * Login with username and password via Backend Proxy (Secure)
   * Backend handles all Keycloak communication, keeping client_secret secure
   * Flow: Frontend → Backend → Keycloak → Backend → Frontend
   */
  login(username: string, password: string): Observable<{ user: User; token: string }> {
    const loginRequest: LoginRequest = {
      username,
      password
    };

    return this.http.post<BackendLoginResponse>(`${this.apiUrl}/api/Auth/login`, loginRequest).pipe(
      timeout(AuthService.requestTimeoutMs),
      map((response) => {
        const token = response.accessToken;
        this.storeToken(token);
        if (response.refreshToken) {
          this.storeRefreshToken(response.refreshToken);
        }
        const user = this.mapUserInfoToUser(response.userInfo);
        return { user, token };
      }),
      catchError((error) => {
        console.error('Login failed:', error);
        if (error instanceof TimeoutError) {
          return throwError(
            () =>
              'The server did not respond in time. Check that the gateway is running and reachable.'
          );
        }
        return throwError(() => this.handleError(error));
      })
    );
  }

  /**
   * Get user information from Gateway using JWT token
   * Still used for token validation and session restoration
   */
  private getUserInfo(token: string): Observable<{ userInfo: UserInfoResponse; token: string }> {
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });

    return this.http.get<UserInfoResponse>(`${this.apiUrl}/api/Auth/user-info`, { headers }).pipe(
      timeout(AuthService.requestTimeoutMs),
      map((userInfo) => ({ userInfo, token }))
    );
  }

  /**
   * Logout: Clear local storage and call Gateway logout endpoint
   */
  logout(): Observable<void> {
    const token = this.getStoredToken();
    if (!token) {
      this.clearStorage();
      return of(void 0);
    }

    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });

    return this.http.get(`${this.apiUrl}/api/Auth/logout`, { headers }).pipe(
      tap(() => {
        this.clearStorage();
      }),
      map(() => void 0),
      catchError(() => {
        this.clearStorage();
        return of(void 0);
      })
    );
  }

  /**
   * Register a new user in Keycloak
   * Note: This requires a Keycloak user registration endpoint or admin API access
   * For now, this is a placeholder - you'll need to implement the backend endpoint
   */
  register(registerData: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.apiUrl}/api/Auth/register`, registerData).pipe(
      timeout(AuthService.requestTimeoutMs),
      catchError((error) => {
        console.error('Registration failed:', error);
        if (error instanceof TimeoutError) {
          return throwError(
            () =>
              'The server did not respond in time. Check that the gateway is running and reachable.'
          );
        }
        return throwError(() => this.handleError(error));
      })
    );
  }

  /**
   * Validate stored token and restore user session
   */
  validateToken(): Observable<{ user: User; token: string } | null> {
    const token = this.getStoredToken();
    
    if (!token) {
      return of(null);
    }

    return this.getUserInfo(token).pipe(
      map(({ userInfo, token }) => {
        if (!userInfo.isAuthenticated) {
          this.clearStorage();
          return null;
        }

        const user = this.mapUserInfoToUser(userInfo);
        return { user, token };
      }),
      catchError(() => {
        this.clearStorage();
        return of(null);
      })
    );
  }

  /**
   * Refresh the access token using the stored refresh token
   */
  refreshToken(): Observable<{ token: string }> {
    const refreshToken = this.getStoredRefreshToken();
    const accessToken = this.getStoredToken();

    if (!refreshToken || !accessToken) {
      return throwError(() => new Error('No tokens available for refresh'));
    }

    return this.http.post<BackendLoginResponse>(`${this.apiUrl}/api/Auth/refresh`, {
      accessToken,
      refreshToken
    }).pipe(
      timeout(AuthService.requestTimeoutMs),
      map((response) => {
        const token = response.accessToken;
        this.storeToken(token);
        if (response.refreshToken) {
          this.storeRefreshToken(response.refreshToken);
        }
        return { token };
      })
    );
  }

  // ============================================
  // User Management Admin Methods
  // ============================================

  getUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/api/Auth/users`).pipe(
      timeout(AuthService.requestTimeoutMs),
      catchError((error) => throwError(() => this.handleError(error)))
    );
  }

  createUser(request: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/api/Auth/users`, request).pipe(
      timeout(AuthService.requestTimeoutMs),
      catchError((error) => throwError(() => this.handleError(error)))
    );
  }

  deleteUser(id: string): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/api/Auth/users/${id}`).pipe(
      timeout(AuthService.requestTimeoutMs),
      catchError((error) => throwError(() => this.handleError(error)))
    );
  }

  updateUserRole(userId: string, role: string): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/api/Auth/users/${userId}/role`, { role }).pipe(
      timeout(AuthService.requestTimeoutMs),
      catchError((error) => throwError(() => this.handleError(error)))
    );
  }

  // ============================================
  // Private Helper Methods
  // ============================================

  /**
   * Map backend UserInfoResponse to frontend User model
   * Handles various Keycloak claim types and formats
   * 
   * Note: The long strings like "http://schemas.xmlsoap.org/..." are NOT URLs.
   * They are XML namespace identifiers used as claim type strings by Keycloak/ASP.NET.
   */
  private mapUserInfoToUser(userInfo: UserInfoResponse): User {
    // Claim type constants (these are string identifiers, not URLs)
    const CLAIM_TYPES = {
      // OIDC standard claim types
      SUB: 'sub',
      EMAIL: 'email',
      NAME: 'name',
      GIVEN_NAME: 'given_name',
      FAMILY_NAME: 'family_name',
      PREFERRED_USERNAME: 'preferred_username',
      
      // ASP.NET/WS-Federation claim types (XML namespace identifiers)
      NAME_IDENTIFIER_WS: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
      NAME_IDENTIFIER_MS: 'http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier',
      EMAILADDRESS_WS: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
      EMAILADDRESS_MS: 'http://schemas.microsoft.com/ws/2008/06/identity/claims/emailaddress',
      GIVENNAME_WS: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname',
      SURNAME_WS: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'
    };

    // Find ID claim - prefer 'sub' (OIDC standard), fallback to nameidentifier
    const idClaim = userInfo.claims.find(c => 
      c.type === CLAIM_TYPES.SUB || 
      c.type === CLAIM_TYPES.NAME_IDENTIFIER_WS ||
      c.type === CLAIM_TYPES.NAME_IDENTIFIER_MS
    );

    // Find email claim - prefer 'email', fallback to emailaddress
    const emailClaim = userInfo.claims.find(c => 
      c.type === CLAIM_TYPES.EMAIL || 
      c.type === CLAIM_TYPES.EMAILADDRESS_WS ||
      c.type === CLAIM_TYPES.EMAILADDRESS_MS
    );

    // Find name claim - be specific to avoid matching nameidentifier
    // Try: name, given_name + family_name, preferred_username
    let nameClaim = userInfo.claims.find(c => c.type === CLAIM_TYPES.NAME);
    
    // If no 'name' claim, try combining given_name and family_name
    if (!nameClaim) {
      const givenName = userInfo.claims.find(c => 
        c.type === CLAIM_TYPES.GIVEN_NAME || 
        c.type === CLAIM_TYPES.GIVENNAME_WS
      );
      const familyName = userInfo.claims.find(c => 
        c.type === CLAIM_TYPES.FAMILY_NAME || 
        c.type === CLAIM_TYPES.SURNAME_WS
      );
      
      if (givenName || familyName) {
        const fullName = [givenName?.value, familyName?.value].filter(Boolean).join(' ');
        if (fullName) {
          nameClaim = { type: CLAIM_TYPES.NAME, value: fullName };
        }
      }
    }

    // Fallback to preferred_username if no name found
    if (!nameClaim) {
      nameClaim = userInfo.claims.find(c => c.type === CLAIM_TYPES.PREFERRED_USERNAME);
    }

    // Final fallback to username from UserInfoResponse
    const displayName = nameClaim?.value || userInfo.username || 'Unknown User';

    const roles = userInfo.claims
      .filter((c) => c.type === 'role')
      .map((c) => c.value);

    return {
      id: idClaim?.value || 'unknown',
      email: emailClaim?.value || userInfo.username || 'unknown',
      name: displayName,
      roles
    };
  }

  /**
   * Store JWT token in localStorage
   */
  private storeToken(token: string): void {
    localStorage.setItem('access_token', token);
  }

  /**
   * Get stored JWT token from localStorage
   */
  getStoredToken(): string | null {
    return localStorage.getItem('access_token');
  }

  /**
   * Store refresh token in localStorage
   */
  private storeRefreshToken(token: string): void {
    localStorage.setItem('refresh_token', token);
  }

  /**
   * Get stored refresh token from localStorage
   */
  getStoredRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  /**
   * Clear all auth data from localStorage
   */
  private clearStorage(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  }

  /**
   * Handle HTTP errors with user-friendly messages
   */
  private handleError(error: any): string {
    if (error.status === 401) {
      return 'Invalid username or password';
    }
    if (error.status === 0) {
      return 'Cannot connect to server. Please check your network connection.';
    }
    if (error.error?.error_description) {
      return error.error.error_description;
    }
    if (error.error?.message) {
      return error.error.message;
    }
    return error.message || 'An unexpected error occurred. Please try again.';
  }
}
