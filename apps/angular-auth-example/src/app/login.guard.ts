import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Client, ValidateTokenRequest, } from '../api-integration/api';
import { tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LoginGuard implements CanActivate {

  constructor(private router: Router, private apiService: Client) { }

  canActivate(): boolean {
    console.log('LoginGuard#canActivate called');
    const token = localStorage.getItem('token');
    console.log('Token validation request:', token);
    if (!token) {
      this.router.navigate(['/login']);
      return false;
    }

    const request: ValidateTokenRequest = { token: token };
    // try validate request
    this.apiService.validatetoken(request).pipe(
      tap((response) => {
        console.log('Token validation response:', response);
        if (!response.isValid) {
          this.router.navigate(['/login']);
          return false;
        }
        return true;
      })
    ).subscribe();

    return true; // Ensure a boolean is always returned
  }
}
