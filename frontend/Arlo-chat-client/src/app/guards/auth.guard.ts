import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** Allows navigation only when a user is already cached in AuthService (set at bootstrap or login). */
export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.currentUser() ? true : router.createUrlTree(['/login']);
};
