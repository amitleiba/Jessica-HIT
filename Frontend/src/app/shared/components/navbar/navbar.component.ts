import { Component } from "@angular/core";
import { ThemeToggleComponent } from "../theme-toggle/theme-toggle.component";
import { CommonModule } from "@angular/common";
import { Router, RouterModule } from "@angular/router";
import { ButtonModule } from "primeng/button";

@Component({
  selector: "app-navbar",
  standalone: true,
  imports: [CommonModule, RouterModule, ThemeToggleComponent, ButtonModule],
  templateUrl: "./navbar.component.html",
  styleUrl: "./navbar.component.scss",
})
export class NavbarComponent {
  constructor(private router: Router) {}

  navigateToLogin(): void {
    this.router.navigate(["/login"]);
  }

  navigateToRegister(): void {
    this.router.navigate(["/register"]);
  }
}
