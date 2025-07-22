
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Recipe } from '../../../core/models/recipe.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-user-recipes',
  templateUrl: './user-recipes.component.html',
  styleUrls: ['./user-recipes.component.css']
})
export class UserRecipesComponent {
  @Input() recipes: Recipe[] = [];
  @Output() recipeCreated = new EventEmitter<Recipe>();
  @Output() recipeUpdated = new EventEmitter<Recipe>();
  @Output() recipeDeleted = new EventEmitter<string>();

  constructor(private router: Router) {}

  createNewRecipe(): void {
    // Navigate to recipe creation page
    console.log('Creating new recipe...');
    // this.router.navigate(['/recipes/create']);
  }

  editRecipe(recipeId: string): void {
    console.log('Editing recipe:', recipeId);
    // this.router.navigate(['/recipes/edit', recipeId]);
  }

  deleteRecipe(recipeId: string): void {
    if (confirm('Ви впевнені, що хочете видалити цей рецепт?')) {
      this.recipeDeleted.emit(recipeId);
      console.log('Recipe deleted:', recipeId);
      // API call to delete recipe
    }
  }

  viewRecipe(recipeId: string): void {
    console.log('Viewing recipe:', recipeId);
    // this.router.navigate(['/recipes', recipeId]);
  }
}
