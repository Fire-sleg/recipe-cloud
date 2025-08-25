// import { Component, OnInit } from '@angular/core';
// import { Recipe } from '../../../core/models/recipe.model';
// import { Router } from '@angular/router';
// import { RecipeService } from '../../../core/services/recipe.service';
// import { AuthService } from '../../../core/services/auth.service';
// import { catchError, Observable, of } from 'rxjs';

// @Component({
//   selector: 'app-user-recipes',
//   templateUrl: './user-recipes.component.html',
//   styleUrls: ['./user-recipes.component.css'],
// })
// export class UserRecipesComponent implements OnInit {
//   recipes$!: Observable<Recipe[]>;
//   userId: string | undefined;
//   showEditForm: boolean = false;
//   selectedRecipe: Recipe | null = null;

//   constructor(
//     private router: Router,
//     private recipeService: RecipeService,
//     private authService: AuthService
//   ) {}

//   ngOnInit(): void {
//     this.userId = this.authService.getCurrentUserId();
//     if (this.userId) {
//       this.recipes$ = this.recipeService.recipesByUser$;
//       this.recipeService
//         .getByUserId(this.userId)
//         .pipe(
//           catchError((error) => {
//             console.error('Error loading recipes:', error);
//             return of([]);
//           })
//         )
//         .subscribe();
//     }
//   }

//   createNewRecipe(): void {
//     this.router.navigate(['/recipes/create']);
//   }

//   editRecipe(recipe: Recipe): void {
//     if (!this.authService.isAuthenticated()) {
//       alert('Для редагування рецепту потрібно увійти в систему');
//       return;
//     }
//     this.selectedRecipe = recipe;
//     this.showEditForm = true;
//   }

//   viewRecipe(transliteratedName: string): void {
//     this.router.navigate(['recipes', 'detail', transliteratedName]);
//   }

//   onDeleteRecipe(recipeId: string): void {
//     if (this.userId && confirm('Ви впевнені, що хочете видалити цей рецепт?')) {
//       this.recipeService.deleteRecipe(recipeId).pipe(
//         catchError(error => {
//           console.error('Error deleting recipe:', error);
//           alert('Помилка при видаленні рецепту. Спробуйте ще раз.');
//           return of(null);
//         })
//       ).subscribe(() => {
//         alert('Рецепт успішно видалено!');
//       });
//     }
//   }

//   onRecipeUpdated(updatedRecipe: Recipe): void {
//     this.showEditForm = false;
//     this.selectedRecipe = null;
//     // Refresh the recipe list
//     if (this.userId) {
//       this.recipeService.getByUserId(this.userId).subscribe();
//     }
//     alert('Рецепт успішно оновлено!');
//     // Optionally navigate to the updated recipe's detail page
//     if (updatedRecipe.transliteratedName) {
//       this.router.navigate(['recipes', 'detail', updatedRecipe.transliteratedName]);
//     }
//   }

//   onFormClosed(): void {
//     this.showEditForm = false;
//     this.selectedRecipe = null;
//   }

//   trackByRecipeId(index: number, recipe: Recipe): string {
//     return recipe.id ?? '';
//   }
// }


// UserRecipesComponent TypeScript (add ViewChild, modify createNewRecipe, remove showEditForm and related logic)
import { Component, OnInit, ViewChild } from '@angular/core';
import { Recipe } from '../../../core/models/recipe.model';
import { Router } from '@angular/router';
import { RecipeService } from '../../../core/services/recipe.service';
import { AuthService } from '../../../core/services/auth.service';
import { catchError, Observable, of } from 'rxjs';
import { RecipeFormComponent } from '../../recipes/recipe-form/recipe-form.component';

@Component({
  selector: 'app-user-recipes',
  templateUrl: './user-recipes.component.html',
  styleUrls: ['./user-recipes.component.css'],
})
export class UserRecipesComponent implements OnInit {
  recipes$!: Observable<Recipe[]>;
  userId: string | undefined;
  selectedRecipe: Recipe | null = null;

  @ViewChild(RecipeFormComponent) recipeFormComponent!: RecipeFormComponent;

  constructor(
    private router: Router,
    private recipeService: RecipeService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.userId = this.authService.getCurrentUserId();
    if (this.userId) {
      this.recipes$ = this.recipeService.recipesByUser$;
      this.recipeService
        .getByUserId(this.userId)
        .pipe(
          catchError((error) => {
            console.error('Error loading recipes:', error);
            return of([]);
          })
        )
        .subscribe();
    }
  }

  createNewRecipe(): void {
    this.recipeFormComponent.showCreateForm();
  }

  editRecipe(recipe: Recipe): void {
    if (!this.authService.isAuthenticated()) {
      alert('Для редагування рецепту потрібно увійти в систему');
      return;
    }
    this.selectedRecipe = recipe;
  }

  viewRecipe(transliteratedName: string): void {
    this.router.navigate(['recipes', 'detail', transliteratedName]);
  }

  onDeleteRecipe(recipeId: string): void {
    if (this.userId && confirm('Ви впевнені, що хочете видалити цей рецепт?')) {
      this.recipeService.deleteRecipe(recipeId).pipe(
        catchError(error => {
          console.error('Error deleting recipe:', error);
          alert('Помилка при видаленні рецепту. Спробуйте ще раз.');
          return of(null);
        })
      ).subscribe(() => {
        alert('Рецепт успішно видалено!');
      });
    }
  }

  onRecipeUpdated(updatedRecipe: Recipe): void {
    this.selectedRecipe = null;
    // Refresh the recipe list
    if (this.userId) {
      this.recipeService.getByUserId(this.userId).subscribe();
    }
    alert('Рецепт успішно оновлено!');
    // Optionally navigate to the updated recipe's detail page
    if (updatedRecipe.transliteratedName) {
      this.router.navigate(['recipes', 'detail', updatedRecipe.transliteratedName]);
    }
  }

  onFormClosed(): void {
    this.selectedRecipe = null;
  }

  trackByRecipeId(index: number, recipe: Recipe): string {
    return recipe.id ?? '';
  }
}