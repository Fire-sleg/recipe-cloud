import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, catchError, map, Observable, of, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Recipe } from '../models/recipe.model';
import { PagedResponse } from '../models/paged-response';
import { CheckboxFilter } from '../models/checkboxfilter.model';
import { Rating } from '../models/rating.model';
import { APIResponse } from '../models/api-response';

@Injectable({
  providedIn: 'root'
})
export class RecipeService {
  constructor(private http: HttpClient) {}

  private recipesByUserSubject = new BehaviorSubject<Recipe[]>([]);
  recipesByUser$ = this.recipesByUserSubject.asObservable();

  getRecipes(page: number, pageSize: number): Observable<APIResponse<PagedResponse<Recipe>>> {
    const params = new HttpParams()
      .set('PageNumber', page.toString())  
      .set('PageSize', pageSize.toString()); 
    return this.http.get<APIResponse<PagedResponse<Recipe>>>(`${environment.apiUrl}/recipes`, { params });
  }

  getByTransliteratedName(transliteratedName: string): Observable<APIResponse<Recipe>> {
    return this.http.get<APIResponse<Recipe>>(`${environment.apiUrl}/recipes/by-slug/${transliteratedName}`);
  }

  getRecipeById(id: string): Observable<APIResponse<Recipe>> {
    return this.http.get<APIResponse<Recipe>>(`${environment.apiUrl}/recipes/${id}`);
  }

  getByUserId(userId: string): Observable<APIResponse<Recipe[]>> {
    return this.http.get<APIResponse<Recipe[]>>(`${environment.apiUrl}/recipes/user/${userId}`).pipe(
      tap(response => {
        if (response.isSuccess) {
          this.recipesByUserSubject.next(response.result);
        }
      }),
      catchError(err => {
        console.error('Error loading recipes', err);
        this.recipesByUserSubject.next([]);
        return of({
          statusCode: 500,
          isSuccess: false,
          errorMessages: ['Failed to load recipes'],
          result: []
        } as APIResponse<Recipe[]>);
      })
    );
  }

  createRecipe(recipe: FormData): Observable<APIResponse<Recipe>> {
    return this.http.post<APIResponse<Recipe>>(`${environment.apiUrl}/recipes`, recipe).pipe(
      tap(response => {
        if (response.isSuccess) {
          console.log('New recipe added:', response.result);
          const updated = [...this.recipesByUserSubject.value, response.result];
          this.recipesByUserSubject.next(updated);
        }
      })
    );
  }

  updateRecipe(id: string, recipe: FormData): Observable<APIResponse<any>> {
    return this.http.put<APIResponse<any>>(`${environment.apiUrl}/recipes/${id}`, recipe).pipe(
      tap(response => {
        if (response.isSuccess) {
          console.log('Recipe updated successfully');
        }
      })
    );
  }

  deleteRecipe(id: string): Observable<APIResponse<any>> {
    return this.http.delete<APIResponse<any>>(`${environment.apiUrl}/recipes/${id}`).pipe(
      tap(response => {
        if (response.isSuccess) {
          const updated = this.recipesByUserSubject.value.filter(r => r.id !== id);
          this.recipesByUserSubject.next(updated);
        }
      }),
      catchError(err => {
        console.error('Error deleting recipe', err);
        return of({
          statusCode: 500,
          isSuccess: false,
          errorMessages: ['Failed to delete recipe'],
          result: null
        } as APIResponse<any>);
      })
    );
  }

  incrementViewCount(recipeId: string): Observable<APIResponse<any>> {
    return this.http.patch<APIResponse<any>>(`${environment.apiUrl}/recipes/${recipeId}/increment-views`, {});
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

  getFilterRecipes(filters: any, pageNumber: number, pageSize: number, sortOrder?: string): Observable<PagedResponse<Recipe>> {
    
    let filterString = createFilterString(filters);
    
    let params = new HttpParams()
      .set('filters', filterString)
      .set('PageNumber', pageNumber.toString())
      .set('PageSize', pageSize.toString());

    if (sortOrder) {
      params = params.set('sortOrder', sortOrder);
    }

    const url = `${environment.apiUrl}/recipes/filter`;
    const finalUrl = `${url}?${params.toString()}`;
    console.log('Request URL:', finalUrl);

    return this.http.get<APIResponse<PagedResponse<Recipe>>>(url, { params })
      .pipe(
        map(response => {
          if (response.isSuccess) {
            return response.result;
          } else {
            throw new Error(response.errorMessages.join(', '));
          }
        }),
        catchError(error => {
          console.error('Error filtering recipes:', error);
          return throwError(() => error);
        })
      );
  }

  getCheckboxFilter(categoryId: string): Observable<CheckboxFilter> {
    const params = new HttpParams().set('categoryId', categoryId);

    return this.http.get<APIResponse<CheckboxFilter>>(
      `${environment.apiUrl}/recipes/checkboxfilter`,
      { params }
    ).pipe(
      map(response => {
        if (response.isSuccess) {
          return response.result;
        } else {
          throw new Error(response.errorMessages.join(', '));
        }
      }),
      catchError(error => {
        console.error('Error fetching checkbox filters:', error);
        return throwError(() => error);
      })
    );
  }

  getRecipesByCategory(categoryId: string): Observable<APIResponse<Recipe[]>> {
    return this.http.get<APIResponse<Recipe[]>>(`${environment.apiUrl}/recipes/by-category/${categoryId}`);
  }

  createRecipeBatch(recipes: any[]): Observable<APIResponse<any>> {
    return this.http.post<APIResponse<any>>(`${environment.apiUrl}/recipes/batch`, recipes);
  }

  patchRecipe(id: string, patchDocument: any): Observable<APIResponse<any>> {
    return this.http.patch<APIResponse<any>>(`${environment.apiUrl}/recipes/${id}`, patchDocument);
  }

  getRecipesCount(): Observable<APIResponse<number>> {
    return this.http.get<APIResponse<number>>(`${environment.apiUrl}/recipes/count`);
  }
}

function createFilterString(filters: any): string {
  let params = new HttpParams();
  for (const key in filters) {
    if (filters.hasOwnProperty(key)) {
      const values = filters[key];
      if (Array.isArray(values)) {
        values.forEach(value => {
          params = params.append(key, value);
        });
      } else if (values !== null && values !== undefined) {
        params = params.append(key, values);
      }
    }
  }
  return params.toString();
}