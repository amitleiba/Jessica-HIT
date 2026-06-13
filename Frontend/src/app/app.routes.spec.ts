import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { routes } from './app.routes';
import { RouteSegments } from './core/constants/routes';

describe('app.routes', () => {
  describe('METRICS route (added in this PR)', () => {
    let metricsRoute: any;

    beforeEach(() => {
      metricsRoute = routes.find(r => r.path === RouteSegments.METRICS);
    });

    it('should define a route for the "metrics" path', () => {
      expect(metricsRoute).toBeDefined();
    });

    it('should use the METRICS segment value "metrics" as the path', () => {
      expect(metricsRoute?.path).toBe('metrics');
    });

    it('should have canActivate guards', () => {
      expect(metricsRoute?.canActivate).toBeDefined();
      expect(Array.isArray(metricsRoute?.canActivate)).toBeTrue();
    });

    it('should include authGuard', () => {
      expect(metricsRoute?.canActivate).toContain(authGuard);
    });

    it('should include roleGuard', () => {
      expect(metricsRoute?.canActivate).toContain(roleGuard);
    });

    it('should restrict access to Operator and Admin roles only', () => {
      const expectedRoles = metricsRoute?.data?.['expectedRoles'];
      expect(expectedRoles).toEqual(['Operator', 'Admin']);
    });

    it('should NOT include Viewer in expectedRoles', () => {
      const expectedRoles = metricsRoute?.data?.['expectedRoles'];
      expect(expectedRoles).not.toContain('Viewer');
    });

    it('should have a loadComponent function for lazy loading', () => {
      expect(metricsRoute?.loadComponent).toBeDefined();
      expect(typeof metricsRoute?.loadComponent).toBe('function');
    });

    it('loadComponent should return a Promise (lazy-loaded module)', () => {
      const result = metricsRoute?.loadComponent?.();
      expect(result instanceof Promise).toBeTrue();
    });
  });

  describe('METRICS route guards compared to other role-restricted routes', () => {
    it('should have the same guards as USER_MANAGEMENT route', () => {
      const metricsRoute = routes.find(r => r.path === RouteSegments.METRICS);
      const usersRoute = routes.find(r => r.path === RouteSegments.USER_MANAGEMENT);

      expect(metricsRoute?.canActivate).toEqual(usersRoute?.canActivate);
    });

    it('should have broader role access than USER_MANAGEMENT (Admin only)', () => {
      const metricsRoute = routes.find(r => r.path === RouteSegments.METRICS);
      const usersRoute = routes.find(r => r.path === RouteSegments.USER_MANAGEMENT);

      const metricsRoles: string[] = metricsRoute?.data?.['expectedRoles'] ?? [];
      const usersRoles: string[] = usersRoute?.data?.['expectedRoles'] ?? [];

      expect(metricsRoles.length).toBeGreaterThan(usersRoles.length);
      expect(metricsRoles).toContain('Operator');
      expect(usersRoles).not.toContain('Operator');
    });
  });

  describe('overall route configuration', () => {
    it('should contain a catch-all redirect to HOME', () => {
      const catchAll = routes.find(r => r.path === '**');
      expect(catchAll).toBeDefined();
      expect(catchAll?.redirectTo).toBe('/home');
    });

    it('should contain a root redirect to HOME', () => {
      const root = routes.find(r => r.path === '');
      expect(root).toBeDefined();
      expect(root?.redirectTo).toBe('/home');
      expect(root?.pathMatch).toBe('full');
    });

    it('should define 11 routes total (including root, catch-all, and metrics)', () => {
      expect(routes.length).toBe(11);
    });
  });
});