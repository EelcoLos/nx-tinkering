import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MyEndpointService } from '../api-integration/api';

@Component({
  selector: 'app-myendpoint',
  template: `
    <form [formGroup]="myEndpointForm" (ngSubmit)="onSubmit()">
      <label for="firstname">First Name:</label>
      <input id="firstname" formControlName="firstname" type="text" />
      <div *ngIf="myEndpointForm.get('firstname')?.invalid && myEndpointForm.get('firstname')?.touched">
        First Name is required.
      </div>

      <label for="lastname">Last Name:</label>
      <input id="lastname" formControlName="lastname" type="text" />
      <div *ngIf="myEndpointForm.get('lastname')?.invalid && myEndpointForm.get('lastname')?.touched">
        Last Name is required.
      </div>

      <label for="age">Age:</label>
      <input id="age" formControlName="age" type="number" />
      <div *ngIf="myEndpointForm.get('age')?.invalid && myEndpointForm.get('age')?.touched">
        Age is required and must be a number.
      </div>

      <button type="submit" [disabled]="myEndpointForm.invalid">Submit</button>
    </form>

    <div *ngIf="response">
      <p>Full Name: {{ response.fullName }}</p>
      <p>Is Over 18: {{ response.isOver18 }}</p>
    </div>
  `,
  styles: []
})
export class MyEndpointComponent {
  myEndpointForm: FormGroup;
  response: any;

  constructor(private fb: FormBuilder, private myEndpointService: MyEndpointService) {
    this.myEndpointForm = this.fb.group({
      firstname: ['', Validators.required],
      lastname: ['', Validators.required],
      age: ['', [Validators.required, Validators.pattern('^[0-9]*$')]]
    });
  }

  onSubmit() {
    if (this.myEndpointForm.valid) {
      const { firstname, lastname, age } = this.myEndpointForm.value;
      this.myEndpointService.createUser({ firstname, lastname, age }).subscribe(
        (res) => {
          this.response = res;
        },
        (err) => {
          console.error('Error:', err);
        }
      );
    }
  }
}
