import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { BehaviorSubject, catchError, Observable, of, tap } from "rxjs";
import { Collection } from "../models/collection.model";
import { environment } from "../../../environments/environment";

@Injectable({
  providedIn: 'root'
})
export class CollectionService {
  constructor(private http: HttpClient) {}

  private collectionsByUserSubject = new BehaviorSubject<Collection[]>([]);
  collectionsByUser$ = this.collectionsByUserSubject.asObservable();

  successMessage$ = new BehaviorSubject<string | null>(null);
  errorMessage$ = new BehaviorSubject<string | null>(null);

  createCollection(collection: FormData):Observable<Collection>{
    // return this.http.post<Collection>(`${environment.apiUrl}/collections`, collection);
    return this.http.post<Collection>(`${environment.apiUrl}/collections`, collection).pipe(
      tap(newCollection => {
        console.log('New collection added:', newCollection); // Debug log
        const updated = [...this.collectionsByUserSubject.value, newCollection];
        this.collectionsByUserSubject.next(updated);
      })
      // REMOVED: catchError (let errors propagate to subscriber)
    );
  }

  getByUserId(userId: string){
    // return this.http.get<Collection[]>(`${environment.apiUrl}/collections/user/` + userId);
    return this.http.get<Collection[]>(`${environment.apiUrl}/collections/user/` + userId).pipe(
      tap(collectionsByUser => this.collectionsByUserSubject.next(collectionsByUser)),
      catchError(err => {
        console.error('Error loading collections', err);
        this.collectionsByUserSubject.next([]); // fallback
        return of([]);
      })
    );
  }

  deleteCollection(id: string){
    // return this.http.delete(`${environment.apiUrl}/collections/` + id);
    return this.http.delete(`${environment.apiUrl}/collections/` + id).pipe(
      tap(() => {
        // оновлюємо локальний state без повторного запиту
        const updated = this.collectionsByUserSubject.value.filter(c => c.id !== id);
        this.collectionsByUserSubject.next(updated);
      }),
      catchError(err => {
        console.error('Error deleting collection', err);
        return of(null);
      })
    );
  }


  addRecipeToCollection(collectionId: string, recipeId: string): Observable<Collection> {
    return this.http.post<Collection>(
      `${environment.apiUrl}/collections/${collectionId}/recipes`,
      { recipeId }
    ).pipe(
      tap(updated => {
        this.collectionsByUserSubject.next(
          this.collectionsByUserSubject.value.map(c => c.id === collectionId ? updated : c)
        );
      })
    );
  }


  removeRecipeFromCollection(collectionId: string, recipeId: string): Observable<Collection> {
    return this.http.delete<Collection>(
      `${environment.apiUrl}/collections/${collectionId}/recipes/${recipeId}`
    ).pipe(
      tap(updated => {
        this.collectionsByUserSubject.next(
          this.collectionsByUserSubject.value.map(c => c.id === collectionId ? updated : c)
        );
      })
    );
  }


  updateCollection(id: string, collection: FormData): Observable<Collection> {
    return this.http.put<Collection>(`${environment.apiUrl}/collections/${id}`, collection).pipe(
      tap(updatedCollection => {
        console.log('Collection updated:', updatedCollection);
        const updated = this.collectionsByUserSubject.value.map(c => 
          c.id === id ? updatedCollection : c
        );
        this.collectionsByUserSubject.next(updated);
        this.successMessage$.next('Колекцію успішно оновлено!');
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