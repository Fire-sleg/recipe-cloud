import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Recipe } from '../models/recipe.model';
import { Category } from '../models/category.model';


@Injectable({
  providedIn: 'root'
})
export class HomeService {
  constructor(private http: HttpClient) {}


  getBaseCategories() {
    return this.http.get<Category[]>(`${environment.apiUrl}/categories/base`);
  }

//   getRecipes(page: number, pageSize: number): Observable<{ data: Recipe[], total: number }> {
//     const params = new HttpParams()
//       .set('page', page.toString())
//       .set('pageSize', pageSize.toString());
//     return this.http.get<{ data: Recipe[], total: number }>(`${environment.apiUrl}/recipe`, { params });
//   }

  
}