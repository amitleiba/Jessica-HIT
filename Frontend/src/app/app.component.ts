import { Component, inject } from "@angular/core";
import { RouterOutlet } from "@angular/router";
import { NavbarComponent } from "./shared/components/navbar/navbar.component";
import { Store } from "@ngrx/store";
import * as AuthActions from "./store/actions/auth.actions";

@Component({
  selector: "app-root",
  standalone: true,
  imports: [RouterOutlet, NavbarComponent],
  templateUrl: "./app.component.html",
  styleUrl: "./app.component.scss",
})
export class AppComponent {
  private readonly store = inject(Store);

  constructor() {
    this.store.dispatch(AuthActions.initAuth());
  }
}
