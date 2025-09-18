import { Component, Input, OnInit, DestroyRef, inject } from '@angular/core';
import { Recipe } from '../../../core/models/recipe.model';
import { ActivatedRoute, Router } from '@angular/router';
import { CategoryService } from '../../../core/services/category.service';
import { RecipeService } from '../../../core/services/recipe.service';
import { AuthService } from '../../../core/services/auth.service';
import { CollectionService } from '../../../core/services/collection.service';
import { User } from '../../../core/models/user.model';
import { Collection } from '../../../core/models/collection.model';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-recipe-detail',
  templateUrl: './recipe-detail.component.html',
  styleUrls: ['./recipe-detail.component.css']
})
export class RecipeDetailComponent implements OnInit {
  @Input() recipe: Recipe | undefined;
  
  // Rating properties
  userRating: number = 0;
  hoverRating: number = 0;
  isRatingSubmitted: boolean = false;
  isAuthenticated: boolean = false;
  ratings: number[] = [1, 2, 3, 4, 5];
  currentUser: User | null = null;

  // Edit mode
  showEditForm: boolean = false;
  categoryTransliteratedName: string | undefined;

  // Collection properties
  showCollectionModal: boolean = false;
  collections: Collection[] = [];
  selectedCollectionId: string | null = null;
  newCollectionName: string = '';
  newCollectionError: string = '';
  successMessage: string | null = null;
  errorMessage: string | null = null;
  transliteratedName: string = '';

  // Destroy ref for subscription cleanup
  private destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private route: ActivatedRoute,
    private recipeService: RecipeService,
    private authService: AuthService,
    private router: Router,
    private categoryService: CategoryService,
    private collectionService: CollectionService
  ) {}

  ngOnInit(): void {
    this.isAuthenticated = this.authService.isAuthenticated();
    const userJson = localStorage.getItem("user");
    this.currentUser = userJson ? JSON.parse(userJson) : null;

    // Subscribe to collection service messages
    this.collectionService.successMessage$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(message => {
        this.successMessage = message;
        if (message) {
          setTimeout(() => {
            this.collectionService.successMessage$.next(null);
            this.successMessage = null;
          }, 3000); // Clear message after 3 seconds
        }
      });

    this.collectionService.errorMessage$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(message => {
        this.errorMessage = message;
        if (message) {
          setTimeout(() => {
            this.collectionService.errorMessage$.next(null);
            this.errorMessage = null;
          }, 3000); // Clear message after 3 seconds
        }
      });

    // Subscribe to collections
    this.collectionService.collectionsByUser$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(collections => {
        this.collections = collections;
      });

    this.route.paramMap.subscribe(params => {
      this.transliteratedName = params.get('transliteratedName') || '';
      if (this.transliteratedName) {
        this.recipeService.getByTransliteratedName(this.transliteratedName).subscribe(apiresponse => {
          debugger;
          this.recipe = apiresponse.result;
          this.incrementViewCount();
          this.getCurrentUserRating();
          this.sendViewHistory();
          if (this.recipe?.categoryId) {
            this.categoryService.getCategoryById(this.recipe.categoryId).subscribe((category) => {
              this.categoryTransliteratedName = category.transliteratedName;
            });
          }
        });
      }
    });
  }

  canEditRecipe(): boolean {
    if (!this.recipe || !this.currentUser) {
      return false;
    }
    if (this.currentUser.role === 'Admin') {
      return true;
    }
    return this.recipe.createdBy === this.currentUser.id;
  }

  onEditRecipe(): void {
    if (!this.recipe || !this.canEditRecipe()) {
      return;
    }
    this.showEditForm = true;
  }

  onRecipeUpdated(updatedRecipe: Recipe): void {
    this.showEditForm = false;
    if (updatedRecipe.transliteratedName) {
      this.router.navigate(['recipes', 'detail', updatedRecipe.transliteratedName]);
    } else {
      this.recipeService.getRecipeById(updatedRecipe.id).subscribe({
        next: (apiresponse) => {
          this.recipe = apiresponse.result;
          if (apiresponse.result.transliteratedName) {
            this.router.navigate(['recipes', 'detail', apiresponse.result.transliteratedName]);
          }
        },
        error: (error) => {
          console.error('Error fetching updated recipe:', error);
        }
      });
    }
  }

  onFormClosed(): void {
    this.showEditForm = false;
  }

  onDeleteRecipe(): void {
    if (!this.recipe || !this.canEditRecipe()) {
      return;
    }
    if (confirm('Ви впевнені, що хочете видалити цей рецепт?')) {
      this.recipeService.deleteRecipe(this.recipe.id).subscribe({
        next: () => {
          console.log("Successfully deleted");
          this.router.navigate(['recipes', this.categoryTransliteratedName]);
        },
        error: (error) => {
          console.error('Error deleting recipe:', error);
        }
      });
    }
  }

  onAddToCollection(): void {
    if (!this.isAuthenticated) {
      this.collectionService.errorMessage$.next('Будь ласка, увійдіть в систему, щоб додати рецепт до колекції');
      return;
    }
    if (!this.recipe?.id) {
      return;
    }

    // Fetch user collections if not already loaded
    if (this.collections.length === 0) {
      const userId = this.authService.getCurrentUserId();
      if (userId) {
        this.collectionService.getByUserId(userId).subscribe();
      }
    }
    this.showCollectionModal = true;
    this.selectedCollectionId = null;
    this.newCollectionName = '';
    this.newCollectionError = '';
  }

  closeCollectionModal(): void {
    this.showCollectionModal = false;
    this.selectedCollectionId = null;
    this.newCollectionName = '';
    this.newCollectionError = '';
  }

  onNewCollectionNameChange(): void {
    this.newCollectionError = '';
    if (this.newCollectionName.trim()) {
      this.selectedCollectionId = null;
    }
  }

  confirmAddToCollection(): void {
    if (!this.recipe?.id) {
      return;
    }

    if (this.newCollectionName.trim()) {
      // Prepare FormData for new collection
      const formData = new FormData();
      formData.append('title', this.newCollectionName.trim());
      if (this.currentUser?.username) {
        formData.append('createdByUsername', this.currentUser.username);
      }
      formData.append('recipeIds[0]', this.recipe.id);

      this.collectionService.createCollection(formData).subscribe({
        next: () => {
          // Success message handled by collectionService.successMessage$
          this.collectionService.getByUserId(this.authService.getCurrentUserId()!).subscribe();
          this.closeCollectionModal();
        },
        error: (error) => {
          console.error('Error creating collection:', error);
          this.newCollectionError = 'Помилка при створенні колекції. Спробуйте ще раз.';
        }
      });
    } else if (this.selectedCollectionId) {
      // Add to existing collection
      this.collectionService.addRecipeToCollection(this.selectedCollectionId, this.recipe.id).subscribe({
        next: () => {
          // Success message handled by collectionService.successMessage$
          this.closeCollectionModal();
        },
        error: (error) => {
          console.error('Error adding recipe to collection:', error);
          this.newCollectionError = 'Помилка при додаванні рецепту до колекції. Спробуйте ще раз.';
        }
      });
    } else {
      this.newCollectionError = 'Будь ласка, оберіть колекцію або введіть назву нової.';
    }
  }

  private getCurrentUserRating() {
    if (this.recipe?.id && this.isAuthenticated) {
      this.recipeService.getRecipeRating(this.recipe.id).subscribe({
        next: (rating) => {
          this.userRating = rating.rating;
        },
        error: (error) => {
          console.error('Failed to get user rating:', error);
        }
      });
    }
  }

  private incrementViewCount(): void {
    if (this.recipe?.id) {
      this.recipeService.incrementViewCount(this.recipe.id).subscribe({
        next: () => {
          this.recipeService.getByTransliteratedName(this.transliteratedName).subscribe(apiresponse => {
            this.recipe = apiresponse.result;
          });
        },
        error: (error) => {
          console.error('Failed to increment view count:', error);
        }
      });
    }
  }

  onStarClick(rating: number): void {
    if (!this.isAuthenticated) {
      this.collectionService.errorMessage$.next('Будь ласка, увійдіть в систему, щоб оцінити рецепт');
      return;
    }

    if (this.recipe?.id) {
      this.recipeService.rateRecipe(this.recipe.id, rating).subscribe({
        next: (response) => {
          this.userRating = rating;
          this.isRatingSubmitted = true;
          this.recipeService.getByTransliteratedName(this.transliteratedName).subscribe(apiresponse => {
            if(this.recipe){
              this.recipe.averageRating = apiresponse.result.averageRating;
            }
          });
        },
        error: (error) => {
          console.error('Failed to submit rating:', error);
          this.collectionService.errorMessage$.next('Помилка при збереженні оцінки. Спробуйте ще раз.');
        }
      });
    }
  }

  getStarClass(starNumber: number): string {
    const rating = this.hoverRating || this.userRating;
    return starNumber <= rating ? 'star-filled' : 'star-empty';
  }

  getRatingText(): string {
    if (!this.isAuthenticated) {
      return 'Увійдіть, щоб оцінити рецепт';
    }
    if (this.userRating) {
      return `Ваша оцінка: ${this.userRating} зірок`;
    }
    return 'Оцініть цей рецепт';
  }

  onStarHover(rating: number): void {
    if (!this.isAuthenticated) return;
    this.hoverRating = rating;
  }

  onStarLeave(): void {
    this.hoverRating = 0;
  }

  sendViewHistory(): void {
    if (this.recipe?.id) {
      this.authService.logView(this.recipe.id).subscribe({
        next: () => console.log('View history logged'),
        error: err => console.error('Error logging view history', err)
      });
    }
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = 'assets/hrechani-mlyntsi.webp';
  }
}