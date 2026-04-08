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
import { PagedResponse } from '../models/paged.model';
import { WalletModel, WalletTransactionModel } from '../models/wallet.model';
import { $userStatus, UserState } from '../dynamicCommunication/userObservable';
type DashTab = 'bookings' | 'payments' | 'cancellations' | 'notifications' | 'reviews' | 'support';

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

  wallet = signal<WalletModel | null>(null);
  walletTransactions = signal<WalletTransactionModel[]>([]);

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

  // ── Write review form (inline in dashboard) ────────────────────────────
  reviewHotels      = signal<any[]>([]);
  reviewHotelId     = signal(0);
  reviewRating      = signal(0);
  reviewHoverStar   = signal(0);
  reviewComment     = signal('');
  reviewPhoto       = signal<File | null>(null);
  reviewPhotoPreview = signal<string | null>(null);
  reviewSubmitting  = signal(false);
  reviewLightbox    = signal<string | null>(null);
  showWriteReview   = signal(false);
  readonly starsArr = [1, 2, 3, 4, 5];
  readonly apiBase  = 'http://localhost:5000';

  private _clientSlice<T>(arr: T[], page: number, size: number): T[] {
    return arr.slice((page - 1) * size, page * size);
  }

  chatMessages  = signal<{ sender: 'user'|'bot'; text: string; time: Date }[]>([]);
  chatInput     = signal('');
  chatSending   = signal(false);
  chatSessionId = crypto.randomUUID();
  chatScrollNeeded = false;

  private readonly chatSuggestionsMap: Record<string, string[]> = {
    greeting:     ['How to book?', 'Cancellation policy', 'Calculate refund'],
    booking:      ['Booking status', 'How to cancel?', 'Calculate refund'],
    cancellation: ['Calculate refund', 'Refund policy', 'How to book?'],
    payment:      ['Payment failed?', 'Calculate refund', 'How to book?'],
    hotel:        ['Check-in time', 'Hotel amenities', 'How to book?'],
    general:      ['How to book?', 'Calculate refund', 'Hotel amenities'],
    support:      ['How to book?', 'Calculate refund', 'Cancellation policy'],
  };
  chatLastIntent = signal('greeting');
  chatSuggestions = computed(() => this.chatSuggestionsMap[this.chatLastIntent()] ?? this.chatSuggestionsMap['general']);

  totalBookings     = computed(() => this.bookingStats()?.total ?? this.bookings().length);
  confirmedBookings = computed(() => this.bookingStats()?.confirmed ?? this.bookings().filter(b => b.status === 'Confirmed').length);
  pendingBookings   = computed(() => this.bookingStats()?.pending ?? this.bookings().filter(b => b.status === 'Pending').length);
  totalSpent        = computed(() => this.bookingStats()?.spent ?? this.bookings().reduce((s, b) => s + (b.totalAmount ?? 0), 0));

  unreadCount = this.unreadNotifCount;
  totalRefund = computed(() => this.wallet()?.balance ?? this.cancellationRefundTotal() ?? 0);

  constructor() {
    this.sub = $userStatus.subscribe(u => {
      this._user.set(u);
      if (u.userId > 0) {
        this.loadBookings(1);
        this.loadNotifications();
        this.loadCancellations();
        this.loadPayments();
        this.loadWallet();
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
    if (tab === 'reviews' && !this.reviewLoaded()) this.loadReviews();
    if (tab === 'reviews' && !this.reviewHotels().length) {
      this.api.apiGetHotelsPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
        next: res => this.reviewHotels.set(res.data ?? []),
        error: () => {}
      });
    }
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
        // Only fetch payments for bookings that would have a payment record
        const payableBookings = bookings.filter(b =>
          b.status && !['Pending', 'Cancelled'].includes(b.status)
        );
        if (!payableBookings.length) { this.payments.set([]); this.payLoading.set(false); return; }
        const reqs = payableBookings.map(b =>
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
          .filter(c => c.status === 'Approved' || c.status === 'Refunded')
          .reduce((s, c) => s + (c.refundAmount ?? 0), 0);
        this.cancellationRefundTotal.set(sum);
        this.cancelLoading.set(false);
      },
      error: () => this.cancelLoading.set(false)
    });
  }

  loadWallet(): void {
    const uid = this._uid;
    if (!uid) return;
    this.api.apiGetWalletBalance(uid).subscribe({
      next: w => this.wallet.set(w),
      error: () => {}
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

  cancelBookingDirect(b: BookingModel): void {
    if (!confirm('Cancel this booking? No refund applies for direct cancellation of pending bookings.')) return;
    this.api.apiCancelBooking(b.bookingId).subscribe({
      next: () => {
        this.bookings.update(l => l.map(x =>
          x.bookingId === b.bookingId ? { ...x, status: 'Cancelled' } : x
        ));
        this.toast.info('Pending booking cancelled. No refund applicable.', 'Booking Cancelled');
        this.loadWallet();
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
      next: (result: any) => {
        this.cancelSubmitting.set(false);
        this.showCancelForm.set(false);

        const refund = result?.refundAmount ?? 0;
        if (refund > 0) {
          this.toast.success(
            `₹${refund.toLocaleString('en-IN')} has been credited to your Wallet!`,
            '💰 Refund Processed'
          );
        } else {
          this.toast.info(
            'Booking cancelled. No refund applicable as per cancellation policy.',
            'Booking Cancelled'
          );
        }

        this.loadCancellations();
        this.loadBookings(1);
        this.loadWallet();
        this.loadNotifications();
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
    return ({ Pending: 'cs-pending', Approved: 'cs-approved', Rejected: 'cs-rejected', Refunded: 'cs-approved' } as Record<string, string>)[s] ?? 'cs-pending';
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

  // ── Inline review form helpers ─────────────────────────────────────────
  isReviewStarFilled(s: number): boolean { return s <= (this.reviewHoverStar() || this.reviewRating()); }
  setReviewRating(s: number): void { this.reviewRating.set(s); }
  reviewRatingLabel(r: number): string { return ['','Poor','Fair','Good','Very Good','Excellent'][r] ?? ''; }

  onReviewPhotoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    if (file.size > 5 * 1024 * 1024) { this.toast.warning('Photo must be under 5 MB.'); return; }
    this.reviewPhoto.set(file);
    const reader = new FileReader();
    reader.onload = e => this.reviewPhotoPreview.set(e.target?.result as string);
    reader.readAsDataURL(file);
  }

  removeReviewPhoto(): void { this.reviewPhoto.set(null); this.reviewPhotoPreview.set(null); }

  reviewPhotoUrl(url: string | null): string {
    if (!url) return '';
    return url.startsWith('http') ? url : `${this.apiBase}${url}`;
  }

  submitDashboardReview(): void {
    if (!this.reviewHotelId())              { this.toast.warning('Please select a hotel.'); return; }
    if (!this.reviewRating())               { this.toast.warning('Please select a star rating.'); return; }
    if (this.reviewComment().trim().length < 5) { this.toast.warning('Comment must be at least 5 characters.'); return; }

    this.reviewSubmitting.set(true);
    this.api.apiCreateReview(this.reviewHotelId(), this._uid, this.reviewRating(), this.reviewComment().trim()).subscribe({
      next: (r: any) => {
        const photo = this.reviewPhoto();
        const finish = (updated: any) => {
          this.reviews.update(list => [updated, ...list]);
          this.reviewHotelId.set(0); this.reviewRating.set(0);
          this.reviewComment.set(''); this.reviewPhoto.set(null);
          this.reviewPhotoPreview.set(null); this.reviewHoverStar.set(0);
          this.showWriteReview.set(false); this.reviewSubmitting.set(false);
          this.toast.success('Review submitted!', '⭐ Review');
        };
        if (photo) {
          this.api.apiUploadReviewPhoto(r.reviewId, photo).subscribe({
            next: (u: any) => {
              finish(u);
              if (u.coinsEarned > 0) {
                this.toast.success(`🎉 ${u.coinsEarned} coins credited to your Wallet for uploading a photo!`, '💰 Coins Earned');
                this.loadWallet();
              }
            },
            error: () => { finish(r); this.toast.warning('Review saved but photo upload failed.'); }
          });
        } else { finish(r); }
      },
      error: (e: any) => {
        this.reviewSubmitting.set(false);
        if (e.status === 409) this.toast.warning('You already reviewed this hotel.', 'Already Reviewed');
        else this.toast.error(e?.error?.message || 'Failed to submit review.', 'Error');
      }
    });
  }
}
