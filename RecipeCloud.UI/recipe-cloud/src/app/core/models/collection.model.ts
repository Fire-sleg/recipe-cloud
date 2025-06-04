import { Recipe } from "./recipe.model";

export interface Collection {
  id: string;
  title: string;
  recipes: Recipe[];
  createdBy: string;
  createdByUsername?: string;
  createdAt: Date;
  updatedAt: Date;
  totalCalories: number;
  totalProtein: number;
  totalFat: number;
  totalCarbohydrates: number;
}