import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, catchError, delay, map, Observable, of, retryWhen, scan, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Category } from '../models/category.model';
import { APIResponse } from '../models/api-response';

@Injectable({
  providedIn: 'root'
})
export class HomeService {
  constructor(private http: HttpClient) {}

  private baseCategoriesSubject = new BehaviorSubject<Category[]>([]);
  baseCategories$ = this.baseCategoriesSubject.asObservable();

  getBaseCategories(): Observable<Category[]> {
    return this.http.get<APIResponse<Category[]>>(`${environment.apiUrl}/categories/base`).pipe(
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
      map(response => {
        if (response.isSuccess) {
          this.baseCategoriesSubject.next(response.result);
          return response.result;
        } else {
          this.baseCategoriesSubject.next([]);
          throw new Error(response.errorMessages.join(', '));
        }
      }),
      catchError(err => {
        console.error('Error loading base categories', err);
        this.baseCategoriesSubject.next([]);
        return of([]);
      })
    );
  }
}
