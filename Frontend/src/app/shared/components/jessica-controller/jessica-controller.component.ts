import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { MediaDisplayComponent } from '../media-display/media-display.component';
import { ControlPanelComponent } from '../control-panel/control-panel.component';
import * as CarActions from '../../../store/actions/car.actions';
import { ButtonModule } from 'primeng/button';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-jessica-controller',
  standalone: true,
  imports: [MediaDisplayComponent, ControlPanelComponent, ButtonModule],
  templateUrl: './jessica-controller.component.html',
  styleUrl: './jessica-controller.component.scss',
})
export class JessicaControllerComponent implements OnInit, OnDestroy {
  private readonly store = inject(Store);
  private subscriptions = new Subscription();

  mediaData: string | null = environment.cameraUrl; // Live camera feed URL
  mediaType: 'image' | 'video' | 'stream' = 'stream';

  // ─────────────────────────────────────────────
  //  Lifecycle
  // ─────────────────────────────────────────────

  ngOnInit(): void {
    console.log('[JessicaController] Initialized — camera feed:', environment.cameraUrl);
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

  onEmergencyStop(): void {
    console.log('[JessicaController] 🛑 EMERGENCY STOP');
    this.store.dispatch(CarActions.emergencyStop());
  }
}
