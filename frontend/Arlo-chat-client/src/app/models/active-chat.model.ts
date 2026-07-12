import { Conversation } from './chat.model';
import { FriendUser } from './friend.model';

export type ActiveChat =
  | { kind: 'conversation'; conversation: Conversation }
  | { kind: 'newChat'; friend: FriendUser };
