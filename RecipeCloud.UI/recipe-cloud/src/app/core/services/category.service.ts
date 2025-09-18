import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { catchError, map, Observable, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Recipe } from '../models/recipe.model';
import { Category } from '../models/category.model';
import { APIResponse } from '../models/api-response';


@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  constructor(private http: HttpClient) {}

  getCategoryById(id: string): Observable<Category> {
    return this.http.get<APIResponse<Category>>(`${environment.apiUrl}/categories/${id}`).pipe(
      map(response => {
        if (response.isSuccess) {
          return response.result;
        } else {
          throw new Error(response.errorMessages.join(', '));
        }
      }),
      catchError(error => {
        console.error('Error fetching category by id:', error);
        return throwError(() => error);
      })
    );
  }

  getCategoryByTransliteratedName(transliteratedName: string): Observable<Category> {
    return this.http.get<APIResponse<Category>>(`${environment.apiUrl}/categories/${transliteratedName}`).pipe(
      map(response => {
        if (response.isSuccess) {
          return response.result;
        } else {
          throw new Error(response.errorMessages.join(', '));
        }
      }),
      catchError(error => {
        console.error('Error fetching category by transliterated name:', error);
        return throwError(() => error);
      })
    );
  }

  getSubCategoriesWithRecipes(): Observable<Category[]> {
    return this.http.get<APIResponse<Category[]>>(`${environment.apiUrl}/categories/sub-with-recipes`).pipe(
      map(response => {
        if (response.isSuccess) {
          return response.result;
        } else {
          throw new Error(response.errorMessages.join(', '));
        }
      }),
      catchError(error => {
        console.error('Error fetching subcategories with recipes:', error);
        return throwError(() => error);
      })
    );
  }

}