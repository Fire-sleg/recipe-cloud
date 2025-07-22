import { inject, Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, Observable, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.authService.getToken();
    let authReq = req;


    if (token) {
      authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(authReq).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          this.authService.logout();
        }
        return throwError(() => error);
      })
    );
  }
}

// export function jwtInterceptorFn(req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> {
//   const authService = inject(AuthService);
//   const token = authService.getToken();

//   if (token) {
//     const cloned = req.clone({
//       setHeaders: {
//         Authorization: `Bearer ${token}`
//       }
//     });
//     return next(cloned);
//   }

//   return next(req);
// }