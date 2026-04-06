import { ApplicationConfig, provideBrowserGlobalErrorListeners, ErrorHandler, Injectable } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { authInterceptor } from './interceptors/authInterceptor';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  handleError(error: any): void {
    // Log the error for debugging
    console.error('Global error caught:', error);
    
    // Don't show user-facing errors for expected API failures
    const isExpectedError = error?.message?.includes('403') || 
                           error?.message?.includes('Forbidden') ||
                           error?.message?.includes('hotelamenity') ||
                           error?.message?.includes('amenities');
    
    if (!isExpectedError) {
      // Only log unexpected errors
      console.error('Unexpected runtime error:', error);
    }
  }
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimations(),
    provideToastr({
      timeOut: 3000,
      positionClass: 'toast-top-right',
      progressBar: true,
      closeButton: true,
      preventDuplicates: true
    }),
    { provide: ErrorHandler, useClass: GlobalErrorHandler }
  ]
};
