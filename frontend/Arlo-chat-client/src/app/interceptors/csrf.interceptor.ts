import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { CsrfService } from '../services/csrf.service';

const MUTATING_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

export const csrfInterceptor: HttpInterceptorFn = (req, next) => {
  const csrf = inject(CsrfService);
  const token = csrf.getToken();

  if (!MUTATING_METHODS.has(req.method) || !token) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { 'X-CSRF-Token': token } }));
};
