import { Component, Input, Output, EventEmitter } from "@angular/core";
import { CommonModule } from "@angular/common";

@Component({
  selector: "app-circular-button",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./circular-button.component.html",
  styleUrl: "./circular-button.component.scss",
})
export class CircularButtonComponent {
  @Input() icon?: string;
  @Input() text?: string;
  @Input() size: "small" | "medium" | "large" = "medium";
  @Input() severity: "primary" | "secondary" | "success" | "danger" = "secondary";
  @Input() isActive: boolean = false;
  @Output() buttonClick = new EventEmitter<void>();

  onClick(): void {
    this.buttonClick.emit();
  }

  get sizeClass(): string {
    return `size-${this.size}`;
  }

  get severityClass(): string {
    return `severity-${this.severity}`;
  }
}
