import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { IClient, Client } from '../api-integration/api';
import { CommonModule } from '@angular/common';
import { map } from 'rxjs';

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
  providers: [{ provide: IClient, useClass: Client }]
})
export class LoginComponent {
  loginForm: FormGroup;
  apiService = inject(Client)
  error: string | null = null;

  constructor(private fb: FormBuilder) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  async onSubmit() {
    if (this.loginForm.valid) {
      const { email, password } = this.loginForm.value;
      try {
        const response = this.apiService.login({ Email: email, Password: password });
        // Handle successful login

      } catch (err) {
        // Handle login error
        this.error = err as string;
      }
    }
  }
}
