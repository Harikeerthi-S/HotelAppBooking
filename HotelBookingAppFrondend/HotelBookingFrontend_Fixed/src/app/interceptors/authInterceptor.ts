import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { userLogout } from '../dynamicCommunication/userObservable';

// FIX: read token from localStorage (matches TokenService key 'hb_token')
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token  = localStorage.getItem('hb_token');
  const router = inject(Router);
  const toastr = inject(ToastrService);

  const authReq = token
    ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // Extract the most useful error message from backend
      const msg: string =
        error.error?.message ||
        error.error?.Message ||
        error.error?.title   ||
        (typeof error.error === 'string' ? error.error : '') ||
        error.message || 'An error occurred.';

      switch (error.status) {
        case 0:   toastr.error('Cannot connect to server. Make sure the backend is running.', 'Network Error'); break;
        case 400: toastr.error(msg, 'Bad Request (400)'); break;
        case 401:
          userLogout();
          toastr.warning('Session expired. Please login again.', 'Session Expired');
          router.navigateByUrl('/login');
          break;
        case 403: toastr.error('You do not have permission to perform this action.', 'Access Denied (403)'); break;
        case 404: toastr.error(msg || 'Resource not found.', 'Not Found (404)'); break;
        case 409: toastr.warning(msg || 'Conflict — record already exists.', 'Conflict (409)'); break;
        case 500: toastr.error('Server error. Please try again later.', 'Server Error (500)'); break;
        default:  toastr.error(msg, `Error ${error.status}`);
      }
      return throwError(() => error);
    })
  );
};
