import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../services/token.service';
import { ToastrService } from 'ngx-toastr';

export const authGuard: CanActivateFn = () => {
  const ts = inject(TokenService), r = inject(Router), t = inject(ToastrService);
  if (ts.isLoggedIn()) return true;
  t.warning('Please login to access this page.', 'Login Required');
  r.navigateByUrl('/login'); return false;
};

export const userGuard: CanActivateFn = () => {
  const ts = inject(TokenService), r = inject(Router), t = inject(ToastrService);
  if (!ts.isLoggedIn()) { t.warning('Please login.', 'Login Required'); r.navigateByUrl('/login'); return false; }
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

export const adminGuard: CanActivateFn = () => {
  const ts = inject(TokenService), r = inject(Router), t = inject(ToastrService);
  if (!ts.isLoggedIn()) { t.warning('Please login.', 'Login Required'); r.navigateByUrl('/login'); return false; }
  const role = ts.getRoleFromToken();
  if (role === 'admin') return true;
  t.error('Admin access required.', 'Access Denied');
  if (role === 'hotelmanager') r.navigateByUrl('/dashboard-manager');
  else                         r.navigateByUrl('/dashboard-user');
  return false;
};

export const managerGuard: CanActivateFn = () => {
  const ts = inject(TokenService), r = inject(Router), t = inject(ToastrService);
  if (!ts.isLoggedIn()) { t.warning('Please login.', 'Login Required'); r.navigateByUrl('/login'); return false; }
  const role = ts.getRoleFromToken();
  if (role === 'hotelmanager') return true;
  t.error('Hotel Manager access required.', 'Access Denied');
  if (role === 'admin') r.navigateByUrl('/dashboard-admin');
  else                  r.navigateByUrl('/dashboard-user');
  return false;
};

export const roomGuard: CanActivateFn = () => {
  const ts = inject(TokenService), r = inject(Router), t = inject(ToastrService);
  if (!ts.isLoggedIn()) { t.warning('Please login.', 'Login Required'); r.navigateByUrl('/login'); return false; }
  const role = ts.getRoleFromToken();
  if (role === 'admin' || role === 'hotelmanager') return true;
  t.error('Admin or Hotel Manager access required.', 'Access Denied');
  r.navigateByUrl('/dashboard-user'); return false;
};
