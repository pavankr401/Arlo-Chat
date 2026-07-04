import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
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

  register(request: RegisterRequest): Observable<ResponseModel> {
    return this.http.post<ResponseModel>('/api/auth/register', request, { withCredentials: true });
  }

  login(request: LoginRequest): Observable<User> {
    return this.http.post<User>('/api/auth/login', request, { withCredentials: true });
  }

  me(): Observable<User> {
    return this.http.get<User>('/api/auth/me', { withCredentials: true });
  }

  refresh(): Observable<User> {
    return this.http.post<User>('/api/auth/refresh', {}, { withCredentials: true });
  }

  logout(): Observable<ResponseModel> {
    return this.http.post<ResponseModel>('/api/auth/logout', {}, { withCredentials: true });
  }
}
