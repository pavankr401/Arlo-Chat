import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/** Marks a request as already-retried-once, so a repeat 401 doesn't loop back into another refresh. */
const SKIP_AUTH_RETRY = new HttpContextToken<boolean>(() => false);

// Login/Register/Me are handled locally by their own callers - there was never a session to expire.
// Refresh is excluded too: its own 401 is handled locally by AuthService.refreshSession(), and
// letting it through here would recurse (refresh failing would trigger another refresh attempt).
const EXCLUDED_FROM_GLOBAL_HANDLING: readonly string[] = [
  '/api/auth/login',
  '/api/auth/register',
  '/api/auth/me',
  '/api/auth/refresh',
];

export const authErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  if (EXCLUDED_FROM_GLOBAL_HANDLING.some(url => req.url.includes(url))) {
    return next(req);
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      if (req.context.get(SKIP_AUTH_RETRY)) {
        authService.handleSessionExpired();
        return throwError(() => error);
      }

      return authService.refreshSession().pipe(
        switchMap(user => {
          if (!user) {
            authService.handleSessionExpired();
            return throwError(() => error);
          }

          return next(req.clone({ context: req.context.set(SKIP_AUTH_RETRY, true) }));
        })
      );
    })
  );
};
