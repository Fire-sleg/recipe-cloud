export interface Recipe {
    id: string;
    title: string;
    description?: string;
    ingredients: string[];
    cookingTime: number;
    difficulty: 'easy' | 'medium' | 'hard';
    imageUrl?: string;
    createdBy: string;
    createdByUsername: string;
    createdAt: string;
    updatedAt: string;
    diets: string[];
    allergens: string[];
    cuisine?: string;
    tags: string[];
    calories: number;
    protein: number;
    fat: number;
    carbohydrates: number;
  }