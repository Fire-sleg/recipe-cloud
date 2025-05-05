export interface Notification {
    id: string;
    userId: string;
    type: 'NewRecipe' | 'NewComment';
    message: string;
    relatedId: string;
    isRead: boolean;
    createdAt: string;
  }