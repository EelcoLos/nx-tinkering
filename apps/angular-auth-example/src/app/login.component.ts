import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Client } from '../api-integration/api';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { catchError, map } from 'rxjs';

@Component({
  selector: 'app-login',
  template: `
    <div class="login-container">
      <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
        <label for="email">Email:</label>
        <input id="email" formControlName="email" type="email" />
        @if (loginForm.get('email')?.invalid && loginForm.get('email')?.touched) {
          <div>Email is required and must be a valid email address.</div>
        }

        <label for="password">Password:</label>
        <input id="password" formControlName="password" type="password" />
        @if (loginForm.get('password')?.invalid && loginForm.get('password')?.touched) {
          <div>Password is required.</div>
        }

        <button type="submit" [disabled]="loginForm.invalid">Login</button>
      </form>
      @if (error) {
        <div class="error">{{ error }}</div>
      }
    </div>
  `,
  styles: [`
    .login-container {
      display: flex;
      justify-content: center;
      align-items: center;
      height: 100vh;
      flex-direction: column;
    }
    form {
      display: flex;
      flex-direction: column;
      width: 300px;
    }
    label, input, button, div {
      margin: 10px 0;
    }
    button {
      align-self: center;
    }
    .error {
      color: red;
      margin-top: 20px;
    }
  `],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  providers: [Client]
})
export class LoginComponent {
  loginForm: FormGroup;
  apiService = inject(Client)
  error: string | null = null;
  router = inject(Router);

  constructor(private fb: FormBuilder) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  onSubmit() {
    if (this.loginForm.valid) {
      const { email, password } = this.loginForm.value;
      console.log('Logging in...');
      this.apiService.login({ email, password }).pipe(map(response => {
        console.log('Login response:', response);
        if (!response.token) {
          this.error = 'Invalid email or password.';
          return [];
        }
        localStorage.setItem('token', response.token);

        this.router.navigate(['/endpoint']);
        return response; // Ensure a value is returned
      }),
        catchError(error => {
          console.error('Login error:', error);
          this.error = 'An error occurred while logging in. Please try again.';
          return [];
        })
      ).subscribe();
    }
  }
}
