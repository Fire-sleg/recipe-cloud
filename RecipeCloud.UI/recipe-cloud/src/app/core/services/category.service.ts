import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Recipe } from '../models/recipe.model';
import { Category } from '../models/category.model';


@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  constructor(private http: HttpClient) {}


  getCategoryById(id: string) {
    return this.http.get<Category>(`${environment.apiUrl}/categories/` + id);
  }
  getCategoryByTransliteratedName(transliteratedName: string){
    return this.http.get<Category>(`${environment.apiUrl}/categories/` + transliteratedName);
  }
  //categories/sub-with-recipes
  getSubCategoriesWithRecipes(){
    return this.http.get<Category[]>(`${environment.apiUrl}/categories/sub-with-recipes`);
  }

}