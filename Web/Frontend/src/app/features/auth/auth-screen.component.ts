import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { CardModule } from 'primeng/card';
import { LoginFormComponent } from './components/login-form/login-form.component';
import { RegisterFormComponent } from './components/register-form/register-form.component';
import { AppRoutes } from '../../core/constants/routes';

/**
 * Auth Screen Component
 * Shared container for login and register forms
 * Detects route and conditionally renders the appropriate form
 * Controls sizing and layout - single source of truth for dimensions
 */
@Component({
  selector: 'app-auth-screen',
  standalone: true,
  imports: [
    CommonModule,
    CardModule,
    LoginFormComponent,
    RegisterFormComponent
  ],
  templateUrl: './auth-screen.component.html',
  styleUrl: './auth-screen.component.scss'
})
export class AuthScreenComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  mode: 'login' | 'register' = 'login';
  readonly routes = AppRoutes;

  ngOnInit(): void {
    // Detect route segment to determine mode
    const routeSegment = this.route.snapshot.url[0]?.path;
    if (routeSegment === 'register') {
      this.mode = 'register';
    } else {
      this.mode = 'login';
    }
  }

  switchToLogin(): void {
    this.mode = 'login';
    this.router.navigate([AppRoutes.LOGIN]);
  }

  switchToRegister(): void {
    this.mode = 'register';
    this.router.navigate([AppRoutes.REGISTER]);
  }
}
