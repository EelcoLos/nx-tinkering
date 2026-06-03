import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Client, ValidateTokenRequest } from '../api-integration/api';
import { tap } from 'rxjs';

export const loginGuard: CanActivateFn = () => {
  const router = inject(Router);
  const apiService = inject(Client);

  const token = localStorage.getItem('token');
  if (!token) {
    router.navigate(['/login']);
    return false;
  }

  const request: ValidateTokenRequest = { token: token };
  // try validate request
  apiService
    .validatetoken(request)
    .pipe(
      tap((response) => {
        if (!response.isValid) {
          router.navigate(['/login']);
        }
      }),
    )
    .subscribe();

  return true; // Ensure a boolean is always returned
};
