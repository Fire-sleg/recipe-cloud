
import { Component, Input, OnInit } from '@angular/core';
import { Recipe } from '../../../core/models/recipe.model';
import { ActivatedRoute } from '@angular/router';
import { CategoryService } from '../../../core/services/category.service';
import { RecipeService } from '../../../core/services/recipe.service';
import { AuthService } from '../../../core/services/auth.service';
import { ViewHistory } from '../../../core/models/view-history.model';
import { User } from '../../../core/models/user.model';

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

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = 'assets/hrechani-mlyntsi.webp';
  }

  constructor(
    private route: ActivatedRoute,
    private recipeService: RecipeService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Check if user is authenticated
    this.isAuthenticated = this.authService.isAuthenticated();
    const userJson = localStorage.getItem("user");
    this.currentUser = userJson ? JSON.parse(userJson) : null;


    
    this.route.paramMap.subscribe(params => {
      const transliteratedName = params.get('transliteratedName');
      if (transliteratedName) {
        this.recipeService.getByTransliteratedName(transliteratedName).subscribe(recipe => {
          this.recipe = recipe;
          this.incrementViewCount();
          this.getCurrentUserRating();
          this.sendViewHistory();
          

        });
      }
    });
  }

  // Перевірка чи може користувач редагувати рецепт
  canEditRecipe(): boolean {
    if (!this.recipe || !this.currentUser) {
      return false;
    }

    // Адміністратор може редагувати будь-який рецепт
    if (this.currentUser.role === 'Admin') {
      return true;
    }

    // Користувач може редагувати тільки свої рецепти
    return this.recipe.createdBy === this.currentUser.id;
  }

  // Обробник кліку на кнопку редагування
  onEditRecipe(): void {
    if (!this.recipe || !this.canEditRecipe()) {
      return;
    }

    // Перенаправлення на сторінку редагування

  }
  onDeleteRecipe(): void {
    if (!this.recipe || !this.canEditRecipe()) {
      return;
    }

    // Перенаправлення на сторінку підтвердження видалення

  }
  onAddToCollection(): void{
    
  }

  private getCurrentUserRating(){
    if (this.recipe?.id && this.isAuthenticated) {
      this.recipeService.getRecipeRating(this.recipe.id).subscribe({
        next: (rating) => {
          if (this.recipe) {
            this.userRating = rating.rating
          }
        },
        error: (error) => {
          console.error('Failed to increment view count:', error);
        }
      });
    }
  }

  private incrementViewCount(): void {
    if (this.recipe?.id) {
      this.recipeService.incrementViewCount(this.recipe.id).subscribe({
        next: () => {
          console.log('View count incremented successfully');
          if (this.recipe) {
            this.recipe.viewCount++;
          }
        },
        error: (error) => {
          console.error('Failed to increment view count:', error);
        }
      });
    }
  }



  // Rating methods
  onStarHover(rating: number): void {
    if (!this.isAuthenticated) return;
    this.hoverRating = rating;
  }

  onStarLeave(): void {
    this.hoverRating = 0;
  }

  onStarClick(rating: number): void {
    if (!this.isAuthenticated) {
      alert('Будь ласка, увійдіть в систему, щоб оцінити рецепт');
      return;
    }

    if (this.recipe?.id) {
          debugger;

      this.recipeService.rateRecipe(this.recipe.id, rating).subscribe({
        next: (response) => {
          this.userRating = rating;
          this.isRatingSubmitted = true;
          
          
          // Update recipe's average rating
          if (this.recipe && response.averageRating !== undefined) {
            this.recipe.averageRating = response.averageRating;
          }
          
          console.log('Rating submitted successfully');
        },
        error: (error) => {
          console.error('Failed to submit rating:', error);
          alert('Помилка при збереженні оцінки. Спробуйте ще раз.');
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


  sendViewHistory(): void {
    if(this.recipe){
      this.authService.logView(this.recipe.id).subscribe({
        next: () => console.log('View history logged'),
        error: err => console.error('Error logging view history', err)
      });

    }
    
  }
}