import { Routes } from '@angular/router';
import { authGuard, guestGuard, adminGuard, managerGuard, userGuard, roomGuard } from './guards/authguard';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },

  // ── Public ────────────────────────────────────────────────────────────────
  { path: 'home',       loadComponent: () => import('./home/home').then(m => m.Home) },
  { path: 'hotels',     loadComponent: () => import('./hotels/hotels').then(m => m.Hotels) },
  { path: 'hotels/:id', loadComponent: () => import('./hotel-detail/hotel-detail').then(m => m.HotelDetail) },
  { path: 'amenities',  loadComponent: () => import('./amenities/amenities').then(m => m.Amenities) },

  // ── Guest only ────────────────────────────────────────────────────────────
  { path: 'login',    canActivate: [guestGuard], loadComponent: () => import('./login/login').then(m => m.Login) },
  { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./register/register').then(m => m.Register) },

  // ── User only ─────────────────────────────────────────────────────────────
  { path: 'dashboard-user',           canActivate: [userGuard], loadComponent: () => import('./dashboard-user/dashboard-user').then(m => m.DashboardUser) },
  { path: 'booking/:hotelId/:roomId', canActivate: [userGuard], loadComponent: () => import('./booking/booking').then(m => m.Booking) },
  { path: 'payment/:bookingId',       canActivate: [userGuard], loadComponent: () => import('./payment/payment').then(m => m.Payment) },
  { path: 'wishlist',                 canActivate: [userGuard], loadComponent: () => import('./wishlist/wishlist').then(m => m.Wishlist) },
  { path: 'notifications',            canActivate: [userGuard], loadComponent: () => import('./notifications/notifications').then(m => m.Notifications) },

  // ── Any logged-in user ────────────────────────────────────────────────────
  { path: 'cancellations', canActivate: [authGuard], loadComponent: () => import('./cancellation/cancellation').then(m => m.Cancellation) },
  { path: 'reviews',       canActivate: [authGuard], loadComponent: () => import('./review/review').then(m => m.Review) },
  { path: 'profile',       canActivate: [authGuard], loadComponent: () => import('./profile/profile').then(m => m.Profile) },

  // ── Admin OR HotelManager ─────────────────────────────────────────────────
  { path: 'rooms',           canActivate: [roomGuard], loadComponent: () => import('./room/room').then(m => m.Room) },
  { path: 'hotel-amenity',   canActivate: [roomGuard], loadComponent: () => import('./hotel-amenity/hotel-amenity').then(m => m.HotelAmenity) },

  // ── Admin only ────────────────────────────────────────────────────────────
  { path: 'dashboard-admin',      canActivate: [adminGuard], loadComponent: () => import('./dashboard-admin/dashboard-admin').then(m => m.DashboardAdmin) },
  { path: 'audit-log',            canActivate: [adminGuard], loadComponent: () => import('./audit-log/audit-log').then(m => m.AuditLog) },

  // ── HotelManager only ─────────────────────────────────────────────────────
  { path: 'dashboard-manager', canActivate: [managerGuard], loadComponent: () => import('./dashboard-manager/dashboard-manager').then(m => m.DashboardManager) },

  { path: '**', redirectTo: 'home' }
];
