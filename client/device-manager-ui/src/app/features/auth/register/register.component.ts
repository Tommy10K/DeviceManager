import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';

import { environment } from '../../../../environments/environment';

interface RegisterResponse {
  id: string;
  name: string;
  email: string;
  role: number;
  location: string;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    RouterLink,
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  isSubmitting = false;
  errorMessage = '';
  readonly form;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly http: HttpClient,
    private readonly router: Router,
  ) {
    this.form = this.formBuilder.nonNullable.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      location: ['', [Validators.required, Validators.maxLength(200)]],
    });
  }

  register(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    this.isSubmitting = true;

    this.http
      .post<RegisterResponse>(`${environment.apiUrl}/auth/register`, this.form.getRawValue())
      .subscribe({
        next: () => {
          this.router.navigate(['/login'], {
            queryParams: { registered: '1' },
          });
        },
        error: (error) => {
          if (error?.status === 409) {
            this.errorMessage = 'Email already in use.';
          } else {
            this.errorMessage = 'Registration failed. Please try again.';
          }

          this.isSubmitting = false;
        },
        complete: () => {
          this.isSubmitting = false;
        },
      });
  }
}
