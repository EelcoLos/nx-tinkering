import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import {
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideZoneChangeDetection } from '@angular/core';
import { authInterceptor } from './app/auth.interceptor';

console.log('Starting bootstrap process...');

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideZoneChangeDetection({ eventCoalescing: true }),
  ],
})
  .then(() => {
    console.log('Bootstrap process completed successfully.');
  })
  .catch((err) => {
    console.error('Bootstrap process failed:', err);
  });
