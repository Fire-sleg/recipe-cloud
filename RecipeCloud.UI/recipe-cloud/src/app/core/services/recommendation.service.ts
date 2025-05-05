import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Recommendation } from '../models/recommendation.model';

@Injectable({
  providedIn: 'root'
})
export class RecommendationService {
  constructor(private http: HttpClient) {}

  getRecommendations(): Observable<Recommendation[]> {
    return this.http.get<Recommendation[]>(`${environment.apiUrl}/recommendations`);
  }

  recordInteraction(recipeId: string, interactionType: 'View' | 'Rate' | 'Search', details?: any): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/interactions`, { recipeId, interactionType, details });
  }
}