import { Component, inject, signal } from '@angular/core';
import { Client, MyRequest, MyResponse } from '../api-integration/api';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { catchError, map } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-endpoint',
  template: `
    <div class="container">
      <h2>Endpoint Form</h2>
      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <div class="form-group">
          <label for="firstName">First Name:</label>
          <input id="firstName" formControlName="firstName" />
        </div>
        <div class="form-group">
          <label for="lastName">Last Name:</label>
          <input id="lastName" formControlName="lastName" />
        </div>
        <div class="form-group">
          <label for="age">Age:</label>
          <input id="age" formControlName="age" />
        </div>
        <button type="submit">Submit</button>
      </form>
      <pre>{{ data() | json }}</pre>
    </div>
  `,
  styles: [`
    .container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100vh;
      font-family: Arial, sans-serif;
    }
    form {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      margin-bottom: 20px;
    }
    .form-group {
      margin-bottom: 10px;
    }
    label {
      margin-bottom: 5px;
    }
    input {
      margin-bottom: 10px;
      padding: 5px;
      width: 200px;
    }
    button {
      padding: 5px 10px;
    }
    h2 {
      margin-bottom: 20px;
    }
  `],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  providers: [Client]
})
export class EndpointComponent {
  apiService = inject(Client);
  form: FormGroup;
  data = signal<MyResponse | null>(null);

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      firstName: [''],
      lastName: [''],
      age: ['']
    });
  }

  onSubmit() {
    const formValue = this.form.value;
    const requestPayload: MyRequest = {
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      age: formValue.age
    };
    this.apiService.createuser(requestPayload).pipe(
      map((response) => {
        console.log('Response:', response);
        this.data.update(() => response);
      }),
      catchError((error) => {
        this.data.update(error);
        throw error;
      })
    ).subscribe();
  }
}
