import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { Conversation, Message } from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class ChatHubService {
  private connection: signalR.HubConnection | null = null;
  private connectPromise: Promise<void> | null = null;

  private readonly messageReceived$ = new Subject<{ senderId: number; message: Message }>();
  private readonly conversationCreated$ = new Subject<Conversation>();
  private readonly conversationUpdated$ = new Subject<Conversation>();
  private readonly groupMessageReceived$ = new Subject<{ conversationId: number; message: Message }>();
  private readonly friendsChanged$ = new Subject<void>();

  readonly onMessageReceived: Observable<{ senderId: number; message: Message }> = this.messageReceived$.asObservable();
  readonly onConversationCreated: Observable<Conversation> = this.conversationCreated$.asObservable();
  readonly onConversationUpdated: Observable<Conversation> = this.conversationUpdated$.asObservable();
  readonly onGroupMessageReceived: Observable<{ conversationId: number; message: Message }> = this.groupMessageReceived$.asObservable();
  readonly onFriendsChanged: Observable<void> = this.friendsChanged$.asObservable();

  connect(): Promise<void> {
    if (this.connection) {
      return Promise.resolve();
    }
    if (!this.connectPromise) {
      this.connectPromise = this.startConnection().catch(err => {
        this.connectPromise = null;
        throw err;
      });
    }
    return this.connectPromise;
  }

  async disconnect(): Promise<void> {
    await this.connection?.stop();
    this.connection = null;
    this.connectPromise = null;
  }

  async sendMessage(targetId: number, message: Partial<Message>): Promise<void> {
    await this.connect();
    return this.connection!.invoke('SendMessage', targetId, message);
  }

  async createGroupConversation(name: string, participantUserIds: number[]): Promise<void> {
    await this.connect();
    return this.connection!.invoke('CreateGroupConversation', name, participantUserIds);
  }

  async sendGroupMessage(conversationId: number, message: Partial<Message>): Promise<void> {
    await this.connect();
    return this.connection!.invoke('SendGroupMessage', conversationId, message);
  }

  private async startConnection(): Promise<void> {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/hubs/chat`, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveMessage', (senderId: number, message: Message) =>
      this.messageReceived$.next({ senderId, message }));
    connection.on('ConversationCreated', (conversation: Conversation) =>
      this.conversationCreated$.next(conversation));
    connection.on('ConversationUpdated', (conversation: Conversation) =>
      this.conversationUpdated$.next(conversation));
    connection.on('ReceiveGroupMessage', (conversationId: number, message: Message) =>
      this.groupMessageReceived$.next({ conversationId, message }));
    connection.on('FriendsChanged', () => this.friendsChanged$.next());

    await connection.start();
    this.connection = connection;
  }
}
