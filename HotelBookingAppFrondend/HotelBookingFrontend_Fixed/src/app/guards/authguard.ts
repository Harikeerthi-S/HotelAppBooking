import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { TokenService } from '../services/token.service';
import { ToastrService } from 'ngx-toastr';

const redirectToLogin = (r: Router, t: ToastrService, state: RouterStateSnapshot, msg = 'Please login to access this page.') => {
  t.warning(msg, 'Login Required');
  r.navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};

export const authGuard: CanActivateFn = (_route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const ts = inject(TokenService), r = inject(Router), t = inject(ToastrService);
  if (ts.isLoggedIn()) return true;
  return redirectToLogin(r, t, state);
};

export const userGuard: CanActivateFn = (_route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const ts = inject(TokenService), r = inject(Router), t = inject(ToastrService);
  if (!ts.isLoggedIn()) return redirectToLogin(r, t, state, 'Please login.');
  const role = ts.getRoleFromToken();
  if (role === 'user') return true;
  t.error('This area is for registered guests only.', 'Access Denied');
  if (role === 'admin')        { r.navigateByUrl('/dashboard-admin');   return false; }
  if (role === 'hotelmanager') { r.navigateByUrl('/dashboard-manager'); return false; }
  r.navigateByUrl('/home'); return false;
};

export const guestGuard: CanActivateFn = () => {
  const ts = inject(TokenService), r = inject(Router);
  if (!ts.isLoggedIn()) return true;
  const role = ts.getRoleFromToken();
  if (role === 'admin')             r.navigateByUrl('/dashboard-admin');
  else if (role === 'hotelmanager') r.navigateByUrl('/dashboard-manager');
  else                              r.navigateByUrl('/dashboard-user');
  return false;
};

export const adminGuard: CanActivateFn = (_route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const ts = inject(TokenService), r = inject(Router), t = inject(ToastrService);
  if (!ts.isLoggedIn()) return redirectToLogin(r, t, state, 'Please login.');
  const role = ts.getRoleFromToken();
  if (role === 'admin') return true;
  t.error('Admin access required.', 'Access Denied');
  if (role === 'hotelmanager') r.navigateByUrl('/dashboard-manager');
  else                         r.navigateByUrl('/dashboard-user');
  return false;
};

export const managerGuard: CanActivateFn = (_route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const ts = inject(TokenService), r = inject(Router), t = inject(ToastrService);
  if (!ts.isLoggedIn()) return redirectToLogin(r, t, state, 'Please login.');
  const role = ts.getRoleFromToken();
  if (role === 'hotelmanager') return true;
  t.error('Hotel Manager access required.', 'Access Denied');
  if (role === 'admin') r.navigateByUrl('/dashboard-admin');
  else                  r.navigateByUrl('/dashboard-user');
  return false;
};

export const roomGuard: CanActivateFn = (_route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const ts = inject(TokenService), r = inject(Router), t = inject(ToastrService);
  if (!ts.isLoggedIn()) return redirectToLogin(r, t, state, 'Please login.');
  const role = ts.getRoleFromToken();
  if (role === 'admin' || role === 'hotelmanager') return true;
  t.error('Admin or Hotel Manager access required.', 'Access Denied');
  r.navigateByUrl('/dashboard-user'); return false;
};
