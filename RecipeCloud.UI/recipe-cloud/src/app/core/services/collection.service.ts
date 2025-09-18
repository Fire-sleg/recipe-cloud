import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { BehaviorSubject, catchError, map, Observable, of, tap, throwError } from "rxjs";
import { Collection } from "../models/collection.model";
import { environment } from "../../../environments/environment";
import { APIResponse } from "../models/api-response";

@Injectable({
  providedIn: 'root'
})
export class CollectionService {
  constructor(private http: HttpClient) {}

  private collectionsByUserSubject = new BehaviorSubject<Collection[]>([]);
  collectionsByUser$ = this.collectionsByUserSubject.asObservable();

  successMessage$ = new BehaviorSubject<string | null>(null);
  errorMessage$ = new BehaviorSubject<string | null>(null);

  createCollection(collection: FormData): Observable<Collection> {
    return this.http.post<APIResponse<Collection>>(`${environment.apiUrl}/collections`, collection).pipe(
      map(response => {
        if (response.isSuccess) {
          const updated = [...this.collectionsByUserSubject.value, response.result];
          this.collectionsByUserSubject.next(updated);
          return response.result;
        } else {
          throw new Error(response.errorMessages.join(', '));
        }
      }),
      catchError(error => {
        console.error('Error creating collection:', error);
        return throwError(() => error);
      })
    );
  }

  getByUserId(userId: string): Observable<Collection[]> {
    return this.http.get<APIResponse<Collection[]>>(`${environment.apiUrl}/collections/user/${userId}`).pipe(
      map(response => {
        if (response.isSuccess) {
          this.collectionsByUserSubject.next(response.result);
          return response.result;
        } else {
          this.collectionsByUserSubject.next([]);
          throw new Error(response.errorMessages.join(', '));
        }
      }),
      catchError(err => {
        console.error('Error loading collections', err);
        this.collectionsByUserSubject.next([]);
        return of([]);
      })
    );
  }

  deleteCollection(id: string): Observable<any> {
    return this.http.delete<APIResponse<any>>(`${environment.apiUrl}/collections/${id}`).pipe(
      map(response => {
        if (response.isSuccess) {
          const updated = this.collectionsByUserSubject.value.filter(c => c.id !== id);
          this.collectionsByUserSubject.next(updated);
          return response.result;
        } else {
          throw new Error(response.errorMessages.join(', '));
        }
      }),
      catchError(err => {
        console.error('Error deleting collection', err);
        return of(null);
      })
    );
  }

  addRecipeToCollection(collectionId: string, recipeId: string): Observable<Collection> {
    return this.http.post<APIResponse<Collection>>(
      `${environment.apiUrl}/collections/${collectionId}/recipes`,
      { recipeId }
    ).pipe(
      map(response => {
        if (response.isSuccess) {
          this.collectionsByUserSubject.next(
            this.collectionsByUserSubject.value.map(c => c.id === collectionId ? response.result : c)
          );
          return response.result;
        } else {
          throw new Error(response.errorMessages.join(', '));
        }
      })
    );
  }

  removeRecipeFromCollection(collectionId: string, recipeId: string): Observable<Collection> {
    return this.http.delete<APIResponse<Collection>>(
      `${environment.apiUrl}/collections/${collectionId}/recipes/${recipeId}`
    ).pipe(
      map(response => {
        if (response.isSuccess) {
          this.collectionsByUserSubject.next(
            this.collectionsByUserSubject.value.map(c => c.id === collectionId ? response.result : c)
          );
          return response.result;
        } else {
          throw new Error(response.errorMessages.join(', '));
        }
      })
    );
  }

  updateCollection(id: string, collection: FormData): Observable<Collection> {
    return this.http.put<APIResponse<Collection>>(`${environment.apiUrl}/collections/${id}`, collection).pipe(
      map(response => {
        if (response.isSuccess) {
          const updated = this.collectionsByUserSubject.value.map(c =>
            c.id === id ? response.result : c
          );
          this.collectionsByUserSubject.next(updated);
          this.successMessage$.next('Колекцію успішно оновлено!');
          return response.result;
        } else {
          this.errorMessage$.next('Помилка при оновленні колекції');
          throw new Error(response.errorMessages.join(', '));
        }
      }),
      catchError(err => {
        console.error('Error updating collection', err);
        this.errorMessage$.next('Помилка при оновленні колекції');
        throw err;
      })
    );
  }

  clearSuccessMessage(): void {
    this.successMessage$.next(null);
  }

  clearErrorMessage(): void {
    this.errorMessage$.next(null);
  }
}
