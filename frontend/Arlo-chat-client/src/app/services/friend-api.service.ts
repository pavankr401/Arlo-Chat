import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { FriendRequest, FriendUser, ManageFriendRequest } from '../models/friend.model';
import { ResponseModel } from '../models/response.model';

@Injectable({ providedIn: 'root' })
export class FriendApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  search(searchQuery: string, lastRecentUserId = -1, pageSize = 20): Observable<FriendUser[]> {
    return this.http.get<FriendUser[]>(`${this.baseUrl}/api/user/search`, {
      params: { searchQuery, lastRecentUserId, pageSize },
      withCredentials: true
    });
  }

  addFriend(targetUserId: number): Observable<ResponseModel> {
    return this.http.post<ResponseModel>(`${this.baseUrl}/api/user/friends/${targetUserId}`, {}, { withCredentials: true });
  }

  manageFriend(request: ManageFriendRequest): Observable<ResponseModel> {
    return this.http.post<ResponseModel>(`${this.baseUrl}/api/user/friends/manage`, request, { withCredentials: true });
  }

  fetchFriends(lastRecentUserId = -1, pageSize = 20): Observable<FriendUser[]> {
    return this.http.get<FriendUser[]>(`${this.baseUrl}/api/user/friends`, {
      params: { lastRecentUserId, pageSize },
      withCredentials: true
    });
  }

  fetchFriendRequests(lastRecentFriendshipId = -1, pageSize = 20): Observable<FriendRequest[]> {
    return this.http.get<FriendRequest[]>(`${this.baseUrl}/api/user/friends/requests`, {
      params: { lastRecentFriendshipId, pageSize },
      withCredentials: true
    });
  }
}
