import { Routes } from '@angular/router';
import { LoginComponent } from './login.component';
import { EndpointComponent } from './endpoint.component';
import { loginGuard } from './login.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'endpoint', component: EndpointComponent, canActivate: [loginGuard] },
  { path: '**', redirectTo: 'endpoint' },
];
