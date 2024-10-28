import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LoginGuard implements CanActivate {

  constructor(private router: Router) {}

  canActivate(): Observable<boolean> | Promise<boolean> | boolean {
    const isAuthenticated = this.checkAuthentication();
    if (!isAuthenticated) {
      this.router.navigate(['/login']);
    }
    return isAuthenticated;
  }

  private checkAuthentication(): boolean {
    // Implement your authentication check logic here
    // For example, check if a valid JWT token exists in local storage
    const token = localStorage.getItem('authToken');
    return !!token;
  }
}
