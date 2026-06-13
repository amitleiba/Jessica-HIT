import { Component, inject, OnInit, OnDestroy } from "@angular/core";
import { Store } from "@ngrx/store";
import { Subscription } from "rxjs";
import { JessicaControllerComponent } from "../../shared/components/jessica-controller/jessica-controller.component";
import { SensorDataPanelComponent } from "../../shared/components/sensor-data-panel/sensor-data-panel.component";
import { CarSensorData } from "../../shared/models/car-sensor-data.model";
import { carFeature } from "../../store/reducers/car.reducer";
import * as CarActions from "../../store/actions/car.actions";

@Component({
  selector: "app-manual-controller",
  standalone: true,
  imports: [JessicaControllerComponent, SensorDataPanelComponent],
  templateUrl: "./manual-controller.component.html",
  styleUrl: "./manual-controller.component.scss",
})
export class ManualControllerComponent implements OnInit, OnDestroy {
  private readonly store = inject(Store);
  private subscriptions = new Subscription();

  speed = 0;
  currentDirection = 'idle';
  sensorData: CarSensorData = {};

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(carFeature.selectSpeed).subscribe((speed) => {
        this.speed = speed;
      })
    );
    this.subscriptions.add(
      this.store.select(carFeature.selectSensorData).subscribe((sensorData) => {
        this.sensorData = sensorData;
      })
    );
    this.subscriptions.add(
      this.store.select(carFeature.selectCurrentDirection).subscribe((direction) => {
        this.currentDirection = direction;
      })
    );
    console.log('[ManualController] Initialized');
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    console.log('[ManualController] Destroyed');
  }

  onSpeedChange(speed: number): void {
    console.log(`[ManualController] Speed dial → ${speed} — dispatching`);
    this.store.dispatch(CarActions.changeSpeed({ speed }));
  }
}

