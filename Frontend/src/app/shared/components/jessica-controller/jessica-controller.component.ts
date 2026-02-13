import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { MediaDisplayComponent } from '../media-display/media-display.component';
import { ControlPanelComponent } from '../control-panel/control-panel.component';
import { carFeature } from '../../../store/reducers/car.reducer';
import * as CarActions from '../../../store/actions/car.actions';

@Component({
  selector: 'app-jessica-controller',
  standalone: true,
  imports: [MediaDisplayComponent, ControlPanelComponent],
  templateUrl: './jessica-controller.component.html',
  styleUrl: './jessica-controller.component.scss',
})
export class JessicaControllerComponent implements OnInit, OnDestroy {
  private readonly store = inject(Store);
  private subscriptions = new Subscription();

  mediaData: string | null = null; // Will receive from WebSocket
  mediaType: 'image' | 'video' = 'image';
  isRunning = false;

  // ─────────────────────────────────────────────
  //  Lifecycle
  // ─────────────────────────────────────────────

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(carFeature.selectIsRunning).subscribe((running) => {
        console.log('[JessicaController] Store isRunning →', running);
        this.isRunning = running;
      })
    );
    console.log('[JessicaController] Initialized');
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    console.log('[JessicaController] Destroyed');
  }

  // ─────────────────────────────────────────────
  //  Direction change (unified — single/combo/idle)
  // ─────────────────────────────────────────────

  /**
   * Called by the control-panel (directionChange) output.
   * Values: "idle", "up", "down", "left", "right", "left-right", etc.
   * Already deduplicated — only fires on actual change.
   */
  onDirectionChange(direction: string): void {
    console.log(`[JessicaController] 🎮 Direction → "${direction}" — dispatching`);
    this.store.dispatch(CarActions.changeDirection({ direction }));
  }

  // ─────────────────────────────────────────────
  //  Start / Stop
  // ─────────────────────────────────────────────

  onStart = (): void => {
    console.log('[JessicaController] ▶ START → dispatching');
    this.store.dispatch(CarActions.startCar());
  };

  onStop = (): void => {
    console.log('[JessicaController] ⏹ STOP → dispatching');
    this.store.dispatch(CarActions.stopCar());
  };
}
