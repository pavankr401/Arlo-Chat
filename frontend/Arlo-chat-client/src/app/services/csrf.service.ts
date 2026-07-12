import { Injectable } from '@angular/core';

/**
 * Holds the CSRF token in memory. The backend's XSRF-TOKEN cookie lives on a
 * different origin than the frontend, so document.cookie can't read it - the
 * token is instead handed to us in the auth response body and stashed here.
 */
@Injectable({ providedIn: 'root' })
export class CsrfService {
  private token: string | null = null;

  setToken(token: string): void {
    this.token = token;
  }

  getToken(): string | null {
    return this.token;
  }

  clear(): void {
    this.token = null;
  }
}
