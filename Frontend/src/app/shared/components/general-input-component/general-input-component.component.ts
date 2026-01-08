import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';

@Component({
  selector: 'app-general-input-component',
  standalone: true,
  imports: [CommonModule, FormsModule, InputTextModule],
  templateUrl: './general-input-component.component.html',
  styleUrls: ['./general-input-component.component.scss']
})
export class GeneralInputComponentComponent {

  @Input() title: string = '';

  protected inputValue = '';

  constructor() {}
}
