import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ResponseModel } from '../../models/response.model';
import { passwordComplexityValidator } from '../../validators/password-complexity.validator';

@Component({
  selector: 'app-create-account',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './create-account.component.html',
  styleUrl: './create-account.component.css'
})
export class CreateAccountComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly errorMessage = signal<string | null>(null);
  readonly isSubmitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, passwordComplexityValidator]]
  });

  get passwordErrors() {
    return this.form.controls.password.errors;
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    this.authService.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigate(['/login'], { queryParams: { registered: '1' } });
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        const body = err.error as ResponseModel | null;
        this.errorMessage.set(body?.message ?? 'Registration failed. Please try again.');
      }
    });
  }
}
