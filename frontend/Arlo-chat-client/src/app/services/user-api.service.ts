import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginRequest, RegisterRequest } from '../models/auth-requests.model';
import { ResponseModel } from '../models/response.model';
import { User } from '../models/user.model';

/**
 * Thin HTTP wrapper around the AuthController endpoints. Holds no state itself -
 * AuthService owns the current-user state and calls through here.
 */
@Injectable({ providedIn: 'root' })
export class UserApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  register(request: RegisterRequest): Observable<ResponseModel> {
    return this.http.post<ResponseModel>(`${this.baseUrl}/api/auth/register`, request, { withCredentials: true });
  }

  login(request: LoginRequest): Observable<User> {
    return this.http.post<User>(`${this.baseUrl}/api/auth/login`, request, { withCredentials: true });
  }

  me(): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/api/auth/me`, { withCredentials: true });
  }

  refresh(): Observable<User> {
    return this.http.post<User>(`${this.baseUrl}/api/auth/refresh`, {}, { withCredentials: true });
  }

  logout(): Observable<ResponseModel> {
    return this.http.post<ResponseModel>(`${this.baseUrl}/api/auth/logout`, {}, { withCredentials: true });
  }
}
