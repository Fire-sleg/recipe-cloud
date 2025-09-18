import { Component, OnInit, ViewChild } from '@angular/core';
import { Recipe } from '../../../core/models/recipe.model';
import { Router } from '@angular/router';
import { RecipeService } from '../../../core/services/recipe.service';
import { AuthService } from '../../../core/services/auth.service';
import { Collection } from '../../../core/models/collection.model';
import { CollectionService } from '../../../core/services/collection.service';
import { Observable, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { PagedResponse } from '../../../core/models/paged-response';
import { CollectionFormComponent } from '../../collections/collection-form/collection-form.component';
import { APIResponse } from '../../../core/models/api-response';


@Component({
  selector: 'app-user-collections',
  templateUrl: './user-collections.component.html',
  styleUrls: ['./user-collections.component.css']
})
export class UserCollectionsComponent implements OnInit {
  collections$!: Observable<Collection[]>;
  userId: string | undefined;
  showAddModal = false;
  selectedCollectionId: string | null = null;
  selectedCollection: Collection | null = null;

  // Search and recipe properties
  pageNumber = 1;
  pageSize = 10;
  recipes: Recipe[] = [];
  filteredRecipes: Recipe[] = [];
  totalCount = 0;
  showNotFound = false;
  filters: { name?: string } = {};
  searchTimeout: any;

  @ViewChild(CollectionFormComponent) collectionFormComponent!: CollectionFormComponent;

  constructor(
    private router: Router,
    private collectionService: CollectionService,
    private authService: AuthService,
    private recipeService: RecipeService
  ) {}

  ngOnInit(): void {
    this.userId = this.authService.getCurrentUserId();
    if (this.userId) {
      this.collections$ = this.collectionService.collectionsByUser$;
      this.collectionService.getByUserId(this.userId).pipe(
        catchError(error => {
          console.error('Error loading collections:', error);
          return of([]);
        })
      ).subscribe();
    }
  }

  createNewCollection(): void {
    this.collectionFormComponent.showCreateForm();
  }

  editCollection(collection: Collection): void {
    if (!this.authService.isAuthenticated()) {
      alert('Для редагування колекції потрібно увійти в систему');
      return;
    }
    this.selectedCollection = collection;
  }

  openRecipeSelector(collectionId: string): void {
    this.selectedCollectionId = collectionId;
    this.showAddModal = true;
    this.filters.name = '';
    this.filteredRecipes = [];
    this.showNotFound = false;
  }

  closeRecipeSelector(): void {
    this.showAddModal = false;
    this.selectedCollectionId = null;
    this.filters.name = '';
    this.filteredRecipes = [];
    this.showNotFound = false;
  }

  onDeleteCollection(collectionId: string): void {
    if (this.userId) {
      this.collectionService.deleteCollection(collectionId).pipe(
        catchError(error => {
          console.error('Error deleting collection:', error);
          return of(null);
        })
      ).subscribe();
    }
  }

  removeFromCollection(collectionId: string, recipeId: string): void {
    if (this.userId) {
      this.collectionService.removeRecipeFromCollection(collectionId, recipeId).pipe(
        catchError(error => {
          console.error('Error removing recipe:', error);
          return of(null);
        })
      ).subscribe();
    }
  }

  addRecipeToCollection(recipeId: string): void {
    if (this.userId && this.selectedCollectionId) {
      this.collectionService.addRecipeToCollection(this.selectedCollectionId, recipeId).pipe(
        catchError(error => {
          console.error('Error adding recipe to collection:', error);
          return of(null);
        })
      ).subscribe(() => {
        this.closeRecipeSelector();
        this.collectionService.getByUserId(this.userId!).subscribe();
      });
    }
  }

  onSearchInput(): void {
    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      if (!this.filters.name?.trim()) {
        this.filteredRecipes = [];
        this.showNotFound = false;
        return;
      }
      this.pageNumber = 1;
      this.loadRecipes();
    }, 300);
  }

  loadRecipes(): void {
    this.showNotFound = false;
    this.recipeService.getRecipes(this.pageNumber, this.pageSize).pipe(
      switchMap(apiresponse =>
        of({
          ...apiresponse,
          result: {
            ...apiresponse.result,
            data: this.smartSearch(this.filters.name || '', apiresponse.result.data)
          }
        })
      ),
      catchError(error => {
        console.error('Error loading recipes:', error);
        return of({
          statusCode: 500,
          isSuccess: false,
          errorMessages: ['Failed to load recipes'],
          result: { data: [], totalCount: 0, pageNumber: 0, pageSize: 0 } as PagedResponse<Recipe>
        });
      })
    ).subscribe({
      next: (apiresponse: APIResponse<PagedResponse<Recipe>>) => {
        this.filteredRecipes = apiresponse.result.data;
        this.totalCount = apiresponse.result.totalCount;
        this.showNotFound = !!this.filters.name?.trim() && !this.filteredRecipes.length;
      },
      error: (error) => {
        console.error('Subscription error:', error);
      }
    });
  }

  smartSearch(searchTerm: string, recipes: Recipe[]): Recipe[] {
    if (!searchTerm || !searchTerm.trim()) {
      return [];
    }
    const term = searchTerm.toLowerCase().trim();
    return recipes.filter(recipe => {
      const titleMatch = recipe.title?.toLowerCase().includes(term) ?? false;
      const transliteratedMatch = recipe.transliteratedName?.toLowerCase().includes(term) ?? false;
      return titleMatch || transliteratedMatch;
    });
  }

  onCollectionCreated(collection: Collection): void {
    // Refresh the collections list
    if (this.userId) {
      this.collectionService.getByUserId(this.userId).subscribe();
    }
  }

  onCollectionUpdated(updatedCollection: Collection): void {
    this.selectedCollection = null;
    // Refresh the collections list
    if (this.userId) {
      this.collectionService.getByUserId(this.userId).subscribe();
    }
  }

  onFormClosed(): void {
    this.selectedCollection = null;
  }

  trackByCollectionId(index: number, collection: Collection): string {
    return collection.id;
  }

  trackByRecipeId(index: number, recipe: Recipe): string {
    return recipe.id ?? '';
  }
}