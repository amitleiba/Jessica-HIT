import { Component } from '@angular/core';
import { GeneralInputComponentComponent } from '../../shared/components/general-input-component/general-input-component.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [GeneralInputComponentComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {

  constructor() {}

  public onSubmit(): void {
    //TODO: implement registration logic and validation
    console.log('Register form submitted');
  }
}
