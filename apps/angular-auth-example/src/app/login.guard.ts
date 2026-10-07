import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { DotnetFEAuthService } from '../api-integration/api';
import { catchError, map, of } from 'rxjs';

export const loginGuard: CanActivateFn = () => {
  const loginUrl = inject(Router).createUrlTree(['/login']);
  if (!localStorage.getItem('token')) {
    return loginUrl;
  }

  // validate the token with the server before allowing navigation
  return inject(DotnetFEAuthService)
    .validatetoken()
    .pipe(
      map(() => true),
      catchError(() => of(loginUrl)),
    );
};
