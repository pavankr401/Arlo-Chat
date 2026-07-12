import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse } from '../models/auth-response.model';
import { LoginRequest, RegisterRequest } from '../models/auth-requests.model';
import { ResponseModel } from '../models/response.model';
import { User } from '../models/user.model';
import { CsrfService } from './csrf.service';

/**
 * Thin HTTP wrapper around the AuthController endpoints. Holds no user state itself -
 * AuthService owns the current-user state and calls through here. Login/me/refresh
 * responses also carry the CSRF token (see CsrfService for why), stashed here so
 * callers keep dealing with plain User values.
 */
@Injectable({ providedIn: 'root' })
export class UserApiService {
  private readonly http = inject(HttpClient);
  private readonly csrf = inject(CsrfService);
  private readonly baseUrl = environment.apiBaseUrl;

  register(request: RegisterRequest): Observable<ResponseModel> {
    return this.http.post<ResponseModel>(`${this.baseUrl}/api/auth/register`, request, { withCredentials: true });
  }

  login(request: LoginRequest): Observable<User> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/api/auth/login`, request, { withCredentials: true }).pipe(
      tap(res => this.csrf.setToken(res.csrfToken)),
      map(res => res.user)
    );
  }

  me(): Observable<User> {
    return this.http.get<AuthResponse>(`${this.baseUrl}/api/auth/me`, { withCredentials: true }).pipe(
      tap(res => this.csrf.setToken(res.csrfToken)),
      map(res => res.user)
    );
  }

  refresh(): Observable<User> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/api/auth/refresh`, {}, { withCredentials: true }).pipe(
      tap(res => this.csrf.setToken(res.csrfToken)),
      map(res => res.user)
    );
  }

  logout(): Observable<ResponseModel> {
    return this.http.post<ResponseModel>(`${this.baseUrl}/api/auth/logout`, {}, { withCredentials: true }).pipe(
      tap(() => this.csrf.clear())
    );
  }
}
