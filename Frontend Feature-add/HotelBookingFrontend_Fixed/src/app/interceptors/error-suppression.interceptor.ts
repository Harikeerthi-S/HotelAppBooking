import { HttpInterceptorFn } from '@angular/common/http';
import { catchError, of } from 'rxjs';

export const errorSuppressionInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError(error => {
      // Suppress 403 errors for hotel amenity endpoints to avoid console noise
      if (error.status === 403 && 
          (req.url.includes('/hotelamenity') || req.url.includes('/hotel/') && req.url.includes('/amenities'))) {
        console.log('Hotel amenity access restricted - continuing without amenity filtering');
        // Return empty response to prevent console errors
        return of({
          body: [],
          status: 200,
          statusText: 'OK',
          headers: error.headers,
          url: error.url
        } as any);
      }
      
      // For all other errors, let them through normally
      throw error;
    })
  );
};