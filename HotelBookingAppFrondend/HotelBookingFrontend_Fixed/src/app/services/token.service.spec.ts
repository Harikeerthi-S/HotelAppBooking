import { TestBed } from '@angular/core/testing';
import { TokenService } from './token.service';

// A valid-looking JWT with role=admin, exp far in future
// Payload: { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "AdminUser",
//            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "admin",
//            "exp": 9999999999 }
const VALID_TOKEN =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.' +
  btoa(JSON.stringify({
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'AdminUser',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'admin',
    exp: 9999999999
  })).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_') +
  '.signature';

const EXPIRED_TOKEN =
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.' +
  btoa(JSON.stringify({
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'OldUser',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'user',
    exp: 1  // expired in 1970
  })).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_') +
  '.signature';

describe('TokenService', () => {
  let service: TokenService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenService);
    sessionStorage.clear();
  });

  afterEach(() => sessionStorage.clear());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getToken returns null when no token in sessionStorage', () => {
    expect(service.getToken()).toBeNull();
  });

  it('setToken stores token in sessionStorage', () => {
    service.setToken('my-token');
    expect(sessionStorage.getItem('token')).toBe('my-token');
  });

  it('getToken returns stored token', () => {
    service.setToken('my-token');
    expect(service.getToken()).toBe('my-token');
  });

  it('removeToken clears token from sessionStorage', () => {
    service.setToken('my-token');
    service.removeToken();
    expect(sessionStorage.getItem('token')).toBeNull();
  });

  it('isLoggedIn returns false when no token', () => {
    expect(service.isLoggedIn()).toBeFalse();
  });

  it('isLoggedIn returns false for expired token', () => {
    service.setToken(EXPIRED_TOKEN);
    expect(service.isLoggedIn()).toBeFalse();
  });

  it('isLoggedIn returns true for valid non-expired token', () => {
    service.setToken(VALID_TOKEN);
    expect(service.isLoggedIn()).toBeTrue();
  });

  it('getRoleFromToken returns null when no token', () => {
    expect(service.getRoleFromToken()).toBeNull();
  });

  it('getRoleFromToken returns admin from valid token', () => {
    service.setToken(VALID_TOKEN);
    expect(service.getRoleFromToken()).toBe('admin');
  });

  it('getUserNameFromToken returns empty string when no token', () => {
    expect(service.getUserNameFromToken()).toBe('');
  });

  it('getUserNameFromToken returns correct username from valid token', () => {
    service.setToken(VALID_TOKEN);
    expect(service.getUserNameFromToken()).toBe('AdminUser');
  });

  it('getRoleFromToken returns null for malformed token', () => {
    service.setToken('not.a.valid.token');
    expect(service.getRoleFromToken()).toBeNull();
  });

  it('isLoggedIn returns false for malformed token', () => {
    service.setToken('garbage');
    expect(service.isLoggedIn()).toBeFalse();
  });
});
