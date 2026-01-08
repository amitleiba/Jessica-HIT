import { Component } from '@angular/core';
import { GeneralInputComponentComponent } from '../../shared/components/general-input-component/general-input-component.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [GeneralInputComponentComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {

  constructor() {}

  public onSubmit(): void {
    //TODO: implement login logic and validation
    console.log('Login form submitted');
  }
}
