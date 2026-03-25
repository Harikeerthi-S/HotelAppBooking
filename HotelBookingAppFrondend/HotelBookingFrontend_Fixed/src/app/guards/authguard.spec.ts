import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { authGuard, guestGuard, adminGuard, managerGuard, userGuard } from './authguard';
import { TokenService } from '../services/token.service';
import { routes } from '../app.routes';

const makeToken = (role: string) =>
  'eyJ.' +
  btoa(JSON.stringify({
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'TestUser',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
    exp: 9999999999
  })).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_') +
  '.sig';

const mockRoute = {} as ActivatedRouteSnapshot;
const mockState = {} as RouterStateSnapshot;

describe('Guards', () => {
  let tokenService: TokenService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    });
    tokenService = TestBed.inject(TokenService);
    router       = TestBed.inject(Router);
    sessionStorage.clear();
  });

  afterEach(() => sessionStorage.clear());

  // ── authGuard ─────────────────────────────────────────────────────────
  describe('authGuard — any logged-in user', () => {
    it('should return false and redirect to /login when not logged in', () => {
      const spy = spyOn(router, 'navigateByUrl');
      const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));
      expect(result).toBeFalse();
      expect(spy).toHaveBeenCalledWith('/login');
    });

    it('should return true for role user', () => {
      tokenService.setToken(makeToken('user'));
      const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));
      expect(result).toBeTrue();
    });

    it('should return true for role admin', () => {
      tokenService.setToken(makeToken('admin'));
      const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));
      expect(result).toBeTrue();
    });

    it('should return true for role hotelmanager', () => {
      tokenService.setToken(makeToken('hotelmanager'));
      const result = TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState));
      expect(result).toBeTrue();
    });
  });

  // ── userGuard ─────────────────────────────────────────────────────────
  describe('userGuard — only role user', () => {
    it('should return false and redirect to /login when not logged in', () => {
      const spy = spyOn(router, 'navigateByUrl');
      const result = TestBed.runInInjectionContext(() => userGuard(mockRoute, mockState));
      expect(result).toBeFalse();
      expect(spy).toHaveBeenCalledWith('/login');
    });

    it('should return true for role user', () => {
      tokenService.setToken(makeToken('user'));
      const result = TestBed.runInInjectionContext(() => userGuard(mockRoute, mockState));
      expect(result).toBeTrue();
    });

    it('should return false for admin and redirect to /dashboard-admin', () => {
      tokenService.setToken(makeToken('admin'));
      const spy = spyOn(router, 'navigateByUrl');
      const result = TestBed.runInInjectionContext(() => userGuard(mockRoute, mockState));
      expect(result).toBeFalse();
      expect(spy).toHaveBeenCalledWith('/dashboard-admin');
    });

    it('should return false for hotelmanager and redirect to /dashboard-manager', () => {
      tokenService.setToken(makeToken('hotelmanager'));
      const spy = spyOn(router, 'navigateByUrl');
      const result = TestBed.runInInjectionContext(() => userGuard(mockRoute, mockState));
      expect(result).toBeFalse();
      expect(spy).toHaveBeenCalledWith('/dashboard-manager');
    });
  });

  // ── guestGuard ────────────────────────────────────────────────────────
  describe('guestGuard — redirects logged-in users to their dashboard', () => {
    it('should return true when not logged in', () => {
      const result = TestBed.runInInjectionContext(() => guestGuard(mockRoute, mockState));
      expect(result).toBeTrue();
    });

    it('should redirect admin to /dashboard-admin', () => {
      tokenService.setToken(makeToken('admin'));
      const spy = spyOn(router, 'navigateByUrl');
      TestBed.runInInjectionContext(() => guestGuard(mockRoute, mockState));
      expect(spy).toHaveBeenCalledWith('/dashboard-admin');
    });

    it('should redirect hotelmanager to /dashboard-manager', () => {
      tokenService.setToken(makeToken('hotelmanager'));
      const spy = spyOn(router, 'navigateByUrl');
      TestBed.runInInjectionContext(() => guestGuard(mockRoute, mockState));
      expect(spy).toHaveBeenCalledWith('/dashboard-manager');
    });

    it('should redirect user to /dashboard-user', () => {
      tokenService.setToken(makeToken('user'));
      const spy = spyOn(router, 'navigateByUrl');
      TestBed.runInInjectionContext(() => guestGuard(mockRoute, mockState));
      expect(spy).toHaveBeenCalledWith('/dashboard-user');
    });
  });

  // ── adminGuard ────────────────────────────────────────────────────────
  describe('adminGuard — only role admin', () => {
    it('should return false and redirect to /login when not logged in', () => {
      const spy = spyOn(router, 'navigateByUrl');
      const result = TestBed.runInInjectionContext(() => adminGuard(mockRoute, mockState));
      expect(result).toBeFalse();
      expect(spy).toHaveBeenCalledWith('/login');
    });

    it('should return true for admin', () => {
      tokenService.setToken(makeToken('admin'));
      const result = TestBed.runInInjectionContext(() => adminGuard(mockRoute, mockState));
      expect(result).toBeTrue();
    });

    it('should return false for user and redirect to /dashboard-user', () => {
      tokenService.setToken(makeToken('user'));
      const spy = spyOn(router, 'navigateByUrl');
      const result = TestBed.runInInjectionContext(() => adminGuard(mockRoute, mockState));
      expect(result).toBeFalse();
      expect(spy).toHaveBeenCalledWith('/dashboard-user');
    });

    it('should return false for hotelmanager and redirect to /dashboard-manager', () => {
      tokenService.setToken(makeToken('hotelmanager'));
      const spy = spyOn(router, 'navigateByUrl');
      const result = TestBed.runInInjectionContext(() => adminGuard(mockRoute, mockState));
      expect(result).toBeFalse();
      expect(spy).toHaveBeenCalledWith('/dashboard-manager');
    });
  });

  // ── managerGuard ──────────────────────────────────────────────────────
  describe('managerGuard — only role hotelmanager', () => {
    it('should return false and redirect to /login when not logged in', () => {
      const spy = spyOn(router, 'navigateByUrl');
      const result = TestBed.runInInjectionContext(() => managerGuard(mockRoute, mockState));
      expect(result).toBeFalse();
      expect(spy).toHaveBeenCalledWith('/login');
    });

    it('should return true for hotelmanager', () => {
      tokenService.setToken(makeToken('hotelmanager'));
      const result = TestBed.runInInjectionContext(() => managerGuard(mockRoute, mockState));
      expect(result).toBeTrue();
    });

    it('should return false for admin and redirect to /dashboard-admin', () => {
      tokenService.setToken(makeToken('admin'));
      const spy = spyOn(router, 'navigateByUrl');
      const result = TestBed.runInInjectionContext(() => managerGuard(mockRoute, mockState));
      expect(result).toBeFalse();
      expect(spy).toHaveBeenCalledWith('/dashboard-admin');
    });

    it('should return false for user and redirect to /dashboard-user', () => {
      tokenService.setToken(makeToken('user'));
      const spy = spyOn(router, 'navigateByUrl');
      const result = TestBed.runInInjectionContext(() => managerGuard(mockRoute, mockState));
      expect(result).toBeFalse();
      expect(spy).toHaveBeenCalledWith('/dashboard-user');
    });
  });
});
