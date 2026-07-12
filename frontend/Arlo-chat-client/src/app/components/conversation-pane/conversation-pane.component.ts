import { DatePipe } from '@angular/common';
import {
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  SimpleChanges,
  ViewChild,
  inject,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { ChatApiService } from '../../services/chat-api.service';
import { ChatHubService } from '../../services/chat-hub.service';
import { ActiveChat } from '../../models/active-chat.model';
import { Conversation, Message } from '../../models/chat.model';

const SENDER_COLORS_LIGHT = ['#0e7c3f', '#b45309', '#1d4ed8', '#be185d', '#7c3aed', '#0f766e', '#b91c1c', '#4338ca'];
const SENDER_COLORS_DARK = ['#4ade80', '#fbbf24', '#60a5fa', '#f472b6', '#a78bfa', '#2dd4bf', '#f87171', '#818cf8'];

const MESSAGES_PAGE_SIZE = 30;
const LOAD_MORE_SCROLL_THRESHOLD_PX = 80;

@Component({
  selector: 'app-conversation-pane',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './conversation-pane.component.html',
  styleUrl: './conversation-pane.component.css'
})
export class ConversationPaneComponent implements OnChanges, OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly chatApi = inject(ChatApiService);
  private readonly chatHub = inject(ChatHubService);

  @Input({ required: true }) target!: ActiveChat;
  @Input({ required: true }) title!: string;

  @ViewChild('messagesEnd') private messagesEnd?: ElementRef<HTMLDivElement>;
  @ViewChild('contentBody') private contentBody?: ElementRef<HTMLDivElement>;

  private readonly subscriptions: Subscription[] = [];
  private resolved: ActiveChat | null = null;
  private loadingMoreMessages = false;

  readonly currentUser = this.authService.currentUser;
  readonly messages = signal<Message[]>([]);
  readonly loadingMessages = signal(false);
  readonly loadingOlderMessages = signal(false);
  readonly messagesHasMore = signal(false);
  readonly messageDraft = signal('');
  readonly sendError = signal<string | null>(null);

  readonly isGroup = signal(false);
  private readonly participantsById = signal<Map<number, string>>(new Map());

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['target']) {
      this.applyTarget(this.target);
    }
  }

  ngOnInit(): void {
    this.chatHub.connect().catch(err => console.error('Chat hub connection failed', err));

    this.subscriptions.push(
      this.chatHub.onMessageReceived.subscribe(({ message }) => this.handleIncomingMessage(message)),
      this.chatHub.onGroupMessageReceived.subscribe(({ message }) => this.handleIncomingMessage(message)),
      this.chatHub.onConversationCreated.subscribe(conversation => this.handleConversationEvent(conversation)),
      this.chatHub.onConversationUpdated.subscribe(conversation => this.handleConversationEvent(conversation))
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
  }

  isMine(message: Message): boolean {
    return message.ownerId === this.currentUser()?.id;
  }

  senderName(message: Message): string {
    if (this.isMine(message)) {
      return 'Me';
    }
    return this.participantsById().get(message.ownerId) ?? 'Unknown';
  }

  senderColor(userId: number): string {
    const isDark = document.body.classList.contains('theme-dark');
    const palette = isDark ? SENDER_COLORS_DARK : SENDER_COLORS_LIGHT;
    return palette[userId % palette.length];
  }

  onMessagesScroll(event: Event): void {
    const el = event.target as HTMLDivElement;
    if (el.scrollTop <= LOAD_MORE_SCROLL_THRESHOLD_PX) {
      this.loadOlderMessages();
    }
  }

  sendMessage(): void {
    const content = this.messageDraft().trim();
    const active = this.resolved;
    if (!content || !active) {
      return;
    }

    this.sendError.set(null);
    const payload = { content, type: 'Text' as const };

    const send = active.kind === 'conversation' && active.conversation.type === 'Group'
      ? this.chatHub.sendGroupMessage(active.conversation.id, payload)
      : this.chatHub.sendMessage(
          active.kind === 'conversation' ? this.otherParticipantId(active.conversation) : active.friend.id,
          payload
        );

    send
      .then(() => this.messageDraft.set(''))
      .catch((err: unknown) => this.sendError.set(err instanceof Error ? err.message : 'Could not send the message.'));
  }

  private applyTarget(target: ActiveChat): void {
    this.resolved = target;
    this.sendError.set(null);

    if (target.kind === 'conversation') {
      this.updateGroupState(target.conversation);
      this.loadMessages(target.conversation.id);
    } else {
      this.isGroup.set(false);
      this.messages.set([]);
      this.messagesHasMore.set(false);
      this.resolveExistingConversation(target.friend.id);
    }
  }

  private updateGroupState(conversation: Conversation): void {
    this.isGroup.set(conversation.type === 'Group');
    this.participantsById.set(new Map(conversation.participants.map(p => [p.userId, p.username])));
  }

  private resolveExistingConversation(friendId: number): void {
    this.chatApi.getConversations().subscribe(conversations => {
      if (this.resolved?.kind !== 'newChat' || this.resolved.friend.id !== friendId) {
        return;
      }

      const existing = conversations.find(
        c => c.type === 'Single' && (c.userIdLow === friendId || c.userIdHigh === friendId)
      );
      if (existing) {
        this.resolved = { kind: 'conversation', conversation: existing };
        this.loadMessages(existing.id);
      }
    });
  }

  private loadMessages(conversationId: number): void {
    this.loadingMessages.set(true);
    this.chatApi.getConversationMessages(conversationId, -1, MESSAGES_PAGE_SIZE).subscribe({
      next: page => {
        this.loadingMessages.set(false);
        this.messages.set([...page].reverse());
        this.messagesHasMore.set(page.length === MESSAGES_PAGE_SIZE);
        this.scrollToBottom();
      },
      error: () => this.loadingMessages.set(false)
    });
  }

  private loadOlderMessages(): void {
    const active = this.resolved;
    const currentMessages = this.messages();
    if (active?.kind !== 'conversation' || currentMessages.length === 0 || this.loadingMoreMessages || !this.messagesHasMore()) {
      return;
    }

    const oldestMessageId = currentMessages[0].id;
    this.loadingMoreMessages = true;
    this.loadingOlderMessages.set(true);

    const container = this.contentBody?.nativeElement;
    const previousScrollHeight = container?.scrollHeight ?? 0;

    this.chatApi.getConversationMessages(active.conversation.id, oldestMessageId, MESSAGES_PAGE_SIZE).subscribe({
      next: page => {
        this.loadingMoreMessages = false;
        this.loadingOlderMessages.set(false);
        this.messages.update(current => [...[...page].reverse(), ...current]);
        this.messagesHasMore.set(page.length === MESSAGES_PAGE_SIZE);

        setTimeout(() => {
          if (container) {
            container.scrollTop = container.scrollHeight - previousScrollHeight;
          }
        }, 0);
      },
      error: () => {
        this.loadingMoreMessages = false;
        this.loadingOlderMessages.set(false);
      }
    });
  }

  private handleIncomingMessage(message: Message): void {
    if (this.resolved?.kind === 'conversation' && this.resolved.conversation.id === message.conversationId) {
      this.messages.update(list => (list.some(m => m.id === message.id) ? list : [...list, message]));
      this.scrollToBottom();
    }
  }

  private handleConversationEvent(conversation: Conversation): void {
    if (this.resolved?.kind === 'conversation' && this.resolved.conversation.id === conversation.id) {
      this.resolved = { kind: 'conversation', conversation };
      this.updateGroupState(conversation);
      return;
    }

    if (this.resolved?.kind === 'newChat' && conversation.type === 'Single') {
      const me = this.currentUser()?.id;
      const matches =
        (conversation.userIdLow === me && conversation.userIdHigh === this.resolved.friend.id) ||
        (conversation.userIdHigh === me && conversation.userIdLow === this.resolved.friend.id);

      if (matches) {
        this.resolved = { kind: 'conversation', conversation };
        this.loadMessages(conversation.id);
      }
    }
  }

  private otherParticipantId(conversation: Conversation): number {
    const me = this.currentUser()?.id;
    return conversation.userIdLow === me ? conversation.userIdHigh! : conversation.userIdLow!;
  }

  private scrollToBottom(): void {
    setTimeout(() => this.messagesEnd?.nativeElement.scrollIntoView({ block: 'end' }), 0);
  }
}
