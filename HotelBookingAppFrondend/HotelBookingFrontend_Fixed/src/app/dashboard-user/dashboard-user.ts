import { Component, inject, signal, computed, OnDestroy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of, Subscription } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { BookingModel } from '../models/booking.model';
import { CancellationModel } from '../models/cancellation.model';
import { NotificationModel } from '../models/notification.model';
import { PaymentModel } from '../models/payment.model';
import { ReviewModel } from '../models/review.model';
import { AmenityModel } from '../models/amenity.model';
import { PagedResponse } from '../models/paged.model';
import { $userStatus, UserState } from '../dynamicCommunication/userObservable';
import { TokenService } from '../services/token.service';

type DashTab = 'bookings' | 'payments' | 'cancellations' | 'notifications' | 'reviews' | 'amenities';

@Component({
  selector: 'app-dashboard-user',
  standalone: true,
  imports: [RouterLink, CommonModule, DatePipe, FormsModule],
  templateUrl: './dashboard-user.html',
  styleUrl: './dashboard-user.css'
})
export class DashboardUser implements OnDestroy {
  private api   = inject(APIService);
  private ts    = inject(TokenService);
  private toast = inject(ToastrService);

  private _user = signal<UserState>({ userId: 0, userName: '', email: '', role: '' });
  private sub: Subscription;

  private get _uid():  number { return this._user().userId; }
  private get _name(): string { return this._user().userName; }

  get userName(): string { return this._name; }

  activeTab = signal<DashTab>('bookings');

  // ── Bookings ───────────────────────────────────────────────────────────────
  bookings       = signal<BookingModel[]>([]);
  bookingLoading = signal(false);
  bookingPage    = signal(1);
  pagedBookings  = signal<PagedResponse<BookingModel> | null>(null);

  // ── Payments ──────────────────────────────────────────────────────────────
  payments   = signal<PaymentModel[]>([]);
  payLoading = signal(false);
  payLoaded  = signal(false);

  // ── Cancellations ──────────────────────────────────────────────────────────
  cancellations    = signal<CancellationModel[]>([]);
  cancelLoading    = signal(false);
  eligibleBookings = signal<BookingModel[]>([]);
  formBookingId    = signal(0);
  formReason       = signal('');
  showCancelForm   = signal(false);
  cancelSubmitting = signal(false);

  // ── Notifications ─────────────────────────────────────────────────────────
  notifications = signal<NotificationModel[]>([]);
  notifLoading  = signal(false);

  // ── Reviews ───────────────────────────────────────────────────────────────
  reviews       = signal<ReviewModel[]>([]);
  reviewLoading = signal(false);
  reviewLoaded  = signal(false);

  // ── Amenities ─────────────────────────────────────────────────────────────
  amenities = signal<AmenityModel[]>([]);

  // ── Computed stats ─────────────────────────────────────────────────────────
  totalBookings     = computed(() => this.bookings().length);
  confirmedBookings = computed(() => this.bookings().filter(b => b.status === 'Confirmed').length);
  pendingBookings   = computed(() => this.bookings().filter(b => b.status === 'Pending').length);
  totalSpent        = computed(() => this.bookings().reduce((s, b) => s + (b.totalAmount ?? 0), 0));
  unreadCount       = computed(() => this.notifications().filter(n => !n.isRead).length);
  totalRefund       = computed(() =>
    this.cancellations()
      .filter(c => c.status === 'Approved')
      .reduce((s, c) => s + (c.refundAmount ?? 0), 0)
  );

  constructor() {
    this.sub = $userStatus.subscribe(u => {
      this._user.set(u);
      if (u.userId > 0) {
        this.loadBookings(1);
        this.loadNotifications();
        this.loadCancellations();
        this.loadEligibleBookings();
        this.loadAmenities();
      }
    });
  }

  ngOnDestroy(): void { this.sub.unsubscribe(); }

  switchTab(tab: string): void {
    this.activeTab.set(tab as DashTab);
    if (tab === 'payments' && !this.payLoaded())    this.loadPayments();
    if (tab === 'reviews'  && !this.reviewLoaded()) this.loadReviews();
  }

  // ── Loaders ────────────────────────────────────────────────────────────────
  loadBookings(p: number): void {
    const uid = this._uid;
    if (!uid) return;
    this.bookingLoading.set(true);
    this.bookingPage.set(p);
    this.api.apiGetBookingsByUser(uid, { pageNumber: p, pageSize: 5 }).subscribe({
      next: res => {
        this.bookings.set(res.data ?? []);
        this.pagedBookings.set(res);
        this.bookingLoading.set(false);
      },
      error: () => this.bookingLoading.set(false)
    });
  }

  loadEligibleBookings(): void {
    const uid = this._uid;
    if (!uid) return;
    this.api.apiGetBookingsByUser(uid, { pageNumber: 1, pageSize: 50 }).subscribe({
      next: res => this.eligibleBookings.set(
        (res.data ?? []).filter(b => b.status === 'Pending' || b.status === 'Confirmed')
      ),
      error: () => {}
    });
  }

  /**
   * FIX: User has no list-all-payments endpoint.
   * Correct approach: load all user bookings → for each booking that has a
   * confirmed/completed status, call GET /api/payment/booking/{bookingId}
   * to find its payment. This uses the new backend endpoint added for this purpose.
   */
  loadPayments(): void {
    const uid = this._uid;
    if (!uid) return;
    this.payLoading.set(true);
    this.payLoaded.set(true);

    this.api.apiGetBookingsByUser(uid, { pageNumber: 1, pageSize: 50 }).subscribe({
      next: res => {
        // Any booking that was ever paid can have a payment record
        const allBookings = res.data ?? [];
        if (!allBookings.length) { this.payLoading.set(false); return; }

        // Fetch payment for every booking (null if not found yet)
        const reqs = allBookings.map(b =>
          this.api.apiGetPaymentByBookingId(b.bookingId).pipe(
            catchError(() => of(null))
          )
        );
        forkJoin(reqs).subscribe({
          next: results => {
            const found = results.filter((p): p is PaymentModel => p !== null);
            this.payments.set(found);
            this.payLoading.set(false);
          },
          error: () => this.payLoading.set(false)
        });
      },
      error: () => this.payLoading.set(false)
    });
  }

  loadCancellations(): void {
    const uid = this._uid;
    if (!uid) return;
    this.cancelLoading.set(true);
    this.api.apiGetCancellationsByUser(uid, { pageNumber: 1, pageSize: 20 }).subscribe({
      next: res => { this.cancellations.set(res.data ?? []); this.cancelLoading.set(false); },
      error: () => this.cancelLoading.set(false)
    });
  }

  loadNotifications(): void {
    this.notifLoading.set(true);
    this.api.apiGetMyNotifications().subscribe({
      next: list => {
        this.notifications.set(
          [...(list ?? [])].sort((a, b) =>
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
          )
        );
        this.notifLoading.set(false);
      },
      error: () => this.notifLoading.set(false)
    });
  }

  loadReviews(): void {
    const uid = this._uid;
    if (!uid) return;
    this.reviewLoading.set(true);
    this.reviewLoaded.set(true);
    // Get bookings → unique hotel IDs → load reviews filtered by this userId
    this.api.apiGetBookingsByUser(uid, { pageNumber: 1, pageSize: 50 }).subscribe({
      next: res => {
        const hotelIds = [...new Set((res.data ?? []).map(b => b.hotelId))];
        if (!hotelIds.length) { this.reviewLoading.set(false); return; }
        const reqs = hotelIds.map(hId =>
          this.api.apiGetReviewsPaged(hId, { pageNumber: 1, pageSize: 50 }).pipe(
            map(r => (r.data ?? []).filter(rv => rv.userId === uid)),
            catchError(() => of([] as ReviewModel[]))
          )
        );
        forkJoin(reqs).subscribe({
          next: results => { this.reviews.set(results.flat()); this.reviewLoading.set(false); },
          error: () => this.reviewLoading.set(false)
        });
      },
      error: () => this.reviewLoading.set(false)
    });
  }

  loadAmenities(): void {
    this.api.apiGetAmenities().subscribe({ next: a => this.amenities.set(a ?? []), error: () => {} });
  }

  // ── Booking actions ────────────────────────────────────────────────────────
  cancelBookingDirect(b: BookingModel): void {
    if (!confirm('Cancel this booking?')) return;
    this.api.apiCancelBooking(b.bookingId).subscribe({
      next: () => {
        this.bookings.update(l => l.map(x =>
          x.bookingId === b.bookingId ? { ...x, status: 'Cancelled' } : x
        ));
        this.toast.success('Booking cancelled.');
      },
      error: e => this.toast.error(e?.error?.message || 'Failed to cancel booking.', 'Error')
    });
  }

  openCancelForm(b: BookingModel): void {
    this.formBookingId.set(b.bookingId);
    this.formReason.set('');
    this.showCancelForm.set(true);
  }
  closeCancelForm(): void { this.showCancelForm.set(false); }

  submitCancellation(): void {
    if (!this.formReason().trim())           { this.toast.warning('Please enter a reason.'); return; }
    if (this.formReason().trim().length < 5) { this.toast.warning('Reason must be at least 5 characters.'); return; }
    this.cancelSubmitting.set(true);
    this.api.apiCreateCancellation(this.formBookingId(), this.formReason().trim()).subscribe({
      next: () => {
        this.cancelSubmitting.set(false);
        this.showCancelForm.set(false);
        this.toast.success('Cancellation requested successfully.');
        this.loadCancellations();
        this.loadBookings(this.bookingPage());
        this.loadEligibleBookings();
      },
      error: e => {
        this.cancelSubmitting.set(false);
        this.toast.error(e?.error?.message || 'Failed to request cancellation.', 'Error');
      }
    });
  }

  // ── Notification actions ───────────────────────────────────────────────────
  markRead(n: NotificationModel): void {
    if (n.isRead) return;
    this.api.apiMarkNotificationRead(n.notificationId).subscribe({
      next: () => this.notifications.update(l =>
        l.map(x => x.notificationId === n.notificationId ? { ...x, isRead: true } : x)
      ),
      error: () => {}
    });
  }

  deleteNotif(n: NotificationModel): void {
    this.api.apiDeleteNotification(n.notificationId).subscribe({
      next: () => {
        this.notifications.update(l => l.filter(x => x.notificationId !== n.notificationId));
        this.toast.info('Notification deleted.');
      },
      error: () => {}
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  statusClass(s: string): string {
    return ({
      Pending:   'badge-pending',
      Confirmed: 'badge-confirmed',
      Completed: 'badge-completed',
      Cancelled: 'badge-cancelled',
      Refunded:  'badge-refunded'
    } as Record<string, string>)[s] ?? 'badge-pending';
  }

  cancelStatusClass(s: string): string {
    return ({ Pending: 'cs-pending', Approved: 'cs-approved', Rejected: 'cs-rejected' } as Record<string, string>)[s] ?? 'cs-pending';
  }

  payStatusClass(s: string): string {
    return ({ Completed: 'badge-confirmed', Pending: 'badge-pending', Failed: 'badge-cancelled', Refunded: 'badge-refunded' } as Record<string, string>)[s] ?? 'badge-pending';
  }

  bookingLabel(b: BookingModel): string {
    return `#${b.bookingId} — ${b.hotelName ?? 'Hotel'} (${b.status})`;
  }

  stars(n: number): boolean[] { return [1, 2, 3, 4, 5].map(s => s <= n); }
}
