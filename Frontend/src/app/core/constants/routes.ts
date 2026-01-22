/**
 * Application Route Constants
 * Centralized route definitions to prevent typos and improve maintainability
 * 
 * Define route segments once, then derive full paths
 * Change a segment in one place, updates everywhere!
 * 
 * Usage:
 * - Navigation: this.router.navigate([AppRoutes.HOME]);
 * - Route Config: path: RouteSegments.HOME
 */

/**
 * Route Segments (without leading slash)
 * Used in Angular route configuration
 */
export const RouteSegments = {
  /** Home page segment */
  HOME: 'home',
  
  /** Login page segment */
  LOGIN: 'login',
  
  /** Registration page segment */
  REGISTER: 'register',
} as const;

/**
 * Full Route Paths (with leading slash)
 * Used for navigation throughout the app
 */
export const AppRoutes = {
  /** Root path - redirects to home */
  ROOT: '/',
  
  /** Home page - main dashboard/landing page */
  HOME: `/${RouteSegments.HOME}`,
  
  /** Login page - user authentication */
  LOGIN: `/${RouteSegments.LOGIN}`,
  
  /** Registration page - new user signup */
  REGISTER: `/${RouteSegments.REGISTER}`,
} as const;

/**
 * Route parameter builders for complex routes with query params
 * Use these when you need to pass data via URL
 */
export const RouteBuilders = {
  /**
   * Build login route with return URL
   * Used to redirect user back after login
   */
  loginWithRedirect: (returnUrl: string) => ({
    path: AppRoutes.LOGIN,
    queryParams: { returnUrl }
  }),
  
  /**
   * Build login route with registration success message
   * Used after successful registration to show message on login page
   */
  loginWithRegistrationSuccess: (message: string) => ({
    path: AppRoutes.LOGIN,
    queryParams: { registered: 'true', message }
  }),
} as const;

/**
 * Type-safe route type
 * Use this for function parameters that expect routes
 */
export type AppRoute = typeof AppRoutes[keyof typeof AppRoutes];
