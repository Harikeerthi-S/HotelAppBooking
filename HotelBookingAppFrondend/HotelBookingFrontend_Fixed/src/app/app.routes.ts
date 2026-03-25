import { Routes } from '@angular/router';
import { authGuard, guestGuard, adminGuard, managerGuard, userGuard, roomGuard } from './guards/authguard';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },

  // ── Public ────────────────────────────────────────────────────────────────
  { path: 'home',       loadComponent: () => import('./home/home').then(m => m.Home) },
  { path: 'hotels',     loadComponent: () => import('./hotels/hotels').then(m => m.Hotels) },
  { path: 'hotels/:id', loadComponent: () => import('./hotel-detail/hotel-detail').then(m => m.HotelDetail) },

  // Amenities — public browsing; admin actions handled inside component
  { path: 'amenities',  loadComponent: () => import('./amenities/amenities').then(m => m.Amenities) },

  // ── Guest only ────────────────────────────────────────────────────────────
  { path: 'login',    canActivate: [guestGuard], loadComponent: () => import('./login/login').then(m => m.Login) },
  { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./register/register').then(m => m.Register) },

  // ── User only ─────────────────────────────────────────────────────────────
  { path: 'dashboard-user',           canActivate: [userGuard], loadComponent: () => import('./dashboard-user/dashboard-user').then(m => m.DashboardUser) },
  { path: 'booking/:hotelId/:roomId', canActivate: [userGuard], loadComponent: () => import('./booking/booking').then(m => m.Booking) },
  { path: 'payment/:bookingId',       canActivate: [userGuard], loadComponent: () => import('./payment/payment').then(m => m.Payment) },
  { path: 'wishlist',                 canActivate: [userGuard], loadComponent: () => import('./wishlist/wishlist').then(m => m.Wishlist) },
  { path: 'booking-room/:bookingId',  canActivate: [userGuard], loadComponent: () => import('./booking-room/booking-room').then(m => m.BookingRoom) },
  // notifications backend is [Authorize(Roles="user")] — must use userGuard
  { path: 'notifications',            canActivate: [userGuard], loadComponent: () => import('./notifications/notifications').then(m => m.Notifications) },
  // cancellations — user creates; admin views/updates; authGuard lets both roles in
  { path: 'cancellations',            canActivate: [authGuard], loadComponent: () => import('./cancellation/cancellation').then(m => m.Cancellation) },

  // ── Admin only ────────────────────────────────────────────────────────────
  { path: 'dashboard-admin', canActivate: [adminGuard], loadComponent: () => import('./dashboard-admin/dashboard-admin').then(m => m.DashboardAdmin) },
  { path: 'hotel-amenity',   canActivate: [adminGuard], loadComponent: () => import('./hotel-amenity/hotel-amenity').then(m => m.HotelAmenity) },

  // ── Admin OR HotelManager ─────────────────────────────────────────────────
  { path: 'rooms', canActivate: [roomGuard], loadComponent: () => import('./room/room').then(m => m.Room) },

  // ── HotelManager only ─────────────────────────────────────────────────────
  { path: 'dashboard-manager', canActivate: [managerGuard], loadComponent: () => import('./dashboard-manager/dashboard-manager').then(m => m.DashboardManager) },

  // ── Any logged-in user ────────────────────────────────────────────────────
  { path: 'profile', canActivate: [authGuard], loadComponent: () => import('./profile/profile').then(m => m.Profile) },

  { path: '**', redirectTo: 'home' }
];
