import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterModule } from '@angular/router';
import { provideMockStore, MockStore } from '@ngrx/store/testing';
import { BehaviorSubject } from 'rxjs';

import { HomeComponent } from './home.component';
import { SignalManagerService } from '../../core/services/signal-manager.service';
import { selectIsAdmin, selectIsOperator } from '../../store/selectors/auth.selectors';
import { authFeature } from '../../store/reducers/auth.reducer';
import { carFeature } from '../../store/reducers/car.reducer';
import { AppRoutes } from '../../core/constants/routes';
import { EMPTY_SENSOR_DATA } from '../../shared/models/car-sensor-data.model';

describe('HomeComponent — Analytics card (added in this PR)', () => {
  let fixture: ComponentFixture<HomeComponent>;
  let component: HomeComponent;
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
      [],
      { connectionState$: connectionStateSubject.asObservable() }
    );

    await TestBed.configureTestingModule({
      imports: [HomeComponent, RouterModule.forRoot([])],
      providers: [
        provideMockStore({ initialState }),
        { provide: SignalManagerService, useValue: signalManagerSpy },
      ],
    }).compileComponents();

    store = TestBed.inject(MockStore);
    store.overrideSelector(selectIsOperator, false);
    store.overrideSelector(selectIsAdmin, false);
    store.overrideSelector(authFeature.selectIsAuthenticated, false);
    store.overrideSelector(authFeature.selectUser, null);
    store.overrideSelector(carFeature.selectSensorData, EMPTY_SENSOR_DATA);

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    store.resetSelectors();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should expose routes.METRICS equal to "/metrics"', () => {
    expect(component.routes.METRICS).toBe('/metrics');
  });

  it('should expose routes.METRICS equal to AppRoutes.METRICS', () => {
    expect(component.routes.METRICS).toBe(AppRoutes.METRICS);
  });

  describe('Analytics card visibility', () => {
    it('should NOT show the Analytics card when user has no operator or admin role', () => {
      store.overrideSelector(selectIsOperator, false);
      store.overrideSelector(selectIsAdmin, false);
      store.refreshState();
      fixture.detectChanges();

      const card = fixture.nativeElement.querySelector('#feature-metrics');
      expect(card).toBeNull();
    });

    it('should show the Analytics card for a user with the Operator role', () => {
      store.overrideSelector(selectIsOperator, true);
      store.overrideSelector(selectIsAdmin, false);
      store.refreshState();
      fixture.detectChanges();

      const card = fixture.nativeElement.querySelector('#feature-metrics');
      expect(card).not.toBeNull();
    });

    it('should show the Analytics card for a user with the Admin role', () => {
      store.overrideSelector(selectIsOperator, false);
      store.overrideSelector(selectIsAdmin, true);
      store.refreshState();
      fixture.detectChanges();

      const card = fixture.nativeElement.querySelector('#feature-metrics');
      expect(card).not.toBeNull();
    });

    it('should show the Analytics card when user is both Operator and Admin', () => {
      store.overrideSelector(selectIsOperator, true);
      store.overrideSelector(selectIsAdmin, true);
      store.refreshState();
      fixture.detectChanges();

      const card = fixture.nativeElement.querySelector('#feature-metrics');
      expect(card).not.toBeNull();
    });
  });

  describe('Analytics card content', () => {
    beforeEach(() => {
      store.overrideSelector(selectIsOperator, true);
      store.overrideSelector(selectIsAdmin, false);
      store.refreshState();
      fixture.detectChanges();
    });

    it('should display "Analytics" as the card title', () => {
      const title = fixture.nativeElement.querySelector('#feature-metrics .feature-title');
      expect(title?.textContent?.trim()).toBe('Analytics');
    });

    it('should render the chart-line icon', () => {
      const icon = fixture.nativeElement.querySelector('#feature-metrics .pi-chart-line');
      expect(icon).not.toBeNull();
    });

    it('should render a description mentioning metrics or sensor data', () => {
      const desc = fixture.nativeElement.querySelector('#feature-metrics .feature-desc');
      const text: string = desc?.textContent ?? '';
      expect(text.toLowerCase()).toMatch(/metrics|sensor|analytics|historical/);
    });

    it('should include an arrow icon for navigation affordance', () => {
      const arrow = fixture.nativeElement.querySelector('#feature-metrics .feature-arrow');
      expect(arrow).not.toBeNull();
    });
  });

  describe('Analytics card is absent from DOM for non-privileged roles', () => {
    it('should not list #feature-metrics among rendered feature cards for unauthenticated user', () => {
      store.overrideSelector(selectIsOperator, false);
      store.overrideSelector(selectIsAdmin, false);
      store.refreshState();
      fixture.detectChanges();

      const cards = fixture.nativeElement.querySelectorAll('.feature-card');
      const ids: string[] = Array.from(cards).map((el: any) => (el as HTMLElement).id);
      expect(ids).not.toContain('feature-metrics');
    });
  });
});