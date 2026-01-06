import { Component } from "@angular/core";
import { JessicaControllerComponent } from "../../shared/components/jessica-controller/jessica-controller.component";

@Component({
  selector: "app-home",
  standalone: true,
  imports: [JessicaControllerComponent],
  templateUrl: "./home.component.html",
  styleUrl: "./home.component.scss",
})
export class HomeComponent {
  constructor() {
    console.log("[Home Component] Initialized");
  }
}
