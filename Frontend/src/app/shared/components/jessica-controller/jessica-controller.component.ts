import { Component } from "@angular/core";
import { MediaDisplayComponent } from "../media-display/media-display.component";
import { ControlPanelComponent } from "../control-panel/control-panel.component";

@Component({
  selector: "app-jessica-controller",
  standalone: true,
  imports: [MediaDisplayComponent, ControlPanelComponent],
  templateUrl: "./jessica-controller.component.html",
  styleUrl: "./jessica-controller.component.scss",
})
export class JessicaControllerComponent {
  mediaData: string | null = null; // Will receive from WebSocket
  mediaType: "image" | "video" = "image";
  isRunning = false;

  onUp = (): void => {
    // TODO: Send up command via WebSocket
  };

  onDown = (): void => {
    // TODO: Send down command via WebSocket
  };

  onLeft = (): void => {
    // TODO: Send left command via WebSocket
  };

  onRight = (): void => {
    // TODO: Send right command via WebSocket
  };

  onStart = (): void => {
    this.isRunning = true;
    // TODO: Send start command via WebSocket
  };

  onStop = (): void => {
    this.isRunning = false;
    // TODO: Send stop command via WebSocket
  };
}


