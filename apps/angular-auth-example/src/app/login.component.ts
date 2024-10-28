import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-login',
  template: `
    <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
      <label for="email">Email:</label>
      <input id="email" formControlName="email" type="email" />
      <div *ngIf="loginForm.get('email')?.invalid && loginForm.get('email')?.touched">
        Email is required and must be a valid email address.
      </div>

      <label for="password">Password:</label>
      <input id="password" formControlName="password" type="password" />
      <div *ngIf="loginForm.get('password')?.invalid && loginForm.get('password')?.touched">
        Password is required.
      </div>

      <button type="submit" [disabled]="loginForm.invalid">Login</button>
    </form>
  `,
  styles: []
})
export class LoginComponent {
  loginForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  onSubmit() {
    if (this.loginForm.valid) {
      const { email, password } = this.loginForm.value;
      // Handle login logic here
      console.log('Email:', email);
      console.log('Password:', password);
    }
  }
}
