import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Recommendation } from '../models/recommendation.model';
import { Recipe } from '../models/recipe.model';

@Injectable({
  providedIn: 'root'
})
export class RecommendationService {
  constructor(private http: HttpClient) {}

  // getRecommendations(limit: number): Observable<Recipe[]> {
  //   return this.http.get<Recipe[]>(`${environment.recomApiUrl}/recommendations/` + limit);
  // }
  getRecommendations(limit: number): Observable<Recipe[]> {
  return this.http.get<Recipe[]>(`${environment.recomApiUrl}/recommendations`, {
    params: { limit: limit.toString() }
  });
}


}