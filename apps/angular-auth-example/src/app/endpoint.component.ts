import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { form, FormField, FormRoot } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { JsonPipe } from '@angular/common';
import { DotnetFEAuthService, MyResponse } from '../api-integration/api';

@Component({
  selector: 'app-endpoint',
  template: `
    <div class="container">
      <h2>Endpoint Form</h2>
      <form [formRoot]="endpointForm">
        <div class="form-group">
          <label for="firstName">First Name:</label>
          <input id="firstName" [formField]="endpointForm.firstName" />
        </div>
        <div class="form-group">
          <label for="lastName">Last Name:</label>
          <input id="lastName" [formField]="endpointForm.lastName" />
        </div>
        <div class="form-group">
          <label for="age">Age:</label>
          <input id="age" type="number" [formField]="endpointForm.age" />
        </div>
        <button type="submit">Submit</button>
      </form>
      <pre>{{ data() | json }}</pre>
    </div>
  `,
  styles: [
    `
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
    `,
  ],
  imports: [FormField, FormRoot, JsonPipe],
})
export class EndpointComponent {
  private readonly api = inject(DotnetFEAuthService);

  readonly data = signal<MyResponse | HttpErrorResponse | null>(null);

  readonly endpointForm = form(
    signal({ firstName: '', lastName: '', age: 0 }),
    {
      submission: {
        action: async (f) => {
          try {
            this.data.set(
              await firstValueFrom(this.api.createuser(f().value())),
            );
          } catch (error) {
            this.data.set(error as HttpErrorResponse);
          }
          return undefined;
        },
      },
    },
  );
}
