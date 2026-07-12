import { User } from './user.model';

export interface AuthResponse {
  user: User;
  csrfToken: string;
}
