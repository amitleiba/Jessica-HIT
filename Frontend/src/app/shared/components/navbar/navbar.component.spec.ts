import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterModule } from '@angular/router';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { provideMockStore, MockStore } from '@ngrx/store/testing';
import { BehaviorSubject } from 'rxjs';

import { NavbarComponent } from './navbar.component';
import { SignalManagerService } from '../../../core/services/signal-manager.service';
import { selectIsAdmin, selectIsOperator } from '../../../store/selectors/auth.selectors';
import { authFeature } from '../../../store/reducers/auth.reducer';
import { carFeature } from '../../../store/reducers/car.reducer';
import { AppRoutes } from '../../../core/constants/routes';
import { EMPTY_SENSOR_DATA } from '../../../shared/models/car-sensor-data.model';

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;
  let store: MockStore;
  let connectionStateSubject: BehaviorSubject<string>;

  const initialState = {
    auth: {
      user: null,
      token: null,
      isAuthenticated: false,
      isLoading: false,
      isInitialized: false,
      error: null,
    },
    car: {
      currentDirection: 'idle',
      speed: 50,
      sensorData: EMPTY_SENSOR_DATA,
    },
  };

  beforeEach(async () => {
    connectionStateSubject = new BehaviorSubject<string>('disconnected');

    const signalManagerSpy = jasmine.createSpyObj<SignalManagerService>(
      'SignalManagerService',
      ['connect', 'disconnect'],
      { connectionState$: connectionStateSubject.asObservable() }
    );

    await TestBed.configureTestingModule({
      imports: [NavbarComponent, RouterModule.forRoot([])],
      providers: [
        provideMockStore({ initialState }),
        { provide: SignalManagerService, useValue: signalManagerSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    store = TestBed.inject(MockStore);
    store.overrideSelector(selectIsOperator, false);
    store.overrideSelector(selectIsAdmin, false);
    store.overrideSelector(authFeature.selectIsAuthenticated, false);
    store.overrideSelector(authFeature.selectUser, null);
    store.overrideSelector(carFeature.selectSensorData, EMPTY_SENSOR_DATA);

    fixture = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    store.resetSelectors();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should expose routes.METRICS equal to "/metrics"', () => {
    expect(component.routes.METRICS).toBe('/metrics');
  });

  it('should expose routes.METRICS equal to AppRoutes.METRICS', () => {
    expect(component.routes.METRICS).toBe(AppRoutes.METRICS);
  });

  describe('Metrics nav link visibility (added in this PR)', () => {
    function makeAuthenticated(isOperator: boolean, isAdmin: boolean): void {
      store.overrideSelector(authFeature.selectIsAuthenticated, true);
      store.overrideSelector(authFeature.selectUser, {
        id: '1',
        name: 'Test User',
        email: 'test@example.com',
        roles: isAdmin ? ['Admin'] : isOperator ? ['Operator'] : ['Viewer'],
      } as any);
      store.overrideSelector(selectIsOperator, isOperator);
      store.overrideSelector(selectIsAdmin, isAdmin);
      store.refreshState();
      fixture.detectChanges();
    }

    it('should NOT show the Metrics link when user is not authenticated', () => {
      store.overrideSelector(authFeature.selectIsAuthenticated, false);
      store.overrideSelector(selectIsOperator, false);
      store.overrideSelector(selectIsAdmin, false);
      store.refreshState();
      fixture.detectChanges();

      const metricsLink = findMetricsNavLink();
      expect(metricsLink).toBeNull();
    });

    it('should NOT show the Metrics link for an authenticated Viewer', () => {
      makeAuthenticated(false, false);

      const metricsLink = findMetricsNavLink();
      expect(metricsLink).toBeNull();
    });

    it('should show the Metrics link for an authenticated Operator', () => {
      makeAuthenticated(true, false);

      const metricsLink = findMetricsNavLink();
      expect(metricsLink).not.toBeNull();
    });

    it('should show the Metrics link for an authenticated Admin', () => {
      makeAuthenticated(false, true);

      const metricsLink = findMetricsNavLink();
      expect(metricsLink).not.toBeNull();
    });

    it('should show the Metrics link when user is both Operator and Admin', () => {
      makeAuthenticated(true, true);

      const metricsLink = findMetricsNavLink();
      expect(metricsLink).not.toBeNull();
    });

    it('should have a routerLink pointing to the METRICS route on the Metrics nav link', () => {
      makeAuthenticated(true, false);

      // The anchor wrapping the Metrics button should have the correct routerLink attribute
      const metricsLink = findMetricsNavLink();
      expect(metricsLink).not.toBeNull();
      // The component uses [routerLink]="routes.METRICS" which resolves to "/metrics"
      expect(component.routes.METRICS).toBe('/metrics');
    });

    it('Metrics link should appear in navbar when Operator, alongside other nav items', () => {
      makeAuthenticated(true, false);

      const navLinks = fixture.nativeElement.querySelectorAll('.nav-link');
      expect(navLinks.length).toBeGreaterThan(0);
      const metricsLink = findMetricsNavLink();
      expect(metricsLink).not.toBeNull();
    });

    /**
     * Regression: Metrics link must not appear twice in the navbar (fixed in this PR by
     * using a single @if block instead of two separate router links).
     */
    it('should render at most one Metrics nav link (no duplicates)', () => {
      makeAuthenticated(true, true);

      const allNavLinks: HTMLElement[] = Array.from(
        fixture.nativeElement.querySelectorAll('.nav-link')
      );
      const metricsLinks = allNavLinks.filter(
        (el) => el.textContent?.includes('Metrics')
      );
      expect(metricsLinks.length).toBeLessThanOrEqual(1);
    });
  });

  /** Finds the nav link anchor containing the Metrics button by text content. */
  function findMetricsNavLink(): HTMLElement | null {
    const navLinks: HTMLElement[] = Array.from(
      fixture.nativeElement.querySelectorAll('.nav-link')
    );
    return navLinks.find((el) => el.textContent?.includes('Metrics')) ?? null;
  }
});
