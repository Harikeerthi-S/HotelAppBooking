import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { authInterceptor } from './authInterceptor';
import { routes } from '../app.routes';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    });
    http     = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    sessionStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('should not attach Authorization header when no token', () => {
    http.get('/api/test').subscribe();
    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('should attach Authorization header when token exists', () => {
    sessionStorage.setItem('token', 'my-jwt-token');
    http.get('/api/test').subscribe();
    const req = httpMock.expectOne('/api/test');
    expect(req.request.headers.has('Authorization')).toBeTrue();
    expect(req.request.headers.get('Authorization')).toBe('Bearer my-jwt-token');
    req.flush({});
  });

  it('should send correct Bearer prefix with token', () => {
    sessionStorage.setItem('token', 'abc123');
    http.get('/api/hotels').subscribe();
    const req = httpMock.expectOne('/api/hotels');
    const authHeader = req.request.headers.get('Authorization');
    expect(authHeader).toMatch(/^Bearer /);
    req.flush([]);
  });

  it('should pass through POST requests without token', () => {
    http.post('/api/auth/login', { email: 'a@a.com' }).subscribe();
    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({ token: 'new-token' });
  });

  it('should attach token to POST requests when token exists', () => {
    sessionStorage.setItem('token', 'valid-token');
    http.post('/api/booking', {}).subscribe();
    const req = httpMock.expectOne('/api/booking');
    expect(req.request.headers.get('Authorization')).toBe('Bearer valid-token');
    req.flush({ bookingId: 1 });
  });

  it('should attach token to PUT requests', () => {
    sessionStorage.setItem('token', 'put-token');
    http.put('/api/booking/1/confirm', {}).subscribe();
    const req = httpMock.expectOne('/api/booking/1/confirm');
    expect(req.request.headers.get('Authorization')).toBe('Bearer put-token');
    req.flush({});
  });

  it('should attach token to DELETE requests', () => {
    sessionStorage.setItem('token', 'del-token');
    http.delete('/api/hotel/1').subscribe();
    const req = httpMock.expectOne('/api/hotel/1');
    expect(req.request.headers.get('Authorization')).toBe('Bearer del-token');
    req.flush({});
  });

  it('should clear session and redirect on 401 error', () => {
    sessionStorage.setItem('token', 'expired-token');
    sessionStorage.setItem('hotel_user', JSON.stringify({ userId: 1 }));

    http.get('/api/protected').subscribe({ error: () => {} });
    const req = httpMock.expectOne('/api/protected');
    req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(sessionStorage.getItem('token')).toBeNull();
    expect(sessionStorage.getItem('hotel_user')).toBeNull();
  });
});
