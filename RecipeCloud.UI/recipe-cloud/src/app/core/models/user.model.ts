export interface User {
    id: string;
    username: string;
    email?: string;
    followersCount: number;
    followingCount: number;
    isBlocked: boolean;
    isAdmin?: boolean;
  }