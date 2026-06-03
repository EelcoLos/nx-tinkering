import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Client, ValidateTokenRequest } from '../api-integration/api';
import { map } from 'rxjs';

export const loginGuard: CanActivateFn = () => {
  const router = inject(Router);
  const apiService = inject(Client);
  const loginUrl = router.createUrlTree(['/login']);

  const token = localStorage.getItem('token');
  if (!token) {
    return loginUrl;
  }

  const request: ValidateTokenRequest = { token: token };
  // validate token with the server before allowing navigation
  return apiService.validatetoken(request).pipe(
    map((response) => {
      if (!response.isValid) {
        return loginUrl;
      }
      return true;
    }),
  );
};
