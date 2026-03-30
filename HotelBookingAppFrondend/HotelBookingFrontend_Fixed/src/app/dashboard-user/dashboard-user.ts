import { Component, inject, signal, computed, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of, Subscription, interval } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
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
import { UserAmenityPreferenceModel } from '../models/user-amenity-preference.model';
type DashTab = 'bookings' | 'payments' | 'cancellations' | 'notifications' | 'reviews' | 'amenities' | 'support';

@Component({
  selector: 'app-dashboard-user',
  standalone: true,
  imports: [RouterLink, CommonModule, DatePipe, FormsModule],
  templateUrl: './dashboard-user.html',
  styleUrl: './dashboard-user.css'
})
export class DashboardUser implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('chatMsgContainer') private chatContainer!: ElementRef;
  private api   = inject(APIService);
  private toast = inject(ToastrService);

  private _user = signal<UserState>({ userId: 0, userName: '', email: '', role: '' });
  private sub: Subscription;
  private pollSub: Subscription | null = null;

  private get _uid():  number { return this._user().userId; }
  private get _name(): string { return this._user().userName; }

  get userName(): string { return this._name; }

  activeTab = signal<DashTab>('bookings');

  bookings       = signal<BookingModel[]>([]);
  bookingLoading = signal(false);
  bookingPage    = signal(1);
  pagedBookings  = signal<PagedResponse<BookingModel> | null>(null);
  bookingStats = signal<{ total: number; confirmed: number; pending: number; spent: number } | null>(null);
  roomDateMap = signal<Record<number, boolean>>({});

  payments   = signal<PaymentModel[]>([]);
  payLoading = signal(false);
  payLoaded  = signal(false);
  paymentsPage = signal(1);
  readonly PAY_PS = 5;
  pagedPayments = computed(() => this._clientSlice(this.payments(), this.paymentsPage(), this.PAY_PS));
  paymentsTotalPages = computed(() => Math.ceil(this.payments().length / this.PAY_PS) || 1);

  cancellations    = signal<CancellationModel[]>([]);
  cancelLoading    = signal(false);
  cancellationRefundTotal = signal<number | null>(null);
  cancelsPage = signal(1);
  readonly CANCEL_PS = 5;
  pagedCancels = computed(() => this._clientSlice(this.cancellations(), this.cancelsPage(), this.CANCEL_PS));
  cancelsTotalPages = computed(() => Math.ceil(this.cancellations().length / this.CANCEL_PS) || 1);

  eligibleBookings = signal<BookingModel[]>([]);
  formBookingId    = signal(0);
  formReason       = signal('');
  showCancelForm   = signal(false);
  cancelSubmitting = signal(false);

  notifications = signal<NotificationModel[]>([]);
  notifLoading  = signal(false);
  unreadNotifCount = signal(0);
  notifsPage = signal(1);
  readonly NOTIF_PS = 5;
  pagedNotifs = computed(() => this._clientSlice(this.notifications(), this.notifsPage(), this.NOTIF_PS));
  notifsTotalPages = computed(() => Math.ceil(this.notifications().length / this.NOTIF_PS) || 1);

  reviews       = signal<ReviewModel[]>([]);
  reviewLoading = signal(false);
  reviewLoaded  = signal(false);
  reviewsPage = signal(1);
  readonly REVIEW_PS = 5;
  pagedReviews = computed(() => this._clientSlice(this.reviews(), this.reviewsPage(), this.REVIEW_PS));
  reviewsTotalPages = computed(() => Math.ceil(this.reviews().length / this.REVIEW_PS) || 1);

  amenities     = signal<AmenityModel[]>([]);
  amenitySearch = signal('');
  amenitiesPage = signal(1);
  readonly AMENITY_PS = 8;
  filteredAmenities = computed(() => {
    const q = this.amenitySearch().toLowerCase().trim();
    if (!q) return this.amenities();
    return this.amenities().filter(a =>
      a.name.toLowerCase().includes(q) ||
      (a.description ?? '').toLowerCase().includes(q)
    );
  });
  pagedAmenities = computed(() => this._clientSlice(this.filteredAmenities(), this.amenitiesPage(), this.AMENITY_PS));
  amenitiesTotalPages = computed(() => Math.ceil(this.filteredAmenities().length / this.AMENITY_PS) || 1);

  private _clientSlice<T>(arr: T[], page: number, size: number): T[] {
    return arr.slice((page - 1) * size, page * size);
  }

  myPreferences     = signal<UserAmenityPreferenceModel[]>([]);
  prefLoading       = signal(false);
  prefSaving        = signal(false);
  selectedAmenityIds = computed(() => new Set(this.myPreferences().map(p => p.amenityId)));

  preferenceForAmenity(amenityId: number): UserAmenityPreferenceModel | undefined {
    return this.myPreferences().find(p => p.amenityId === amenityId);
  }

  preferenceStatusClass(status: string | undefined): string {
    const s = (status || 'Pending').toLowerCase();
    if (s === 'approved') return 'bg-success';
    if (s === 'rejected') return 'bg-danger';
    return 'bg-warning text-dark';
  }

  chatMessages  = signal<{ sender: 'user'|'bot'; text: string; time: Date }[]>([]);
  chatInput     = signal('');
  chatSending   = signal(false);
  chatSessionId = crypto.randomUUID();
  chatScrollNeeded = false;

  private readonly chatSuggestionsMap: Record<string, string[]> = {
    greeting:     ['How to book?', 'Cancellation policy', 'Payment methods'],
    booking:      ['Booking status', 'How to cancel?', 'Payment methods'],
    cancellation: ['Refund policy', 'How to book?', 'Contact support'],
    payment:      ['Payment failed?', 'Cancellation policy', 'How to book?'],
    hotel:        ['Check-in time', 'Hotel amenities', 'How to book?'],
    general:      ['How to book?', 'Cancellation policy', 'Hotel amenities'],
    support:      ['How to book?', 'Cancellation policy', 'Payment methods'],
  };
  chatLastIntent = signal('greeting');
  chatSuggestions = computed(() => this.chatSuggestionsMap[this.chatLastIntent()] ?? this.chatSuggestionsMap['general']);

  totalBookings     = computed(() => this.bookingStats()?.total ?? this.bookings().length);
  confirmedBookings = computed(() => this.bookingStats()?.confirmed ?? this.bookings().filter(b => b.status === 'Confirmed').length);
  pendingBookings   = computed(() => this.bookingStats()?.pending ?? this.bookings().filter(b => b.status === 'Pending').length);
  totalSpent        = computed(() => this.bookingStats()?.spent ?? this.bookings().reduce((s, b) => s + (b.totalAmount ?? 0), 0));

  unreadCount = this.unreadNotifCount;
  totalRefund       = computed(() => this.cancellationRefundTotal() ?? 0);

  constructor() {
    this.sub = $userStatus.subscribe(u => {
      this._user.set(u);
      if (u.userId > 0) {
        this.loadBookings(1);
        this.loadNotifications();
        this.loadCancellations();
        this.loadAmenities();
      }
    });
    this.chatMessages.set([{
      sender: 'bot',
      text: '👋 Hi! I\'m the StayEase support bot.\n\nI can help with **bookings**, **cancellations**, **hotel details**, and **payments**.\n\nWhat can I help you with?',
      time: new Date()
    }]);
  }

  ngAfterViewChecked(): void {
    if (this.chatScrollNeeded) {
      try {
        const el = this.chatContainer?.nativeElement;
        if (el) el.scrollTop = el.scrollHeight;
      } catch {}
      this.chatScrollNeeded = false;
    }
  }

  ngOnInit(): void {
    this.pollSub = interval(20000).pipe(
      switchMap(() => {
        const bks = this.bookings().filter(b => b.status === 'Pending' || b.status === 'Confirmed');
        if (!bks.length) return of([] as any[]);
        return forkJoin(bks.map(b =>
          this.api.apiCheckRoomAvailability(b.roomId, b.checkIn, b.checkOut).pipe(
            map(r => ({ bookingId: b.bookingId, isAvailable: r.isAvailable })),
            catchError(() => of(null))
          )
        ));
      })
    ).subscribe({
      next: results => {
        if (!Array.isArray(results)) return;
        const updated = { ...this.roomDateMap() };
        results.forEach(r => { if (r) updated[r.bookingId] = r.isAvailable; });
        this.roomDateMap.set(updated);
      },
      error: () => {}
    });
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
    this.pollSub?.unsubscribe();
  }

  switchTab(tab: string): void {
    this.activeTab.set(tab as DashTab);
    if (tab === 'payments'  && !this.payLoaded())    this.loadPayments();
    if (tab === 'reviews'   && !this.reviewLoaded()) this.loadReviews();
    if (tab === 'amenities')                         this.loadMyPreferences();
  }

  loadBookings(page: number = 1): void {
    const uid = this._uid;
    if (!uid) return;
    this.bookingLoading.set(true);
    this.bookingPage.set(page);
    this.api.apiGetBookingsByUser(uid, { pageNumber: page, pageSize: 5 }).subscribe({
      next: res => {
        const data = res.data ?? [];
        this.bookings.set(data);
        this.pagedBookings.set(res);
        this.bookingStats.set({
          total: res.totalRecords,
          confirmed: data.filter(b => b.status === 'Confirmed').length,
          pending:   data.filter(b => b.status === 'Pending').length,
          spent:     data.reduce((s, b) => s + (b.totalAmount ?? 0), 0)
        });
        this.eligibleBookings.set(
          data.filter(b => b.status === 'Pending' || b.status === 'Confirmed')
        );
        this.bookingLoading.set(false);
        data.forEach(b => {
          this.api.apiCheckRoomAvailability(b.roomId, b.checkIn, b.checkOut).subscribe({
            next: r => this.roomDateMap.update(m => ({ ...m, [b.bookingId]: r.isAvailable })),
            error: () => {}
          });
        });
      },
      error: () => this.bookingLoading.set(false)
    });
  }

  loadPayments(): void {
    const uid = this._uid;
    if (!uid) return;
    this.payLoading.set(true);
    this.payLoaded.set(true);
    // No direct paged payment endpoint for users — fetch via bookings
    this.api.apiGetBookingsByUser(uid, { pageNumber: 1, pageSize: 100 }).subscribe({
      next: res => {
        const bookings = res.data ?? [];
        if (!bookings.length) { this.payments.set([]); this.payLoading.set(false); return; }
        const reqs = bookings.map(b =>
          this.api.apiGetPaymentByBookingId(b.bookingId).pipe(catchError(() => of(null)))
        );
        forkJoin(reqs).subscribe({
          next: results => {
            this.payments.set(results.filter((p): p is PaymentModel => p !== null));
            this.paymentsPage.set(1);
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
      next: res => {
        const rows = res.data ?? [];
        this.cancellations.set(rows);
        this.cancelsPage.set(1);
        const sum = rows
          .filter(c => c.status === 'Approved')
          .reduce((s, c) => s + (c.refundAmount ?? 0), 0);
        this.cancellationRefundTotal.set(sum);
        this.cancelLoading.set(false);
      },
      error: () => this.cancelLoading.set(false)
    });
  }

  loadNotifications(): void {
    this.notifLoading.set(true);
    this.api.apiGetMyNotifications().subscribe({
      next: list => {
        const sorted = [...(list ?? [])].sort((a, b) =>
          new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        );
        this.notifications.set(sorted);
        this.unreadNotifCount.set(sorted.filter(n => !n.isRead).length);
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
    this.api.apiGetReviewsPaged({ userId: uid }, { pageNumber: 1, pageSize: 50 }).subscribe({
      next: res => {
        this.reviews.set(res.data ?? []);
        this.reviewLoading.set(false);
      },
      error: () => this.reviewLoading.set(false)
    });
  }

  loadAmenities(): void {
    this.api.apiGetAmenities().subscribe({ next: a => this.amenities.set(a ?? []), error: () => {} });
  }

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
        this.loadBookings(1);
      },
      error: e => {
        this.cancelSubmitting.set(false);
        this.toast.error(e?.error?.message || 'Failed to request cancellation.', 'Error');
      }
    });
  }

  markRead(n: NotificationModel): void {
    if (n.isRead) return;
    this.api.apiMarkNotificationRead(n.notificationId).subscribe({
      next: () => {
        this.notifications.update(l =>
          l.map(x => x.notificationId === n.notificationId ? { ...x, isRead: true } : x)
        );
        this.unreadNotifCount.update(c => Math.max(0, c - 1));
      },
      error: () => {}
    });
  }

  deleteNotif(n: NotificationModel): void {
    this.api.apiDeleteNotification(n.notificationId).subscribe({
      next: () => {
        this.toast.info('Notification deleted.');
        this.loadNotifications();
      },
      error: () => {}
    });
  }

  sendChat(text?: string): void {
    const msg = (text ?? this.chatInput()).trim();
    if (!msg || this.chatSending()) return;
    this.chatMessages.update(l => [...l, { sender: 'user', text: msg, time: new Date() }]);
    this.chatInput.set('');
    this.chatSending.set(true);
    this.chatScrollNeeded = true;

    this.api.apiChatMessage({
      userId:    this._uid > 0 ? this._uid : undefined,
      sessionId: this.chatSessionId,
      message:   msg
    }).subscribe({
      next: r => {
        this.chatLastIntent.set(r.intent ?? 'general');
        this.chatMessages.update(l => [...l, { sender: 'bot', text: r.reply, time: new Date() }]);
        this.chatSending.set(false);
        this.chatScrollNeeded = true;
      },
      error: () => {
        this.chatMessages.update(l => [...l, { sender: 'bot', text: '⚠️ Sorry, I\'m having trouble connecting. Please try again.', time: new Date() }]);
        this.chatSending.set(false);
        this.chatScrollNeeded = true;
      }
    });
  }

  clearChat(): void {
    this.chatSessionId = crypto.randomUUID();
    this.chatLastIntent.set('greeting');
    this.chatMessages.set([{ sender: 'bot', text: '🔄 Chat cleared! How can I help you?', time: new Date() }]);
    this.chatScrollNeeded = true;
  }

  formatChatText(text: string): string {
    return text
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\n/g, '<br>');
  }

  loadMyPreferences(): void {
    const uid = this._uid;
    if (!uid) return;
    this.prefLoading.set(true);
    this.api.apiGetMyAmenityPreferences(uid).subscribe({
      next: p => { this.myPreferences.set(p ?? []); this.prefLoading.set(false); },
      error: () => { this.myPreferences.set([]); this.prefLoading.set(false); }
    });
  }

  togglePreference(amenity: AmenityModel): void {
    const uid = this._uid;
    if (!uid) return;
    const isSelected = this.selectedAmenityIds().has(amenity.amenityId);

    if (isSelected) {
      this.prefSaving.set(true);
      this.api.apiRemoveAmenityPreferenceByUserAmenity(uid, amenity.amenityId).subscribe({
        next: () => {
          this.myPreferences.update(l => l.filter(p => p.amenityId !== amenity.amenityId));
          this.prefSaving.set(false);
          this.toast.info(`Removed "${amenity.name}" from preferences.`);
        },
        error: e => { this.prefSaving.set(false); this.toast.error(e?.error?.message || 'Error.', 'Error'); }
      });
    } else {
      this.prefSaving.set(true);
      this.api.apiAddAmenityPreference(uid, amenity.amenityId).subscribe({
        next: p => {
          this.myPreferences.update(l => [...l, p]);
          this.prefSaving.set(false);
          this.toast.success(`"${amenity.name}" added to your preferences!`);
        },
        error: e => {
          this.prefSaving.set(false);
          if (e.status === 409) this.toast.warning('Already in your preferences.');
          else if (e.status === 503) this.toast.warning('Preference feature requires a database migration. Please contact admin.');
          else this.toast.error(e?.error?.message || 'Error.', 'Error');
        }
      });
    }
  }

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

  roomUnavailable(bookingId: number): boolean {
    const val = this.roomDateMap()[bookingId];
    return val === false;
  }

  stars(n: number): boolean[] { return [1, 2, 3, 4, 5].map(s => s <= n); }
}
