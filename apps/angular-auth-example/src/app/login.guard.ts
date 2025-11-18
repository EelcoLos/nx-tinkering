import { inject, Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Client, ValidateTokenRequest } from '../api-integration/api';
import { tap } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class LoginGuard implements CanActivate {
  private router = inject(Router);
  private apiService = inject(Client);

  canActivate(): boolean {
    const token = localStorage.getItem('token');
    if (!token) {
      this.router.navigate(['/login']);
      return false;
    }

    const request: ValidateTokenRequest = { token: token };
    // try validate request
    this.apiService
      .validatetoken(request)
      .pipe(
        tap((response) => {
          if (!response.isValid) {
            this.router.navigate(['/login']);
            return false;
          }
          return true;
        }),
      )
      .subscribe();

    return true; // Ensure a boolean is always returned
  }
}
