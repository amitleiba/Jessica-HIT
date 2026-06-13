import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { Router, RouterLink } from '@angular/router';
import { AppRoutes } from '../../../../core/constants/routes';

// PrimeNG Imports
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';

// Shared Components
import { FormInputComponent } from '../../../../shared/components/form-input/form-input.component';

// Store
import * as AuthActions from '../../../../store/actions/auth.actions';
import { authFeature } from '../../../../store/reducers/auth.reducer';

/**
 * Register Form Component
 * Contains only the register form content (no container/card)
 * Used inside AuthComponent
 */
@Component({
  selector: 'app-register-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    ButtonModule,
    MessageModule,
    FormInputComponent
  ],
  templateUrl: './register-form.component.html',
  styleUrl: './register-form.component.scss'
})
export class RegisterFormComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  registerForm: FormGroup;

  isLoading$ = this.store.select(authFeature.selectIsLoading);
  error$ = this.store.select(authFeature.selectError);
  isAuthenticated$ = this.store.select(authFeature.selectIsAuthenticated);

  readonly routes = AppRoutes;

  constructor() {
    this.registerForm = this.fb.group({
      username: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  ngOnInit(): void {
    this.isAuthenticated$.subscribe(isAuthenticated => {
      if (isAuthenticated) {
        this.router.navigate([AppRoutes.HOME]);
      }
    });
  }

  private passwordMatchValidator(group: FormGroup): { [key: string]: boolean } | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    
    if (password !== confirmPassword) {
      group.get('confirmPassword')?.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    
    return null;
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const { username, email, firstName, lastName, password } = this.registerForm.value;
    this.store.dispatch(AuthActions.register({ 
      username, 
      email, 
      firstName, 
      lastName, 
      password 
    }));
  }
}
