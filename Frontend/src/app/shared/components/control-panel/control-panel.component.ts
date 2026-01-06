import { Component, Input } from "@angular/core";
import { CircularButtonComponent } from "../circular-button/circular-button.component";
import { CommonModule } from "@angular/common";

@Component({
  selector: "app-control-panel",
  standalone: true,
  imports: [CircularButtonComponent, CommonModule],
  templateUrl: "./control-panel.component.html",
  styleUrl: "./control-panel.component.scss",
})
export class ControlPanelComponent {
  @Input() onUp?: () => void;
  @Input() onDown?: () => void;
  @Input() onLeft?: () => void;
  @Input() onRight?: () => void;
  @Input() onStart?: () => void;
  @Input() onStop?: () => void;
  @Input() isRunning: boolean = false;

  onStartStopClick(): void {
    if (this.isRunning && this.onStop) {
      this.onStop();
    } else if (!this.isRunning && this.onStart) {
      this.onStart();
    }
  }
}
