import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Subscription } from '../models/subscription.model';
import { User } from '../models/user.model';
import { Recipe } from '../models/recipe.model';

@Injectable({
  providedIn: 'root'
})
export class SubscriptionService {
  constructor(private http: HttpClient) {}

  subscribe(followedId: string): Observable<Subscription> {
    return this.http.post<Subscription>(`${environment.apiUrl}/subscriptions`, { followedId });
  }

  unsubscribe(subscriptionId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/subscriptions/${subscriptionId}`);
  }

  getFollowers(userId: string, page: number, pageSize: number): Observable<User[]> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<User[]>(`${environment.apiUrl}/subscriptions/followers/${userId}`, { params });
  }

  getFollowing(userId: string, page: number, pageSize: number): Observable<User[]> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<User[]>(`${environment.apiUrl}/subscriptions/following/${userId}`, { params });
  }

  getSubscriptionFeed(page: number, pageSize: number): Observable<Recipe[]> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<Recipe[]>(`${environment.apiUrl}/recipe/subscriptions`, { params });
  }

  isSubscribed(userId: string): Observable<Subscription | null> {
    return this.http.get<Subscription>(`${environment.apiUrl}/subscriptions/check/${userId}`);
  }
}