import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { FriendApiService } from '../../services/friend-api.service';
import { ChatHubService } from '../../services/chat-hub.service';
import { FriendRequest, FriendUser } from '../../models/friend.model';
import { ResponseModel } from '../../models/response.model';
import { ActiveChat } from '../../models/active-chat.model';
import { ConversationPaneComponent } from '../conversation-pane/conversation-pane.component';
import { PopupComponent } from '../popup/popup.component';

type Tab = 'add' | 'requests' | 'friends';

const SEARCH_PAGE_SIZE = 20;
const FRIENDS_PAGE_SIZE = 20;

@Component({
  selector: 'app-manage-friends',
  standalone: true,
  imports: [FormsModule, ConversationPaneComponent, PopupComponent],
  templateUrl: './manage-friends.component.html',
  styleUrl: './manage-friends.component.css'
})
export class ManageFriendsComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly friendApi = inject(FriendApiService);
  private readonly chatHub = inject(ChatHubService);
  private readonly router = inject(Router);

  readonly currentUser = this.authService.currentUser;

  readonly activeTab = signal<Tab>('add');

  readonly searchQuery = signal('');
  readonly searchResults = signal<FriendUser[]>([]);
  readonly searchHasMore = signal(false);
  readonly searchMessage = signal<string | null>(null);
  readonly searchLoading = signal(false);
  private searchLoadingMore = false;

  readonly friendRequests = signal<FriendRequest[]>([]);
  readonly incomingRequests = computed(() =>
    this.friendRequests().filter(r => r.requesteeId === this.currentUser()?.id)
  );
  readonly outgoingRequests = computed(() =>
    this.friendRequests().filter(r => r.requesterId === this.currentUser()?.id)
  );

  readonly friends = signal<FriendUser[]>([]);
  readonly friendsHasMore = signal(false);
  readonly loadingMoreFriends = signal(false);

  readonly actionError = signal<string | null>(null);
  readonly actionSuccess = signal<string | null>(null);

  readonly messagePopupTarget = signal<ActiveChat | null>(null);
  private successTimeout?: ReturnType<typeof setTimeout>;
  private friendsChangedSub?: Subscription;

  ngOnInit(): void {
    this.loadFriendRequests();
    this.loadFriends();

    this.chatHub.connect().catch(err => console.error('Chat hub connection failed', err));
    this.friendsChangedSub = this.chatHub.onFriendsChanged.subscribe(() => {
      this.loadFriendRequests();
      this.loadFriends();
    });
  }

  ngOnDestroy(): void {
    clearTimeout(this.successTimeout);
    this.friendsChangedSub?.unsubscribe();
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
    this.actionError.set(null);

    if (tab === 'requests') {
      this.loadFriendRequests();
    } else if (tab === 'friends') {
      this.loadFriends();
    }
  }

  get messagePopupTitle(): string {
    const target = this.messagePopupTarget();
    return target?.kind === 'newChat' ? target.friend.username : '';
  }

  search(): void {
    const query = this.searchQuery().trim();
    if (!query) {
      return;
    }

    this.searchLoading.set(true);
    this.searchMessage.set(null);
    this.searchResults.set([]);
    this.searchHasMore.set(false);
    this.searchLoadingMore = false;

    this.friendApi.search(query, -1, SEARCH_PAGE_SIZE).subscribe({
      next: users => {
        this.searchLoading.set(false);
        this.searchResults.set(users);
        this.searchHasMore.set(users.length === SEARCH_PAGE_SIZE);
        if (users.length === 0) {
          this.searchMessage.set('No users found.');
        }
      },
      error: (err: HttpErrorResponse) => {
        this.searchLoading.set(false);
        const body = err.error as ResponseModel | null;
        this.searchMessage.set(body?.message ?? 'Search failed. Please try again.');
      }
    });
  }

  loadMoreSearchResults(): void {
    const query = this.searchQuery().trim();
    const results = this.searchResults();
    if (!query || results.length === 0 || this.searchLoadingMore) {
      return;
    }

    const lastRecentUserId = results[results.length - 1].id;
    this.searchLoadingMore = true;
    this.searchLoading.set(true);

    this.friendApi.search(query, lastRecentUserId, SEARCH_PAGE_SIZE).subscribe({
      next: users => {
        this.searchLoading.set(false);
        this.searchLoadingMore = false;
        this.searchResults.update(list => [...list, ...users]);
        this.searchHasMore.set(users.length === SEARCH_PAGE_SIZE);
      },
      error: (err: HttpErrorResponse) => {
        this.searchLoading.set(false);
        this.searchLoadingMore = false;
        const body = err.error as ResponseModel | null;
        this.searchMessage.set(body?.message ?? 'Could not load more results.');
      }
    });
  }

  addFriend(user: FriendUser): void {
    this.actionError.set(null);

    if (user.id === this.currentUser()?.id) {
      this.actionError.set("You can't add yourself as a friend.");
      return;
    }

    this.friendApi.addFriend(user.id).subscribe({
      next: () => {
        this.patchSearchResult(user.id, { friendshipStatus: 'RequestSent' });
        this.loadFriendRequests();
      },
      error: (err: HttpErrorResponse) => {
        const body = err.error as ResponseModel | null;
        this.actionError.set(body?.message ?? 'Could not send friend request.');
      }
    });
  }

  respondToRequest(request: FriendRequest, accept: boolean): void {
    this.actionError.set(null);
    this.friendApi.manageFriend({ targetUserId: request.requesterId, status: accept ? 'Accepted' : 'Rejected' }).subscribe({
      next: () => {
        this.friendRequests.update(list => list.filter(r => r.friendshipId !== request.friendshipId));
        if (accept) {
          this.loadFriends();
        }
      },
      error: (err: HttpErrorResponse) => {
        const body = err.error as ResponseModel | null;
        this.actionError.set(body?.message ?? 'Could not update the request.');
      }
    });
  }

  cancelRequest(request: FriendRequest): void {
    this.actionError.set(null);
    this.friendApi.manageFriend({ targetUserId: request.requesteeId, status: 'Cancelled' }).subscribe({
      next: () => this.friendRequests.update(list => list.filter(r => r.friendshipId !== request.friendshipId)),
      error: (err: HttpErrorResponse) => {
        const body = err.error as ResponseModel | null;
        this.actionError.set(body?.message ?? 'Could not cancel the request.');
      }
    });
  }

  removeFriend(friend: FriendUser): void {
    this.actionError.set(null);
    this.friendApi.manageFriend({ targetUserId: friend.id, status: 'Removed' }).subscribe({
      next: () => {
        this.friends.update(list => list.filter(f => f.id !== friend.id));

        this.patchSearchResult(friend.id, { friendshipStatus: 'None' });

        this.showSuccess(`${friend.username} was removed from your friend list.`);
      },
      error: (err: HttpErrorResponse) => {
        const body = err.error as ResponseModel | null;
        this.actionError.set(body?.message ?? 'Could not remove friend.');
      }
    });
  }

  messageFriend(friend: FriendUser): void {
    this.messagePopupTarget.set({ kind: 'newChat', friend });
  }

  closeMessagePopup(): void {
    this.messagePopupTarget.set(null);
  }

  goBack(): void {
    this.router.navigateByUrl('/home');
  }

  logout(): void {
    this.authService.logout().subscribe({
      complete: () => this.router.navigateByUrl('/login'),
      error: () => this.router.navigateByUrl('/login')
    });
  }

  private loadFriendRequests(): void {
    this.friendApi.fetchFriendRequests().subscribe({
      next: requests => this.friendRequests.set(requests)
    });
  }

  private loadFriends(): void {
    this.friendApi.fetchFriends(-1, FRIENDS_PAGE_SIZE).subscribe({
      next: friends => {
        this.friends.set(friends);
        this.friendsHasMore.set(friends.length === FRIENDS_PAGE_SIZE);
      }
    });
  }

  loadMoreFriends(): void {
    const list = this.friends();
    if (list.length === 0 || this.loadingMoreFriends()) {
      return;
    }

    const lastRecentUserId = list[list.length - 1].id;
    this.loadingMoreFriends.set(true);

    this.friendApi.fetchFriends(lastRecentUserId, FRIENDS_PAGE_SIZE).subscribe({
      next: more => {
        this.loadingMoreFriends.set(false);
        this.friends.update(current => [...current, ...more]);
        this.friendsHasMore.set(more.length === FRIENDS_PAGE_SIZE);
      },
      error: () => this.loadingMoreFriends.set(false)
    });
  }

  private patchSearchResult(userId: number, changes: Partial<FriendUser>): void {
    this.searchResults.update(list => list.map(u => (u.id === userId ? { ...u, ...changes } : u)));
  }

  private showSuccess(message: string): void {
    clearTimeout(this.successTimeout);
    this.actionSuccess.set(message);
    this.successTimeout = setTimeout(() => this.actionSuccess.set(null), 3000);
  }
}
