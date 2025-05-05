import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Recipe } from '../models/recipe.model';
import { Comment } from '../models/comment.model';
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
    return this.http.get<{ data: Recipe[], total: number }>(`${environment.apiUrl}/recipe`, { params });
  }

  getRecipe(id: string): Observable<Recipe> {
    return this.http.get<Recipe>(`${environment.apiUrl}/recipe/${id}`);
  }

  createRecipe(recipe: FormData): Observable<Recipe> {
    return this.http.post<Recipe>(`${environment.apiUrl}/recipe`, recipe);
  }

  updateRecipe(id: string, recipe: FormData): Observable<Recipe> {
    return this.http.put<Recipe>(`${environment.apiUrl}/recipe/${id}`, recipe);
  }

  deleteRecipe(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/recipe/${id}`);
  }

  searchRecipes(query: string, filters: any): Observable<Recipe[]> {
    let params = new HttpParams().set('query', query);
    if (filters.diets?.length) params = params.set('diet', filters.diets.join(','));
    if (filters.allergens?.length) params = params.set('allergens', filters.allergens.join(','));
    if (filters.cuisine) params = params.set('cuisine', filters.cuisine);
    if (filters.caloriesMin) params = params.set('caloriesMin', filters.caloriesMin);
    if (filters.caloriesMax) params = params.set('caloriesMax', filters.caloriesMax);
    if (filters.fromSubscriptions) params = params.set('fromSubscriptions', 'true');
    return this.http.get<Recipe[]>(`${environment.apiUrl}/recipe/search`, { params });
  }

  getComments(recipeId: string, page: number, pageSize: number): Observable<Comment[]> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<Comment[]>(`${environment.apiUrl}/recipe/${recipeId}/comments`, { params });
  }

  addComment(recipeId: string, text: string): Observable<Comment> {
    return this.http.post<Comment>(`${environment.apiUrl}/recipe/${recipeId}/comments`, { text });
  }

  deleteComment(recipeId: string, commentId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/recipe/${recipeId}/comments/${commentId}`);
  }

  addRating(recipeId: string, score: number): Observable<Rating> {
    return this.http.post<Rating>(`${environment.apiUrl}/recipe/${recipeId}/ratings`, { score });
  }

  getAverageRating(recipeId: string): Observable<number> {
    return this.http.get<number>(`${environment.apiUrl}/recipe/${recipeId}/ratings/average`);
  }
}