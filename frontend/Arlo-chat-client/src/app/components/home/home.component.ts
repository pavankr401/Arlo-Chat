import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { FriendApiService } from '../../services/friend-api.service';
import { ChatApiService } from '../../services/chat-api.service';
import { ChatHubService } from '../../services/chat-hub.service';
import { Conversation } from '../../models/chat.model';
import { FriendUser } from '../../models/friend.model';
import { ConversationPaneComponent } from '../conversation-pane/conversation-pane.component';
import { PopupComponent } from '../popup/popup.component';

const CONVERSATIONS_PAGE_SIZE = 20;

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [FormsModule, ConversationPaneComponent, PopupComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly friendApi = inject(FriendApiService);
  private readonly chatApi = inject(ChatApiService);
  private readonly chatHub = inject(ChatHubService);
  private readonly router = inject(Router);

  readonly currentUser = this.authService.currentUser;

  readonly conversations = signal<Conversation[]>([]);
  readonly conversationsHasMore = signal(false);
  readonly loadingMoreConversations = signal(false);

  readonly friends = signal<FriendUser[]>([]);
  readonly friendsById = computed(() => new Map(this.friends().map(f => [f.id, f])));

  readonly activeConversation = signal<Conversation | null>(null);

  readonly searchTerm = signal('');
  readonly filteredConversations = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const all = this.conversations();
    if (!term) {
      return all;
    }
    return all.filter(c => this.displayNameFor(c).toLowerCase().includes(term));
  });

  @ViewChild('settingsRef') private settingsRef?: ElementRef<HTMLElement>;

  readonly themeMenuOpen = signal(false);

  readonly addGroupOpen = signal(false);
  readonly groupName = signal('');
  readonly selectedFriendIds = signal<ReadonlySet<number>>(new Set());
  readonly creatingGroup = signal(false);
  readonly createGroupError = signal<string | null>(null);

  ngOnInit(): void {
    const savedTheme = localStorage.getItem('arlo-theme');
    if (savedTheme === 'dark') {
      document.body.classList.add('theme-dark');
    }

    this.chatHub.connect().catch(err => console.error('Chat hub connection failed', err));

    this.chatHub.onConversationCreated.subscribe(conversation => this.upsertConversation(conversation));
    this.chatHub.onConversationUpdated.subscribe(conversation => this.upsertConversation(conversation));

    forkJoin({
      friends: this.friendApi.fetchFriends(),
      conversations: this.chatApi.getConversations()
    }).subscribe(({ friends, conversations }) => {
      this.friends.set(friends);
      this.conversations.set(conversations);
      this.conversationsHasMore.set(conversations.length === CONVERSATIONS_PAGE_SIZE);
    });
  }

  ngOnDestroy(): void {
    this.chatHub.disconnect().catch(() => {});
  }

  loadMoreConversations(): void {
    const list = this.conversations();
    if (list.length === 0 || this.loadingMoreConversations()) {
      return;
    }

    const lastConversationId = list[list.length - 1].id;
    this.loadingMoreConversations.set(true);

    this.chatApi.getConversations(lastConversationId, CONVERSATIONS_PAGE_SIZE).subscribe({
      next: more => {
        this.loadingMoreConversations.set(false);
        this.conversations.update(current => [...current, ...more]);
        this.conversationsHasMore.set(more.length === CONVERSATIONS_PAGE_SIZE);
      },
      error: () => this.loadingMoreConversations.set(false)
    });
  }

  selectConversation(conversation: Conversation): void {
    this.activeConversation.set(conversation);
  }

  displayNameFor(conversation: Conversation): string {
    if (conversation.type === 'Group') {
      return conversation.name ?? 'Group';
    }
    const otherId = this.otherParticipantId(conversation);
    return this.friendsById().get(otherId)?.username ?? `User #${otherId}`;
  }

  latestMessagePreview(conversation: Conversation): string {
    return conversation.latestMessage?.content ?? 'No messages yet';
  }

  isActiveConversation(conversation: Conversation): boolean {
    return this.activeConversation()?.id === conversation.id;
  }

  toggleThemeMenu(): void {
    this.themeMenuOpen.update(v => !v);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.themeMenuOpen()) {
      return;
    }
    const target = event.target as Node;
    if (!this.settingsRef?.nativeElement.contains(target)) {
      this.themeMenuOpen.set(false);
    }
  }

  setTheme(theme: 'light' | 'dark'): void {
    document.body.classList.toggle('theme-dark', theme === 'dark');
    localStorage.setItem('arlo-theme', theme);
    this.themeMenuOpen.set(false);
  }

  goToManageFriends(): void {
    this.router.navigateByUrl('/manage-friends');
  }

  openAddGroup(): void {
    this.groupName.set('');
    this.selectedFriendIds.set(new Set());
    this.createGroupError.set(null);
    this.addGroupOpen.set(true);
  }

  closeAddGroup(): void {
    this.addGroupOpen.set(false);
  }

  isFriendSelected(friendId: number): boolean {
    return this.selectedFriendIds().has(friendId);
  }

  toggleFriendSelection(friendId: number): void {
    this.selectedFriendIds.update(current => {
      const next = new Set(current);
      if (next.has(friendId)) {
        next.delete(friendId);
      } else {
        next.add(friendId);
      }
      return next;
    });
  }

  createGroup(): void {
    const name = this.groupName().trim();
    const participantIds = Array.from(this.selectedFriendIds());

    if (!name) {
      this.createGroupError.set('Group name is required.');
      return;
    }
    if (participantIds.length < 2) {
      this.createGroupError.set('Select at least 2 friends - a group needs 3 members including you.');
      return;
    }
    const alreadyInGroup = this.conversations().some(
      c => c.type === 'Group' && c.name?.toLowerCase() === name.toLowerCase()
    );
    if (alreadyInGroup) {
      this.createGroupError.set("You're already in a group with that name.");
      return;
    }

    this.creatingGroup.set(true);
    this.createGroupError.set(null);

    this.chatHub.createGroupConversation(name, participantIds)
      .then(() => {
        this.creatingGroup.set(false);
        this.addGroupOpen.set(false);
      })
      .catch((err: unknown) => {
        this.creatingGroup.set(false);
        this.createGroupError.set(err instanceof Error ? err.message : 'Could not create the group.');
      });
  }

  logout(): void {
    this.authService.logout().subscribe({
      complete: () => this.router.navigateByUrl('/login'),
      error: () => this.router.navigateByUrl('/login')
    });
  }

  private otherParticipantId(conversation: Conversation): number {
    const me = this.currentUser()?.id;
    return conversation.userIdLow === me ? conversation.userIdHigh! : conversation.userIdLow!;
  }

  private upsertConversation(conversation: Conversation): void {
    this.conversations.update(list => [conversation, ...list.filter(c => c.id !== conversation.id)]);

    if (this.activeConversation()?.id === conversation.id) {
      this.activeConversation.set(conversation);
    }
  }
}
