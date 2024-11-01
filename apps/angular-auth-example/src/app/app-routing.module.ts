import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './login.component';
import { EndpointComponent } from './endpoint.component';
import { LoginGuard } from './login.guard';

export const routes: Routes = [
	{ path: 'login', component: LoginComponent },
	{ path: 'endpoint', component: EndpointComponent, canActivate: [LoginGuard] },
	{ path: '**', redirectTo: 'endpoint' }
];

@NgModule({
	imports: [RouterModule.forRoot(routes)],
	exports: [RouterModule]
})
export class AppRoutingModule { }
