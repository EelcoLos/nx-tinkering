import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token');
  if (token && req.url.startsWith('/api/')) {
    return next(
      req.clone({
        headers: req.headers.set('Authorization', `Bearer ${token}`),
      }),
    );
  }
  return next(req);
};
