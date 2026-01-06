import { Component, Input } from "@angular/core";
import { CommonModule } from "@angular/common";

@Component({
  selector: "app-media-display",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./media-display.component.html",
  styleUrl: "./media-display.component.scss",
})
export class MediaDisplayComponent {
  @Input() mediaData: string | null = null; // URL or base64 data for image/video
  @Input() mediaType: "image" | "video" = "image"; // Type of media to display
}
