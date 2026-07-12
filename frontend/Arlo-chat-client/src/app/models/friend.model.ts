export type UiFriendRequestStatus =
  | 'None'
  | 'RequestReceived'
  | 'RequestSent'
  | 'Accepted'
  | 'Rejected'
  | 'Removed'
  | 'Cancelled';

export type FriendRequestStatus = 'Pending' | 'Accepted' | 'Rejected' | 'Removed' | 'Cancelled';

export interface FriendUser {
  id: number;
  username: string;
  email: string;
  friendshipStatus: UiFriendRequestStatus;
  lastActiveAt: string;
}

export interface FriendRequest {
  friendshipId: number;
  requesterId: number;
  requesteeId: number;
  username: string;
  email: string;
  createdDate: string;
}

export interface ManageFriendRequest {
  targetUserId: number;
  status: FriendRequestStatus;
}
