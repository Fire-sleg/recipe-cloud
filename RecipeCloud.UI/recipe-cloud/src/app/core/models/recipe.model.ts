import { BreadcrumbItem } from "./breadcrumb.model";

export interface Recipe {
    id: string;
    title: string;
    description: string;
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
    isPremium: boolean;
    transliteratedName: string;
    serving: number;
    viewCount: number;
    averageRating: number;
    isUserCreated: boolean;
    directions: string[];
    breadcrumbPath: BreadcrumbItem[] | null;
  }


