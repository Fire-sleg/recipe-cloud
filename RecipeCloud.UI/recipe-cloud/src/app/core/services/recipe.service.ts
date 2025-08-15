import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Recipe } from '../models/recipe.model';
import { PagedResponse } from '../models/paged-response';
import { CheckboxFilter } from '../models/checkboxfilter.model';
import { Rating } from '../models/rating.model';

@Injectable({
  providedIn: 'root'
})
export class RecipeService {
  constructor(private http: HttpClient) {}

  getRecipes(page: number, pageSize: number): Observable<{ data: Recipe[], total: number }> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<{ data: Recipe[], total: number }>(`${environment.apiUrl}/recipes`, { params });
  }
  getByTransliteratedName(transliteratedName: string){
    return this.http.get<Recipe>(`${environment.apiUrl}/recipes/` + transliteratedName);
  }
  getByUserId(userId: string){
    return this.http.get<Recipe[]>(`${environment.apiUrl}/recipes/user/` + userId);
  }

  getRecipeById(id: string): Observable<Recipe> {
    return this.http.get<Recipe>(`${environment.apiUrl}/recipes/${id}`);
  }

  createRecipe(recipe: FormData): Observable<Recipe> {
    return this.http.post<Recipe>(`${environment.apiUrl}/recipes`, recipe);
  }

  updateRecipe(id: string, recipe: FormData): Observable<Recipe> {
    return this.http.put<Recipe>(`${environment.apiUrl}/recipes/${id}`, recipe);
  }

  deleteRecipe(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/recipes/${id}`);
  }

  incrementViewCount(recipeId: string): Observable<any> {
    return this.http.patch(`${environment.apiUrl}/recipes/${recipeId}/increment-views`, {});
  }

  rateRecipe(recipeId: string, rating: number): Observable<any> {
    const ratingDto: Rating = {
      recipeId,
      rating
    };
    
    return this.http.post(`${environment.apiUrl}/rating/rate`, ratingDto);
  }

  getRecipeRating(recipeId: string): Observable<Rating> {
    return this.http.get<Rating>(`${environment.apiUrl}/rating/get-rating/${recipeId}`);
  }


  getFilterRecipes(filters: any, pageNumber: number, pageSize: number, sortOrder: string): Observable<PagedResponse<Recipe>> {
    const filterString = createFilterString(filters);
    /* const filtersString = Object.entries(filters)
        .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
        .join('&'); */
    
    const params = new HttpParams()
      .set('filters', filterString)
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('sortOrder', sortOrder);

      const url = `${environment.apiUrl}/recipes/filter`;

      const finalUrl = `${url}?${params.toString()}`;
  
      console.log('Request URL:', finalUrl); // Виведення кінцевого URL перед відправкою запиту

    return this.http.get<PagedResponse<Recipe>>(`${environment.apiUrl}/recipes/filter`, { params });
  }

  getCheckboxFilter(categoryId: string | null): Observable<CheckboxFilter> {
    const params = categoryId ? new HttpParams().set('categoryId', categoryId) : undefined;

    return this.http.get<CheckboxFilter>(
      `${environment.apiUrl}/recipes/checkboxfilter`,
      { params }
    );
}



}

// Функція для перетворення об'єкта фільтрів на рядок
function createFilterString(filters: any): string {
  let params = new HttpParams();
  for (const key in filters) {
    if (filters.hasOwnProperty(key)) {
      const values = filters[key];
      if (Array.isArray(values)) {
        values.forEach(value => {
          params = params.append(key, value);
        });
      } else {
        params = params.append(key, values);
      }
    }
  }
  return params.toString();
}