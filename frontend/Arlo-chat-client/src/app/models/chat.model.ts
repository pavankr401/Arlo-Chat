export type ConversationType = 'Single' | 'Group';
export type MessageType = 'Text' | 'Audio' | 'Video' | 'Document';
export type ParticipantRole = 'Admin' | 'Member';

export interface Message {
  id: number;
  conversationId: number;
  ownerId: number;
  targetId: number;
  content: string;
  type: MessageType;
  format: string | null;
  createdBy: number;
  createdDate: string;
  modifiedBy: number | null;
  modifiedDate: string | null;
}

export interface ConversationParticipant {
  id: number;
  userId: number;
  username: string;
  conversationId: number;
  joinedAt: string;
  leftAt: string | null;
  role: ParticipantRole;
}

export interface Conversation {
  id: number;
  createdBy: number;
  createdDate: string;
  modifiedBy: number | null;
  modifiedDate: string | null;
  type: ConversationType;
  latestMessageId: number | null;
  latestMessage: Message | null;
  userIdLow: number | null;
  userIdHigh: number | null;
  name: string | null;
  participants: ConversationParticipant[];
}
