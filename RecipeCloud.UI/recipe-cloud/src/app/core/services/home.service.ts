import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, catchError, delay, Observable, of, retryWhen, scan, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Recipe } from '../models/recipe.model';
import { Category } from '../models/category.model';


@Injectable({
  providedIn: 'root'
})
export class HomeService {
  constructor(private http: HttpClient) {}

  private baseCategoriesSubject = new BehaviorSubject<Category[]>([]);
  baseCategories$ = this.baseCategoriesSubject.asObservable();


  getBaseCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${environment.apiUrl}/categories/base`).pipe(
    retryWhen(errors =>
      errors.pipe(
        scan((retryCount, err) => {
          if (retryCount >= 10) { // макс. кількість спроб
            throw err;
          }
          console.warn(`Server not ready, retrying... (${retryCount + 1})`);
          return retryCount + 1;
        }, 0),
        delay(2000) // 2 секунди між спробами
      )
    ),
    tap(baseCategories => this.baseCategoriesSubject.next(baseCategories)),
    catchError(err => {
      console.error('Error loading recipes', err);
      this.baseCategoriesSubject.next([]);
      return of([]);
    })
  );
  }

//   getRecipes(page: number, pageSize: number): Observable<{ data: Recipe[], total: number }> {
//     const params = new HttpParams()
//       .set('page', page.toString())
//       .set('pageSize', pageSize.toString());
//     return this.http.get<{ data: Recipe[], total: number }>(`${environment.apiUrl}/recipe`, { params });
//   }

  
}