export interface UserProfile {
    id: string;
    userId: string;
    preferredDiets: string[];
    preferredCuisines: string[];
    allergens: string[];
    enableSubscriptionNotifications: boolean;
    updatedAt: string;
  }