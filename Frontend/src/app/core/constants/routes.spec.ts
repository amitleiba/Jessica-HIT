import { AppRoutes, RouteBuilders, RouteSegments } from './routes';

describe('RouteSegments', () => {
  describe('METRICS (added in this PR)', () => {
    it('should equal "metrics"', () => {
      expect(RouteSegments.METRICS).toBe('metrics');
    });

    it('should not contain a leading slash', () => {
      expect(RouteSegments.METRICS.startsWith('/')).toBeFalse();
    });
  });

  describe('existing segments remain unchanged', () => {
    it('HOME should equal "home"', () => {
      expect(RouteSegments.HOME).toBe('home');
    });

    it('USER_MANAGEMENT should equal "users"', () => {
      expect(RouteSegments.USER_MANAGEMENT).toBe('users');
    });

    it('LOGIN should equal "login"', () => {
      expect(RouteSegments.LOGIN).toBe('login');
    });

    it('REGISTER should equal "register"', () => {
      expect(RouteSegments.REGISTER).toBe('register');
    });
  });
});

describe('AppRoutes', () => {
  describe('METRICS (added in this PR)', () => {
    it('should equal "/metrics"', () => {
      expect(AppRoutes.METRICS).toBe('/metrics');
    });

    it('should start with a leading slash', () => {
      expect(AppRoutes.METRICS.startsWith('/')).toBeTrue();
    });

    it('should be derived from RouteSegments.METRICS', () => {
      expect(AppRoutes.METRICS).toBe(`/${RouteSegments.METRICS}`);
    });

    it('should be distinct from other routes', () => {
      const otherRoutes = [
        AppRoutes.HOME,
        AppRoutes.USER_MANAGEMENT,
        AppRoutes.LOGIN,
        AppRoutes.REGISTER,
        AppRoutes.LIVE_FEED,
        AppRoutes.RECORDER,
        AppRoutes.MANUAL_CONTROLLER,
      ];
      for (const route of otherRoutes) {
        expect(AppRoutes.METRICS).not.toBe(route);
      }
    });
  });

  describe('existing routes remain unchanged', () => {
    it('HOME should equal "/home"', () => {
      expect(AppRoutes.HOME).toBe('/home');
    });

    it('ROOT should equal "/"', () => {
      expect(AppRoutes.ROOT).toBe('/');
    });

    it('USER_MANAGEMENT should equal "/users"', () => {
      expect(AppRoutes.USER_MANAGEMENT).toBe('/users');
    });

    it('LOGIN should equal "/login"', () => {
      expect(AppRoutes.LOGIN).toBe('/login');
    });

    it('REGISTER should equal "/register"', () => {
      expect(AppRoutes.REGISTER).toBe('/register');
    });

    it('LIVE_FEED should equal "/live-feed"', () => {
      expect(AppRoutes.LIVE_FEED).toBe('/live-feed');
    });

    it('MANUAL_CONTROLLER should equal "/manual-controller"', () => {
      expect(AppRoutes.MANUAL_CONTROLLER).toBe('/manual-controller');
    });

    it('RECORDER should equal "/recorder"', () => {
      expect(AppRoutes.RECORDER).toBe('/recorder');
    });
  });

  describe('recorderReplay function', () => {
    it('should build URL without loop param by default', () => {
      expect(AppRoutes.recorderReplay('abc-123')).toBe('/recorder/replay/abc-123');
    });

    it('should append ?loop=1 when loop is true', () => {
      expect(AppRoutes.recorderReplay('abc-123', true)).toBe('/recorder/replay/abc-123?loop=1');
    });

    it('should not append query string when loop is false', () => {
      expect(AppRoutes.recorderReplay('abc-123', false)).toBe('/recorder/replay/abc-123');
    });
  });
});

describe('RouteBuilders', () => {
  it('loginWithRedirect should return correct path and queryParams', () => {
    const result = RouteBuilders.loginWithRedirect('/home');
    expect(result.path).toBe(AppRoutes.LOGIN);
    expect(result.queryParams).toEqual({ returnUrl: '/home' });
  });

  it('loginWithRegistrationSuccess should return correct path and queryParams', () => {
    const result = RouteBuilders.loginWithRegistrationSuccess('Welcome!');
    expect(result.path).toBe(AppRoutes.LOGIN);
    expect(result.queryParams).toEqual({ registered: 'true', message: 'Welcome!' });
  });
});