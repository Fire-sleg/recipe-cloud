import { Component, Input, OnInit } from '@angular/core';
import { map, Observable, of } from 'rxjs';
import { RecipeService } from '../../../core/services/recipe.service';
import { Recipe } from '../../../core/models/recipe.model';
import { User } from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';
import { Category } from '../../../core/models/category.model';
import { PagedResponse } from '../../../core/models/paged-response';
import { CheckboxFilter } from '../../../core/models/checkboxfilter.model';
import { ActivatedRoute } from '@angular/router';
import { CategoryService } from '../../../core/services/category.service';

@Component({
  selector: 'app-recipe-filter',
  templateUrl: './recipe-filter.component.html',
  styleUrls: ['./recipe-filter.component.css']
})
export class RecipeFilterComponent implements OnInit {
  transliteratedName: string | null = null;
  category: Category | null = null;
  showNotFoundCategory = false;
  showNotFoundRecipes = false;

  constructor(
    private route: ActivatedRoute,
    private categoryService: CategoryService
  ) {}

  ngOnInit(): void {

    setTimeout(() => {
          if (!this.category) {
            this.showNotFoundCategory = true;
          }
          if(!this.category?.recipes || !this.category?.recipes.length){
            this.showNotFoundRecipes = true;
          }
        }, 1000);

    this.route.paramMap.subscribe(params => {
      this.transliteratedName = params.get('transliteratedName');

      if (this.transliteratedName) {
        this.loadCategory(this.transliteratedName);
      }
    });
  }

  loadCategory(transliteratedName: string): void {

    this.categoryService.getCategoryByTransliteratedName(transliteratedName).subscribe({
      next: (data: Category) => {
        this.category = data;
      },
      error: err => {
        console.error('Не вдалося завантажити категорію', err);
      }
    });
  }
}


