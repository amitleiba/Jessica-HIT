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
import { selectIsLoggingIn } from '../../../../store/selectors/auth.selectors';

/**
 * Login Form Component
 * Contains only the login form content (no container/card)
 * Used inside AuthComponent
 */
@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    ButtonModule,
    MessageModule,
    FormInputComponent
  ],
  templateUrl: './login-form.component.html',
  styleUrl: './login-form.component.scss'
})
export class LoginFormComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  loginForm: FormGroup;

  isLoggingIn$ = this.store.select(selectIsLoggingIn);
  error$ = this.store.select(authFeature.selectError);
  isAuthenticated$ = this.store.select(authFeature.selectIsAuthenticated);

  readonly routes = AppRoutes;

  constructor() {
    this.loginForm = this.fb.group({
      username: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  ngOnInit(): void {
    this.isAuthenticated$.subscribe(isAuthenticated => {
      if (isAuthenticated) {
        this.router.navigate([AppRoutes.HOME]);
      }
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    const { username, password } = this.loginForm.value;
    this.store.dispatch(AuthActions.login({ username, password }));
  }
}
