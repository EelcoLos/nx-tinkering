import { Component, inject, signal } from '@angular/core';
import {
  email,
  form,
  FormField,
  FormRoot,
  required,
} from '@angular/forms/signals';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { DotnetFEAuthService } from '../api-integration/api';

@Component({
  selector: 'app-login',
  template: `
    <div class="login-container">
      <form [formRoot]="loginForm">
        <label for="email">Email:</label>
        <input id="email" type="email" [formField]="loginForm.email" />
        @if (loginForm.email().invalid() && loginForm.email().touched()) {
          <div>Email is required and must be a valid email address.</div>
        }

        <label for="password">Password:</label>
        <input id="password" type="password" [formField]="loginForm.password" />
        @if (loginForm.password().invalid() && loginForm.password().touched()) {
          <div>Password is required.</div>
        }

        <button type="submit" [disabled]="loginForm().invalid()">Login</button>
      </form>
      @if (error()) {
        <div class="error">{{ error() }}</div>
      }
    </div>
  `,
  styles: [
    `
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
      label,
      input,
      button,
      div {
        margin: 10px 0;
      }
      button {
        align-self: center;
      }
      .error {
        color: red;
        margin-top: 20px;
      }
    `,
  ],
  imports: [FormField, FormRoot],
})
export class LoginComponent {
  private readonly api = inject(DotnetFEAuthService);
  private readonly router = inject(Router);

  readonly error = signal<string | null>(null);

  readonly loginForm = form(
    signal({ email: '', password: '' }),
    (p) => {
      required(p.email);
      email(p.email);
      required(p.password);
    },
    {
      submission: {
        action: async (f) => {
          this.error.set(null);
          try {
            const { token } = await firstValueFrom(this.api.login(f().value()));
            localStorage.setItem('token', token);
            await this.router.navigate(['/endpoint']);
          } catch {
            this.error.set(
              'An error occurred while logging in. Please try again.',
            );
          }
          return undefined;
        },
      },
    },
  );
}
