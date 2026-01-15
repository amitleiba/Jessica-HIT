import { Component, ViewChild } from "@angular/core";
import { MapDisplayComponent } from "../map-display/map-display.component";
import { ControlPanelComponent } from "../control-panel/control-panel.component";

@Component({
  selector: "app-jessica-controller",
  standalone: true,
  imports: [MapDisplayComponent, ControlPanelComponent],
  templateUrl: "./jessica-controller.component.html",
  styleUrl: "./jessica-controller.component.scss",
})
export class JessicaControllerComponent {
  // ViewChild allows us to access the map component and call its methods
  @ViewChild(MapDisplayComponent) mapDisplay!: MapDisplayComponent;

  isRunning = false;

  // Callback functions for control panel - now connected to the map!
  onUp = (): void => {
    console.log("[Jessica Controller] Up clicked");
    this.mapDisplay.moveUp();  // Move the car up on the map
  };

  onDown = (): void => {
    console.log("[Jessica Controller] Down clicked");
    this.mapDisplay.moveDown();  // Move the car down on the map
  };

  onLeft = (): void => {
    console.log("[Jessica Controller] Left clicked");
    this.mapDisplay.moveLeft();  // Move the car left on the map
  };

  onRight = (): void => {
    console.log("[Jessica Controller] Right clicked");
    this.mapDisplay.moveRight();  // Move the car right on the map
  };

  onStart = (): void => {
    console.log("[Jessica Controller] Start clicked");
    this.isRunning = true;
    // TODO: Send start command via WebSocket
  };

  onStop = (): void => {
    console.log("[Jessica Controller] Stop clicked");
    this.isRunning = false;
    // TODO: Send stop command via WebSocket
  };
}
