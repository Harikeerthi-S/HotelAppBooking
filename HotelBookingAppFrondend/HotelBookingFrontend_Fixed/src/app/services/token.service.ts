import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';

// JWT claims from JwtTokenHelper.cs:
//   new(ClaimTypes.NameIdentifier, userId.ToString())  → long XML key
//   new(ClaimTypes.Name, userName)                     → long XML key
//   new(ClaimTypes.Role, role.ToLower())               → long XML key
//   new(JwtRegisteredClaimNames.Sub, userId.ToString()) → "sub"
interface JwtPayload {
  // .NET ClaimTypes serialise to long XML namespace URLs in JWT
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'?: string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'?: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string;
  // Short aliases / fallbacks
  nameid?: string;
  sub?:    string;
  unique_name?: string;
  role?:   string;
  exp?:    number;
}

// FIX: localStorage persists across browser tab refresh; sessionStorage is wiped
const TOKEN_KEY = 'hb_token';

@Injectable({ providedIn: 'root' })
export class TokenService {

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  }

  removeToken(): void {
    localStorage.removeItem(TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const p = jwtDecode<JwtPayload>(token);
      if (!p.exp) return true;
      return Date.now() < p.exp * 1000;
    } catch { return false; }
  }

  // FIX: reads ClaimTypes.Role → long XML key, fallback to short "role"
  getRoleFromToken(): string | null {
    try {
      const token = this.getToken();
      if (!token) return null;
      const p = jwtDecode<JwtPayload>(token);
      return p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
          || p.role
          || null;
    } catch { return null; }
  }

  // FIX: reads ClaimTypes.Name → long XML key
  getUserNameFromToken(): string {
    try {
      const token = this.getToken();
      if (!token) return '';
      const p = jwtDecode<JwtPayload>(token);
      return p['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
          || p.unique_name
          || '';
    } catch { return ''; }
  }

  // FIX: reads ClaimTypes.NameIdentifier → long XML key, fallback to "sub"
  getUserIdFromToken(): number {
    try {
      const token = this.getToken();
      if (!token) return 0;
      const p = jwtDecode<JwtPayload>(token);
      const raw = p['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
               || p.nameid
               || p.sub
               || '0';
      return parseInt(raw, 10) || 0;
    } catch { return 0; }
  }
}
