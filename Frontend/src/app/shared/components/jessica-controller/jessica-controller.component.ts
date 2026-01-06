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

  // Callback functions for control panel
  onUp = (): void => {
    console.log("[Jessica Controller] Up clicked");
    // TODO: Send up command via WebSocket
  };

  onDown = (): void => {
    console.log("[Jessica Controller] Down clicked");
    // TODO: Send down command via WebSocket
  };

  onLeft = (): void => {
    console.log("[Jessica Controller] Left clicked");
    // TODO: Send left command via WebSocket
  };

  onRight = (): void => {
    console.log("[Jessica Controller] Right clicked");
    // TODO: Send right command via WebSocket
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

