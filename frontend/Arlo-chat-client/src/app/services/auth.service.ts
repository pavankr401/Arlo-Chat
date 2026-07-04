import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, map, of, shareReplay, tap, throwError } from 'rxjs';
import { LoginRequest, RegisterRequest } from '../models/auth-requests.model';
import { ResponseModel } from '../models/response.model';
import { User } from '../models/user.model';
import { UserApiService } from './user-api.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userApi = inject(UserApiService);
  private readonly router = inject(Router);

  private readonly _currentUser = signal<User | null>(null);
  readonly currentUser = this._currentUser.asReadonly();

  private refreshInFlight$: Observable<User | null> | null = null;

  /** Called once from APP_INITIALIZER, before routes render. Never throws. */
  bootstrap(): Observable<void> {
    return this.userApi.me().pipe(
      tap(user => this._currentUser.set(user)),
      catchError(() => {
        this._currentUser.set(null);
        return of(null);
      }),
      map(() => void 0)
    );
  }

  login(request: LoginRequest): Observable<User> {
    return this.userApi.login(request).pipe(
      tap(user => this._currentUser.set(user))
    );
  }

  register(request: RegisterRequest): Observable<ResponseModel> {
    return this.userApi.register(request);
  }

  logout(): Observable<ResponseModel> {
    return this.userApi.logout().pipe(
      tap(() => this._currentUser.set(null)),
      catchError(err => {
        this._currentUser.set(null);
        return throwError(() => err);
      })
    );
  }

  /**
   * Shared by the auth-error interceptor so concurrent 401s trigger exactly one
   * refresh call instead of one each (item 20).
   */
  refreshSession(): Observable<User | null> {
    if (!this.refreshInFlight$) {
      this.refreshInFlight$ = this.userApi.refresh().pipe(
        tap(user => this._currentUser.set(user)),
        catchError(() => of(null)),
        finalize(() => { this.refreshInFlight$ = null; }),
        shareReplay(1)
      );
    }
    return this.refreshInFlight$;
  }

  handleSessionExpired(): void {
    this._currentUser.set(null);
    this.router.navigate(['/login']);
  }
}
