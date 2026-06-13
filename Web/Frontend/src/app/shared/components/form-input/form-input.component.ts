import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormControl, ReactiveFormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';

/**
 * Reusable Form Input Component
 * Handles text, email, and password inputs with validation display
 * Works with Angular Reactive Forms
 */
@Component({
  selector: 'app-form-input',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputTextModule, PasswordModule],
  templateUrl: './form-input.component.html',
  styleUrl: './form-input.component.scss'
})
export class FormInputComponent {
  @Input() label: string = '';
  @Input() type: 'text' | 'email' | 'password' = 'text';
  @Input() placeholder: string = '';
  @Input() autocomplete: string = '';
  @Input() control!: AbstractControl;
  @Input() showPasswordStrength: boolean = false;
  @Input() toggleMask: boolean = false;

  get formControl(): FormControl {
    return this.control as FormControl;
  }

  get hasError(): boolean {
    return this.control ? (this.control.invalid && (this.control.dirty || this.control.touched)) : false;
  }

  get errorMessage(): string {
    if (!this.control || !this.hasError) return '';

    const errors = this.control.errors;
    if (!errors) return '';

    if (errors['required']) return `${this.label} is required`;
    if (errors['email']) return 'Please enter a valid email address';
    if (errors['minlength']) {
      const required = errors['minlength'].requiredLength;
      return `${this.label} must be at least ${required} characters`;
    }
    if (errors['passwordMismatch']) return 'Passwords do not match';
    if (errors['pattern']) return `Invalid ${this.label.toLowerCase()} format`;
    
    return 'Invalid input';
  }

  get inputId(): string {
    return this.label.toLowerCase().replace(/\s+/g, '-');
  }
}
