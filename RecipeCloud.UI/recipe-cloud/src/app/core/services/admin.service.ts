import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Recipe } from '../models/recipe.model';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  constructor(private http: HttpClient) {}

  getPendingRecipes(page: number, pageSize: number): Observable<Recipe[]> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<Recipe[]>(`${environment.apiUrl}/admin/recipes/pending`, { params });
  }

  approveRecipe(id: string): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/admin/recipes/${id}/approve`, {});
  }

  rejectRecipe(id: string): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/admin/recipes/${id}/reject`, {});
  }

  deleteComment(recipeId: string, commentId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/admin/recipes/${recipeId}/comments/${commentId}`);
  }
}