// ping.service.ts
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PingService {
  constructor(private http: HttpClient) {}
  ping() { return this.http.get('/api/ping'); }
}