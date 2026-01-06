import { Component } from "@angular/core";
import { ThemeService } from "../../../core/services/theme.service";
import { CircularButtonComponent } from "../circular-button/circular-button.component";

@Component({
  selector: "app-theme-toggle",
  standalone: true,
  imports: [CircularButtonComponent],
  templateUrl: "./theme-toggle.component.html",
  styleUrl: "./theme-toggle.component.scss",
})
export class ThemeToggleComponent {
  constructor(public themeService: ThemeService) {}
}
