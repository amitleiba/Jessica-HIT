import { Component, inject } from "@angular/core";
import { RouterOutlet } from "@angular/router";
import { NavbarComponent } from "./shared/components/navbar/navbar.component";
import { ToastModule } from "primeng/toast";
import { Store } from "@ngrx/store";
import { CommonModule } from "@angular/common";
import { authFeature } from "./store/reducers/auth.reducer";
import { SignalManagerService } from "./core/services/signal-manager.service";

@Component({
  selector: "app-root",
  standalone: true,
  imports: [CommonModule, RouterOutlet, NavbarComponent, ToastModule],
  templateUrl: "./app.component.html",
  styleUrl: "./app.component.scss",
})
export class AppComponent {
  private readonly store = inject(Store);
  private readonly signalManager = inject(SignalManagerService);

  isInitialized$ = this.store.select(authFeature.selectIsInitialized);
}
