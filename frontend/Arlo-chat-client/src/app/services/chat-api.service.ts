import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Conversation, Message } from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class ChatApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getConversations(lastRecentConversationId = -1, pageSize = 20): Observable<Conversation[]> {
    return this.http.get<Conversation[]>(`${this.baseUrl}/api/chat/conversations`, {
      params: { lastRecentConversationId, pageSize },
      withCredentials: true
    });
  }

  getConversationMessages(conversationId: number, lastRecentMessageId = -1, pageSize = 30): Observable<Message[]> {
    return this.http.get<Message[]>(`${this.baseUrl}/api/chat/conversations/${conversationId}/messages`, {
      params: { lastRecentMessageId, pageSize },
      withCredentials: true
    });
  }
}
