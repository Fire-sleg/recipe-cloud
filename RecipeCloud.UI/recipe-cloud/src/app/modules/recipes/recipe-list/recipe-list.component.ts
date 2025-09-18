import { Component, Input, OnInit } from '@angular/core';
import { map, Observable, of } from 'rxjs';
import { RecipeService } from '../../../core/services/recipe.service';
import { Recipe } from '../../../core/models/recipe.model';
import { User } from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';
import { Category } from '../../../core/models/category.model';
import { PagedResponse } from '../../../core/models/paged-response';
import { CheckboxFilter } from '../../../core/models/checkboxfilter.model';
import { UserPreferences } from '../../../core/models/user-preferences.model';
import { title } from 'process';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-recipe-list',
  templateUrl: './recipe-list.component.html',
  styleUrls: ['./recipe-list.component.css']
})
export class RecipeListComponent  implements OnInit  {
  @Input() category: Category | null = null;
  

  //preferences: UserPreferences[] = [];

  recipes: Recipe[] = [];
  filteredRecipes: Recipe[] = [];
  totalCount: number = 0;
  pageNumber: number = 1;
  pageSize: number = 10;
  filters: any = {
    title: '',
    categoryId: '',
    allergens: [],
    cuisines: [],
    diets: [],
    tags: [],
    isUserCreated: false
  };
  sortOrder: string = 'Alphabetical';

  checkboxFilter: CheckboxFilter | undefined;

  private searchTimeout: any;

  isAuthenticated: boolean = false;

  showNotFound = false;
  constructor(private recipeService: RecipeService, private authService: AuthService, private route: ActivatedRoute, private router: Router){}

  ngOnInit(): void {    


    this.isAuthenticated = this.authService.isAuthenticated();
    this.filters["categoryId"] = this.category?.id;
    this.getCheckboxFilter();
    // this.setPreferencesFromStorage();
    // this.loadRecipes();
  }

  setPreferencesFromStorage(): void {
    const prefsJson = localStorage.getItem('preferences');
    if (!prefsJson || !this.checkboxFilter) return;

    try {
      const prefs: UserPreferences = JSON.parse(prefsJson);

      // Фільтруємо тільки доступні в категорії дієти
      this.filters.diets = prefs.dietaryPreferences.filter(d =>
        this.checkboxFilter!.diets.includes(d)
      );

      this.filters.allergens = prefs.allergens.filter(a =>
        this.checkboxFilter!.allergens.includes(a)
      );

      this.filters.cuisines = prefs.favoriteCuisines.filter(c =>
        this.checkboxFilter!.cuisines.includes(c)
      );

    } catch (e) {
      console.error('❌ Invalid preferences format in localStorage', e);
    }
  }

  getCheckboxFilter(): void {
    if (this.category) {
      this.recipeService.getCheckboxFilter(this.category.id).subscribe((filter: CheckboxFilter) => {
        
        this.checkboxFilter = filter;
        //this.setPreferencesFromStorage(); 
        this.loadRecipes(); 
      });
    }
  }

  loadRecipes(): void {
    this.recipeService.getFilterRecipes(this.filters, this.pageNumber, this.pageSize, this.sortOrder)
      .subscribe((response: PagedResponse<Recipe>) => {
        setTimeout(() => {
          if (!this.recipes || !this.recipes.length) {
            this.showNotFound = true;
          }
        }, 1000);
        this.recipes = response.data;
        this.totalCount = response.totalCount;

        // Якщо є пошуковий запит, додатково фільтруємо по назві
        if (this.filters.name && this.filters.name.trim()) {
          this.filteredRecipes = this.smartSearch(this.filters.name, response.data);
        } else {
          this.filteredRecipes = [];
        }
      });
  }
  // Розумний пошук по назві рецепту
  smartSearch(searchTerm: string, recipes: Recipe[]): Recipe[] {
    if (!searchTerm || !searchTerm.trim()) {
      return [];
    }

    const term = searchTerm.toLowerCase().trim();
    
    return recipes.filter(recipe => {
      // Пошук по назві рецепту
      const titleMatch = recipe.title?.toLowerCase().includes(term);
      // Пошук по транслітерованій назві
      const transliteratedMatch = recipe.transliteratedName?.toLowerCase().includes(term);
      // Можна додати пошук по інгредієнтах, якщо є таке поле
      // const ingredientsMatch = recipe.ingredients?.some(ing => ing.toLowerCase().includes(term));
      
      return titleMatch || transliteratedMatch;
    });
  }
  // Пошук в реальному часі при введенні тексту
  onSearchInput(): void {
    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.pageNumber = 1;
      this.loadRecipes();
    }, 300);
  }

  onFilterChange(attributeName: string, attributeValue: string, event: any): void {
    const checked = event?.target?.checked;
  
    if (attributeName && attributeValue !== undefined && attributeValue !== null) {
      if (attributeName === "isUserCreated") {
        this.filters[attributeName] = checked;
      }
      else{
        if (!this.filters[attributeName]) {
        this.filters[attributeName] = [];
        }
    
        if (checked) {
          this.filters[attributeName].push(attributeValue);
        } else {
          this.filters[attributeName] = this.filters[attributeName].filter((value: string) => value !== attributeValue);
        }

      }

      
  
      this.pageNumber = 1; // Скинути до першої сторінки при зміні фільтрів
      this.loadRecipes();
    }
  }
  

  onSortOrderChange(event: any): void {
    const newSortOrder = event?.target?.value;
    if (newSortOrder) {
      this.sortOrder = newSortOrder;
      this.pageNumber = 1; // Скинути до першої сторінки при зміні сортування
      this.loadRecipes();
    }
  }

  onPageChange(newPageNumber: number): void {
    if (newPageNumber > 0 && newPageNumber <= this.getMaxPage()) {
      this.pageNumber = newPageNumber;
      this.loadRecipes();
    }
  }

  getMaxPage(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  getPaginationNumbers(): number[] {
    const maxPage = this.getMaxPage();
    const pages: number[] = [];
    for (let i = 1; i <= maxPage; i++) {
      pages.push(i);
    }
    return pages;
  }

  resetFilters(): void {
    this.filters = {};
    this.filters.name = '';
    this.filters["categoryId"] =this.category?.id;
    this.pageNumber = 1; // Скинути до першої сторінки при скиданні фільтрів
    this.loadRecipes();
  }

}


