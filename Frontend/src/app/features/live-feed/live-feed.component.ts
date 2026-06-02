import { Component, OnInit, OnDestroy } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MediaDisplayComponent } from "../../shared/components/media-display/media-display.component";
import { environment } from "../../../environments/environment";

@Component({
  selector: "app-live-feed",
  standalone: true,
  imports: [CommonModule, MediaDisplayComponent],
  templateUrl: "./live-feed.component.html",
  styleUrl: "./live-feed.component.scss",
})
export class LiveFeedComponent implements OnInit, OnDestroy {
  mediaData: string | null = environment.cameraUrl;
  mediaType: "image" | "video" | "stream" = "stream";

  ngOnInit(): void {
    console.log("[LiveFeed] Initialized — camera URL:", environment.cameraUrl);
  }

  ngOnDestroy(): void {
    console.log("[LiveFeed] Destroyed");
  }
}
