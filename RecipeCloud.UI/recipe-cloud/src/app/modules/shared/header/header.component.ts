import { Component } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { Observable } from 'rxjs';
import { Notification} from '../../../core/models/notification.model';
import { map } from 'rxjs/operators';
import { User } from '../../../core/models/user.model';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent {
  isAuthenticated$: Observable<boolean>;
  currentUser$: Observable<User | null>;
  notifications$!: Observable<Notification[]>;
  unreadCount$!: Observable<number>;

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService,
    private router: Router
  ) {
    this.isAuthenticated$ = this.authService.currentUser$.pipe(map(user => !!user));
    this.currentUser$ = this.authService.currentUser$;
    this.notifications$ = this.notificationService.getNotifications(1, 10);
    this.unreadCount$ = this.notifications$.pipe(
      map(notifications => notifications.filter(n => !n.isRead).length)
    );
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}