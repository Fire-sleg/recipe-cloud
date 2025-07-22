import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';
import { tap } from 'rxjs/operators';
import { UserPreferences } from '../models/user-preferences.model';
import { ViewHistory } from '../models/view-history.model';
import { JwtHelperService } from '@auth0/angular-jwt';
import { Router } from '@angular/router';


@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  // public isAuthenticated$: Observable<boolean> = this.isAuthenticatedSubject.asObservable();
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    const user = localStorage.getItem('user');
    if (user) {
      this.checkTokenValidityOnInit(user);
      // this.currentUserSubject.next(JSON.parse(user));
    }
  }

  // login(username: string, password: string): Observable<{ token: string; user: User }> {
  //   return this.http.post<{ token: string; user: User }>(`${environment.authApiUrl}/auth/login`, { username, password })
  //     .pipe(
  //       tap(response => this.setUser(response.user, response.token))
  //     );
  // }
  getCurrentUserId(): string {
    const userJson = localStorage.getItem('user');
    const userId = userJson ? JSON.parse(userJson).id : null;
    return userId;
  }
  logView(recipeId: string): Observable<any> {
    debugger;
    return this.http.post(`${environment.authApiUrl}/view-history`, {recipeId});
  }



  login(email: string, password: string): Observable<any> {
    return this.http.post<any>(
      `${environment.authApiUrl}/auth/login`,
      { email, password }
    ).pipe(
      tap(async response => {
        const user: User = {
          id: response.id,
          username: response.username,
          role: response.role,
          isAdmin: response.role === 'Admin'
        };
        const token = response.tokenModel.accessToken;
        this.setUser(user, token);
        // Стягування преференсів після логіну
        const preferences = await firstValueFrom(this.getPreferences(user.id));
        localStorage.setItem('preferences', JSON.stringify(preferences));
      })
    );
  }
  getPreferences(userId: string): Observable<UserPreferences> {
    debugger;
    console.log(`${environment.authApiUrl}/preferences/${userId}`);
    return this.http.get<UserPreferences>(`${environment.authApiUrl}/preferences/${userId}`);
  }
  savePreferences(prefs: UserPreferences): Observable<void> {
    return this.http.post<void>(`${environment.authApiUrl}/preferences`, prefs);
  }



  register(username: string, email: string, password: string): Observable<User> {
    debugger;
    return this.http.post<User>(`${environment.authApiUrl}/auth/register`, { username, email, password });
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  setUser(user: User, token: string): void {
    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  isAdmin(): boolean {
    const user = this.currentUserSubject.value;
    return user?.isAdmin || false;
  }



  isTokenValid(): boolean {
    const token = this.getToken();
    if (!token) return false;

    try {
      const helper = new JwtHelperService();
      const decoded = helper.decodeToken(token);
      const currentTime = Math.floor(Date.now() / 1000);
      return decoded.exp ? decoded.exp > currentTime : false;
    } catch (error) {
      console.error('Error decoding token:', error);
      return false;
    }
  }

  private checkTokenValidityOnInit(user: string): void {
    if (this.isTokenValid()) {
      this.currentUserSubject.next(JSON.parse(user));
    } else {
      this.logout();
    }
  }
}