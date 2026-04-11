import { Component, inject, signal, computed } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { CommonModule, DatePipe, SlicePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { HotelModel } from '../models/hotel.model';
import { RoomModel } from '../models/room.model';
import { UserModel } from '../models/user.model';
import { BookingModel } from '../models/booking.model';
import { AmenityModel } from '../models/amenity.model';
import { PaymentModel } from '../models/payment.model';
import { ReviewModel } from '../models/review.model';
import { CancellationModel } from '../models/cancellation.model';
import { AuditLogModel } from '../models/audit-log.model';
import { NotificationModel } from '../models/notification.model';
import { PagedResponse } from '../models/paged.model';

interface CancellationItem extends CancellationModel { hotelName: string; }

const PS = 10;
@Component({
  selector: 'app-dashboard-admin',
  standalone: true,
  imports: [CommonModule, DatePipe, SlicePipe, FormsModule, RouterLink],
  templateUrl: './dashboard-admin.html',
  styleUrl:    './dashboard-admin.css'
})
export class DashboardAdmin {
  private api   = inject(APIService);
  private toast = inject(ToastrService);

  activeTab = signal('Hotels');
  readonly tabs = ['Hotels','Rooms','Users','Bookings','Amenities','Hotel Amenities','Payments','Reviews','Cancellations','Notifications','Audit Logs'];
  saving = signal(false);

  // ── Data ──────────────────────────────────────────────────────────────────
  hotels        = signal<HotelModel[]>([]);
  hotelsForRoomForm = signal<HotelModel[]>([]);
  rooms         = signal<RoomModel[]>([]);
  users         = signal<UserModel[]>([]);
  usersForDropdown = signal<UserModel[]>([]);
  bookings      = signal<BookingModel[]>([]);
  amenities     = signal<AmenityModel[]>([]);
  payments      = signal<PaymentModel[]>([]);
  reviews       = signal<ReviewModel[]>([]);
  cancellations = signal<CancellationItem[]>([]);
  notifications = signal<NotificationModel[]>([]);
  auditLogs     = signal<AuditLogModel[]>([]);

  // ── Paged metadata ────────────────────────────────────────────────────────
  hotelsMeta    = signal<PagedResponse<HotelModel> | null>(null);
  roomsMeta     = signal<PagedResponse<RoomModel> | null>(null);
  bookingsMeta  = signal<PagedResponse<BookingModel> | null>(null);
  reviewsMeta   = signal<PagedResponse<ReviewModel> | null>(null);
  cancelMeta    = signal<PagedResponse<CancellationModel> | null>(null);
  auditMeta     = signal<PagedResponse<AuditLogModel> | null>(null);

  // ── Page numbers ──────────────────────────────────────────────────────────
  hotelsPage    = signal(1);

  // ── Hotel filters ─────────────────────────────────────────────────────────
  readonly cityOptions = ['Mumbai','Delhi','Goa','Bangalore','Jaipur','Chennai','Hyderabad','Kolkata','Pune','Ahmedabad'];
  readonly priceRanges = [
    { label:'Under ₹1,000',      min:'',     max:'1000'  },
    { label:'₹1,000 – ₹3,000',  min:'1000', max:'3000'  },
    { label:'₹3,000 – ₹6,000',  min:'3000', max:'6000'  },
    { label:'₹6,000 – ₹10,000', min:'6000', max:'10000' },
    { label:'Above ₹10,000',     min:'10000',max:''      },
  ];
  hfLocation       = signal('');
  hfMinRating      = signal('');
  hfMinPrice       = signal('');
  hfMaxPrice       = signal('');
  hfPriceRange     = signal('');

  onHotelPriceRange(val: string): void {
    this.hfPriceRange.set(val);
    if (!val) { this.hfMinPrice.set(''); this.hfMaxPrice.set(''); return; }
    const r = this.priceRanges.find(x => `${x.min}-${x.max}` === val);
    if (r) { this.hfMinPrice.set(r.min); this.hfMaxPrice.set(r.max); }
  }
  clearHotelFilter(): void {
    this.hfLocation.set(''); this.hfMinRating.set('');
    this.hfMinPrice.set(''); this.hfMaxPrice.set(''); this.hfPriceRange.set('');
    this.loadHotels(1);
  }
  roomsPage     = signal(1);
  bookingsPage  = signal(1);
  reviewsPage   = signal(1);
  cancelPage    = signal(1);
  auditPage     = signal(1);
  usersPage     = signal(1);
  paymentsPage  = signal(1);
  notifPage     = signal(1);

  // ── Loading ───────────────────────────────────────────────────────────────
  hotelsLoading    = signal(false);
  roomsLoading     = signal(false);
  bookingsLoading  = signal(false);
  reviewsLoading   = signal(false);
  cancelLoading    = signal(false);
  auditLoading     = signal(false);

  // ── Room filter ───────────────────────────────────────────────────────────
  readonly roomTypeOptions = ['Standard','Deluxe','Suite','Single','Double'];
  roomFilter = signal<{ roomType: string; hotelId: string; onlyAvailable: boolean }>({
    roomType: '', hotelId: '', onlyAvailable: false
  });
  roomIsFiltered = computed(() => {
    const f = this.roomFilter();
    return !!(f.roomType || f.hotelId || f.onlyAvailable);
  });
  applyRoomFilter(): void { this.loadRooms(1); }
  clearRoomFilter(): void {
    this.roomFilter.set({ roomType: '', hotelId: '', onlyAvailable: false });
    this.loadRooms(1);
  }

  // ── Review filter ──────────────────────────────────────────────────────────
  reviewFilter = signal<{ hotelId: string; userId: string; rating: string }>({
    hotelId: '', userId: '', rating: ''
  });
  reviewIsFiltered = computed(() => {
    const f = this.reviewFilter();
    return !!(f.hotelId || f.userId || f.rating);
  });
  applyReviewFilter(): void { this.loadReviews(1); }
  clearReviewFilter(): void {
    this.reviewFilter.set({ hotelId: '', userId: '', rating: '' });
    this.loadReviews(1);
  }
  readonly auditActionOptions = [
    'BookingCreated','BookingConfirmed','BookingCompleted','BookingCancelled',
    'HotelCreated','HotelUpdated','HotelDeactivated',
    'RoomCreated','RoomUpdated','RoomDeactivated',
    'PaymentCreated','PaymentStatusUpdated',
    'CancellationRequested','CancellationStatusUpdated',
    'ReviewCreated','ReviewDeleted'
  ];
  readonly auditEntityOptions = ['Hotel','Room','Booking','Payment','Review','Cancellation','User','Amenity'];
  auditFilter = signal<{ action:string; entityName:string; userId?:number; entityId?:number; fromDate:string; toDate:string }>({
    action:'', entityName:'', userId:undefined, entityId:undefined, fromDate:'', toDate:''
  });
  auditIsFiltered = computed(() => {
    const f = this.auditFilter();
    return !!(f.action || f.entityName || f.userId || f.entityId || f.fromDate || f.toDate);
  });
  notifLoading     = signal(false);
  notifSending     = signal(false);

  // ── Client-side pagination (Users, Payments, Notifications) ───────────────
  readonly CPS = 10;
  pagedUsers    = computed(() => this._slice(this.users(),    this.usersPage()));
  pagedPayments = computed(() => this._slice(this.payments(), this.paymentsPage()));
  pagedNotifs   = computed(() => this._slice(this.notifications(), this.notifPage()));
  usersTotalPages    = computed(() => Math.ceil(this.users().length    / this.CPS) || 1);
  paymentsTotalPages = computed(() => Math.ceil(this.payments().length / this.CPS) || 1);
  notifTotalPages    = computed(() => Math.ceil(this.notifications().length / this.CPS) || 1);

  private _slice<T>(arr: T[], page: number): T[] {
    return arr.slice((page - 1) * this.CPS, page * this.CPS);
  }

  // Smart paginator
  visiblePages(current: number, total: number): number[] {
    if (total < 1) return [];
    if (total === 1) return [1];
    const set = new Set([1, total, current, current - 1, current + 1]);
    const sorted = Array.from(set).filter(p => p >= 1 && p <= total).sort((a, b) => a - b);
    const result: number[] = [];
    for (let i = 0; i < sorted.length; i++) {
      if (i > 0 && sorted[i] - sorted[i - 1] > 1) result.push(-1);
      result.push(sorted[i]);
    }
    return result;
  }

  // ── Forms ─────────────────────────────────────────────────────────────────
  editHotelId = signal<number | null>(null);
  editRoomId  = signal<number | null>(null);
  hf = signal({ hotelName:'', location:'', address:'', starRating:3, totalRooms:10, contactNumber:'', imagePath:'' });
  rf = signal({ hotelId:0, roomNumber:1, roomType:'Standard', pricePerNight:1000, capacity:2, imageUrl:'' });
  af = signal({ name:'', description:'', icon:'' });
  notifForm = signal({ userId: 0, message: '' });

  // ── Cancellation modal ────────────────────────────────────────────────────
  activeCancelId  = signal(0);
  modalStatus     = signal('');
  modalRefund     = signal(0);
  showCancelModal = signal(false);
  updatingCancel  = signal(false);

  // ── Confirm Modal ─────────────────────────────────────────────────────────
  showConfirmModal  = signal(false);
  confirmTitle      = signal('');
  confirmMessage    = signal('');
  confirmIcon       = signal('bi-exclamation-triangle-fill');
  confirmColor      = signal('text-danger');
  private _confirmCallback: (() => void) | null = null;

  openConfirm(title: string, message: string, onConfirm: () => void, icon = 'bi-exclamation-triangle-fill', color = 'text-danger'): void {
    this.confirmTitle.set(title);
    this.confirmMessage.set(message);
    this.confirmIcon.set(icon);
    this.confirmColor.set(color);
    this._confirmCallback = onConfirm;
    this.showConfirmModal.set(true);
  }
  closeConfirm(): void { this.showConfirmModal.set(false); this._confirmCallback = null; }
  doConfirm(): void { this._confirmCallback?.(); this.showConfirmModal.set(false); this._confirmCallback = null; }

  // ── Stats ─────────────────────────────────────────────────────────────────
  totalRevenue = computed(() =>
    this.payments().filter(p => p.paymentStatus === 'Completed').reduce((s, p) => s + (p.amount ?? 0), 0)
  );
  totalRefunds = computed(() =>
    this.cancellations().filter(c => c.status === 'Approved').reduce((s, c) => s + (c.refundAmount ?? 0), 0)
  );
  unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);
  adminUnreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);

  constructor() { this.loadAll(); }

  // ── Startup ───────────────────────────────────────────────────────────────
  loadAll(): void {
    this.loadHotels(1);
    this.loadRooms(1);
    this.loadBookings(1);
    this.loadCancellations(1);
    this.loadAuditLogs(1);
    this.api.apiGetAllUsers().subscribe({ next: u => { const l = u ?? []; this.users.set(l); this.usersForDropdown.set(l); this.usersPage.set(1); }, error: () => {} });
    this.api.apiGetAmenities().subscribe({ next: a => this.amenities.set(a ?? []), error: () => {} });
    this.api.apiGetPaymentsPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: r => { this.payments.set(r.data ?? []); this.paymentsPage.set(1); },
      error: () => {}
    });
    this.refreshHotelsForRoomForm();
  }

  private refreshHotelsForRoomForm(): void {
    this.api.apiGetHotelsPaged({ pageNumber: 1, pageSize: 1000 }).subscribe({
      next: r => this.hotelsForRoomForm.set(r.data ?? []),
      error: () => {}
    });
  }

  // ── Paged loaders ─────────────────────────────────────────────────────────
  loadHotels(page: number): void {
    this.hotelsLoading.set(true); this.hotelsPage.set(page);
    const loc = this.hfLocation().trim();
    const rat = this.hfMinRating() ? +this.hfMinRating() : undefined;
    const min = this.hfMinPrice()  ? +this.hfMinPrice()  : undefined;
    const max = this.hfMaxPrice()  ? +this.hfMaxPrice()  : undefined;
    const hasFilter = loc || rat || min || max;
    const req = { pageNumber: page, pageSize: PS };
    const obs = hasFilter
      ? this.api.apiFilterHotels({ location: loc || undefined, minRating: rat, minPrice: min, maxPrice: max }, req)
      : this.api.apiGetHotelsPaged(req);
    obs.subscribe({
      next: r => { this.hotels.set(r.data ?? []); this.hotelsMeta.set(r); this.hotelsLoading.set(false); },
      error: () => this.hotelsLoading.set(false)
    });
  }

  loadRooms(page: number): void {
    this.roomsLoading.set(true); this.roomsPage.set(page);
    const f = this.roomFilter();
    const hotelId = f.hotelId ? +f.hotelId : undefined;
    this.api.apiGetRoomsPaged({ pageNumber: page, pageSize: PS }, hotelId).subscribe({
      next: r => {
        // Client-side filter for roomType and availability
        let data = r.data ?? [];
        if (f.roomType)      data = data.filter(rm => rm.roomType === f.roomType);
        if (f.onlyAvailable) data = data.filter(rm => rm.isAvailable);
        this.rooms.set(data);
        this.roomsMeta.set(r);
        this.roomsLoading.set(false);
      },
      error: () => this.roomsLoading.set(false)
    });
  }

  loadBookings(page: number): void {
    this.bookingsLoading.set(true); this.bookingsPage.set(page);
    this.api.apiGetAllBookingsPaged({ pageNumber: page, pageSize: PS }).subscribe({
      next: r => { this.bookings.set(r.data ?? []); this.bookingsMeta.set(r); this.bookingsLoading.set(false); },
      error: () => this.bookingsLoading.set(false)
    });
  }

  loadReviews(page: number): void {
    this.reviewsLoading.set(true); this.reviewsPage.set(page);
    const f = this.reviewFilter();
    const filter = {
      hotelId: f.hotelId ? +f.hotelId : undefined,
      userId:  f.userId  ? +f.userId  : undefined,
      rating:  f.rating  ? +f.rating  : undefined
    };
    this.api.apiGetReviewsPaged(filter, { pageNumber: page, pageSize: PS }).subscribe({
      next: r => { this.reviews.set(r.data ?? []); this.reviewsMeta.set(r); this.reviewsLoading.set(false); },
      error: () => this.reviewsLoading.set(false)
    });
  }

  loadCancellations(page: number): void {
    this.cancelLoading.set(true); this.cancelPage.set(page);
    this.api.apiGetAllCancellationsPaged({ pageNumber: page, pageSize: PS }).subscribe({
      next: res => {
        this.cancelMeta.set(res);
        this.cancellations.set((res.data ?? []).map(c => ({
          ...c,
          hotelName: this.bookings().find(b => b.bookingId === c.bookingId)?.hotelName ?? `Booking #${c.bookingId}`
        })));
        this.cancelLoading.set(false);
      },
      error: () => this.cancelLoading.set(false)
    });
  }

  loadAuditLogs(page?: number): void {
    const p = page ?? this.auditPage();
    this.auditLoading.set(true);
    this.auditPage.set(p);

    const raw = this.auditFilter();
    const hasFilter = !!(raw.action || raw.entityName || raw.userId || raw.entityId || raw.fromDate || raw.toDate);

    if (hasFilter) {
      // Build filter object with only non-empty values + paging
      const filterBody: Record<string, unknown> = { pageNumber: p, pageSize: PS };
      if (raw.action)     filterBody['action']     = raw.action;
      if (raw.entityName) filterBody['entityName'] = raw.entityName;
      if (raw.userId)     filterBody['userId']     = raw.userId;
      if (raw.entityId)   filterBody['entityId']   = raw.entityId;
      if (raw.fromDate)   filterBody['fromDate']   = raw.fromDate;
      if (raw.toDate)     filterBody['toDate']     = raw.toDate + 'T23:59:59';

      // Call backend directly — POST /api/auditlog/filter/paged with combined body
      this.api.apiFilterAuditLogsPaged(filterBody as any, { pageNumber: p, pageSize: PS }).subscribe({
        next: r => { this.auditLogs.set(r.data ?? []); this.auditMeta.set(r); this.auditLoading.set(false); },
        error: (e) => { this.toast.error(e?.error?.message || 'Filter failed.', 'Error'); this.auditLoading.set(false); }
      });
    } else {
      this.api.apiGetAllAuditLogsPaged({ pageNumber: p, pageSize: PS }).subscribe({
        next: r => { this.auditLogs.set(r.data ?? []); this.auditMeta.set(r); this.auditLoading.set(false); },
        error: (e) => { this.toast.error(e?.error?.message || 'Failed to load audit logs.', 'Error'); this.auditLoading.set(false); }
      });
    }
  }

  applyAuditFilter(): void { this.loadAuditLogs(1); }
  clearAuditFilter(): void {
    this.auditFilter.set({ action: '', entityName: '', userId: undefined, entityId: undefined, fromDate: '', toDate: '' });
    this.loadAuditLogs(1);
  }

  loadNotifications(): void {
    this.notifLoading.set(true);
    this.api.apiGetAllNotifications().subscribe({
      next: n => { this.notifications.set(n ?? []); this.notifPage.set(1); this.notifLoading.set(false); },
      error: () => this.notifLoading.set(false)
    });
  }

  switchTab(t: string): void {
    this.activeTab.set(t);
    if (t === 'Hotels'        && !this.hotelsMeta())   this.loadHotels(1);
    if (t === 'Rooms'         && !this.roomsMeta())    this.loadRooms(1);
    if (t === 'Bookings'      && !this.bookingsMeta()) this.loadBookings(1);
    if (t === 'Reviews'       && !this.reviewsMeta())  this.loadReviews(1);
    if (t === 'Cancellations' && !this.cancelMeta())   this.loadCancellations(1);
    if (t === 'Notifications')                         this.loadNotifications();
    if (t === 'Audit Logs' && !this.auditMeta())      this.loadAuditLogs(1);
  }





  // ── Hotel CRUD ────────────────────────────────────────────────────────────
  saveHotel(): void {
    const f = this.hf();
    if (!f.hotelName.trim()) { this.toast.warning('Hotel name is required.'); return; }
    if (!f.location.trim())  { this.toast.warning('Location is required.');   return; }
    this.saving.set(true);
    const obs = this.editHotelId()
      ? this.api.apiUpdateHotel(this.editHotelId()!, { ...f, starRating:+f.starRating, totalRooms:+f.totalRooms })
      : this.api.apiCreateHotel({ ...f, starRating:+f.starRating, totalRooms:+f.totalRooms });
    obs.subscribe({
      next: () => { this.saving.set(false); this.toast.success(this.editHotelId() ? 'Hotel updated!' : 'Hotel created!'); this.resetHotel(); this.loadHotels(1); this.refreshHotelsForRoomForm(); },
      error: e  => { this.saving.set(false); this.toast.error(e?.error?.message || 'Failed to save hotel.', 'Error'); }
    });
  }
  editHotel(h: HotelModel): void {
    this.editHotelId.set(h.hotelId);
    this.hf.set({ hotelName:h.hotelName, location:h.location, address:h.address, starRating:h.starRating, totalRooms:h.totalRooms, contactNumber:h.contactNumber, imagePath:h.imagePath });
  }
  resetHotel(): void { this.editHotelId.set(null); this.hf.set({ hotelName:'', location:'', address:'', starRating:3, totalRooms:10, contactNumber:'', imagePath:'' }); }
  deleteHotel(h: HotelModel): void {
    this.openConfirm(
      'Deactivate Hotel',
      `Are you sure you want to deactivate "${h.hotelName}"? It will no longer be visible to users.`,
      () => this.api.apiDeleteHotel(h.hotelId).subscribe({
        next: () => { this.toast.success('Hotel deleted.'); this.loadHotels(this.hotelsPage()); this.refreshHotelsForRoomForm(); },
        error: e  => this.toast.error(e?.error?.message || 'Failed to delete hotel.', 'Error')
      })
    );
  }

  // ── Room CRUD ─────────────────────────────────────────────────────────────
  saveRoom(): void {
    const f = this.rf();
    if (!f.hotelId)          { this.toast.warning('Please select a hotel.'); return; }
    if (f.pricePerNight < 1) { this.toast.warning('Price per night must be at least ₹1.'); return; }
    this.saving.set(true);
    const payload = { hotelId:+f.hotelId, roomNumber:+f.roomNumber, roomType:f.roomType, pricePerNight:+f.pricePerNight, capacity:+f.capacity, imageUrl:f.imageUrl||undefined };
    const obs = this.editRoomId()
      ? this.api.apiUpdateRoom(this.editRoomId()!, payload)
      : this.api.apiCreateRoom(payload);
    obs.subscribe({
      next: () => { this.saving.set(false); this.toast.success(this.editRoomId() ? 'Room updated!' : 'Room created!'); this.resetRoom(); this.loadRooms(1); },
      error: e  => { this.saving.set(false); this.toast.error(e?.error?.message || 'Failed to save room.', 'Error'); }
    });
  }
  editRoom(r: RoomModel): void {
    this.editRoomId.set(r.roomId);
    this.rf.set({ hotelId:r.hotelId, roomNumber:r.roomNumber, roomType:r.roomType, pricePerNight:r.pricePerNight, capacity:r.capacity, imageUrl:r.imageUrl ?? '' });
  }
  resetRoom(): void { this.editRoomId.set(null); this.rf.set({ hotelId:0, roomNumber:1, roomType:'Standard', pricePerNight:1000, capacity:2, imageUrl:'' }); }
  deleteRoom(r: RoomModel): void {
    this.openConfirm(
      'Deactivate Room',
      `Are you sure you want to deactivate Room #${r.roomNumber}? It will no longer be available for booking.`,
      () => this.api.apiDeleteRoom(r.roomId).subscribe({
        next: () => { this.rooms.update(l => l.map(x => x.roomId === r.roomId ? { ...x, isAvailable: false } : x)); this.toast.success('Room deactivated.'); },
        error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
      }),
      'bi-slash-circle-fill', 'text-warning'
    );
  }

  // ── Users ─────────────────────────────────────────────────────────────────
  deleteUser(u: UserModel): void {
    this.openConfirm(
      'Deactivate User',
      `Are you sure you want to deactivate user "${u.userName}"? Their account will be disabled.`,
      () => this.api.apiDeleteUser(u.userId).subscribe({
        next: () => {
          this.users.update(l => l.filter(x => x.userId !== u.userId));
          this.usersForDropdown.update(l => l.filter(x => x.userId !== u.userId));
          if (this.usersPage() > this.usersTotalPages()) this.usersPage.set(this.usersTotalPages());
          this.toast.success('User deleted.');
        },
        error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
      })
    );
  }

  // ── Booking actions ───────────────────────────────────────────────────────
  confirmBooking(b: BookingModel): void {
    this.api.apiConfirmBooking(b.bookingId).subscribe({
      next: () => {
        this.bookings.update(l => l.map(x => x.bookingId === b.bookingId ? { ...x, status:'Confirmed' } : x));
        this.toast.success('Booking confirmed!');
      },
      error: e => {
        this.toast.error(e?.error?.message || 'Failed to confirm booking.', 'Error');
        this.loadBookings(this.bookingsPage()); // reload to sync actual DB state
      }
    });
  }
  completeBooking(b: BookingModel): void {
    this.api.apiCompleteBooking(b.bookingId).subscribe({
      next: () => {
        this.bookings.update(l => l.map(x => x.bookingId === b.bookingId ? { ...x, status:'Completed' } : x));
        this.toast.success('Booking completed!');
      },
      error: e => {
        this.toast.error(e?.error?.message || 'Failed to complete booking.', 'Error');
        this.loadBookings(this.bookingsPage()); // reload to sync actual DB state
      }
    });
  }

  // ── Amenities ─────────────────────────────────────────────────────────────
  saveAmenity(): void {
    const a = this.af();
    if (!a.name.trim()) { this.toast.warning('Amenity name is required.'); return; }
    this.saving.set(true);
    this.api.apiCreateAmenity(a.name.trim(), a.description.trim(), a.icon.trim()).subscribe({
      next: c => { this.amenities.update(l => [...l, c]); this.af.set({ name:'', description:'', icon:'' }); this.saving.set(false); this.toast.success('Amenity added!'); },
      error: e => { this.saving.set(false); if (e.status === 409) this.toast.warning('Amenity name already exists.'); else this.toast.error(e?.error?.message || 'Error.', 'Error'); }
    });
  }
  deleteAmenity(a: AmenityModel): void {
    this.openConfirm(
      'Deactivate Amenity',
      `Are you sure you want to deactivate amenity "${a.name}"? It will be removed from all hotels.`,
      () => this.api.apiDeleteAmenity(a.amenityId).subscribe({
        next: () => { this.amenities.update(l => l.filter(x => x.amenityId !== a.amenityId)); this.toast.success('Amenity deleted.'); },
        error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
      })
    );
  }

  // ── Payments ──────────────────────────────────────────────────────────────
  updatePaymentStatus(p: PaymentModel, e: Event): void {
    const s = (e.target as HTMLSelectElement).value;
    if (!s) return;
    this.api.apiUpdatePaymentStatus(p.paymentId, s).subscribe({
      next: u => { this.payments.update(l => l.map(x => x.paymentId === u.paymentId ? u : x)); this.toast.success('Payment status updated!'); },
      error: e => this.toast.error(e?.error?.message || 'Error.', 'Error')
    });
  }

  // ── Reviews ───────────────────────────────────────────────────────────────
  deleteReview(r: ReviewModel): void {
    this.openConfirm(
      'Deactivate Review',
      'Are you sure you want to deactivate this review? It will no longer be visible.',
      () => this.api.apiDeleteReview(r.reviewId).subscribe({
        next: () => { this.reviews.update(l => l.filter(x => x.reviewId !== r.reviewId)); this.toast.success('Review deleted.'); },
        error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
      })
    );
  }

  // ── Cancellation modal ────────────────────────────────────────────────────
  openCancelModal(c: CancellationItem): void {
    this.activeCancelId.set(c.cancellationId);
    this.modalStatus.set(c.status);
    this.modalRefund.set(c.refundAmount ?? 0);
    this.showCancelModal.set(true);
  }

  onModalStatusChange(status: string): void {
    this.modalStatus.set(status);
    // Keep refund amount for Approved and Refunded; clear for Rejected/Pending
    if (status !== 'Approved' && status !== 'Refunded') {
      this.modalRefund.set(0);
    }
  }
  closeCancelModal(): void { this.showCancelModal.set(false); }

  updateCancelStatus(): void {
    if (!this.modalStatus()) { this.toast.warning('Please select a status.'); return; }
    if (this.modalRefund() < 0) { this.toast.warning('Refund cannot be negative.'); return; }
    this.updatingCancel.set(true);
    this.api.apiUpdateCancellationStatus(this.activeCancelId(), this.modalStatus(), this.modalRefund()).subscribe({
      next: (u: any) => {
        this.cancellations.update(l => l.map(x =>
          x.cancellationId === u.cancellationId
            ? { ...x, status: u.status, refundAmount: u.refundAmount, walletCredited: u.walletCredited }
            : x
        ));
        this.updatingCancel.set(false);
        this.closeCancelModal();

        // Show wallet credit info if applicable
        if (u.walletCredited && u.refundAmount > 0) {
          this.toast.success(
            `Status → "${u.status}" · ₹${u.refundAmount.toLocaleString('en-IN')} credited to user's wallet 🪙`,
            'Cancellation Updated'
          );
        } else {
          this.toast.success(`Status updated to "${u.status}".`);
        }
      },
      error: e => {
        this.updatingCancel.set(false);
        this.toast.error(e?.error?.message || 'Error updating cancellation.', 'Error');
      }
    });
  }

  // ── Notifications ─────────────────────────────────────────────────────────
  sendNotification(): void {
    const f = this.notifForm();
    if (!f.userId || f.userId < 1) { this.toast.warning('Please select a user.'); return; }
    if (!f.message.trim())         { this.toast.warning('Message is required.'); return; }
    this.notifSending.set(true);
    this.api.apiCreateNotification(f.userId, f.message.trim()).subscribe({
      next: n => {
        this.notifications.update(l => [n, ...l]);
        this.notifForm.set({ userId: 0, message: '' });
        this.notifSending.set(false);
        this.toast.success('Notification sent!');
      },
      error: e => { this.notifSending.set(false); this.toast.error(e?.error?.message || 'Failed to send.', 'Error'); }
    });
  }

  deleteNotification(n: NotificationModel): void {
    this.openConfirm(
      'Deactivate Notification',
      'Are you sure you want to deactivate this notification?',
      () => this.api.apiDeleteNotification(n.notificationId).subscribe({
        next: () => { this.notifications.update(l => l.filter(x => x.notificationId !== n.notificationId)); this.toast.info('Deleted.'); },
        error: e => this.toast.error(e?.error?.message || 'Error.', 'Error')
      }),
      'bi-bell-slash-fill', 'text-secondary'
    );
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  getStatusClass(s: string): string { return ({Pending:'badge-pending',Confirmed:'badge-confirmed',Completed:'badge-completed',Cancelled:'badge-cancelled'} as Record<string,string>)[s] ?? 'badge-pending'; }
  getPayClass(s: string): string    { return ({Completed:'badge-confirmed',Failed:'badge-cancelled',Pending:'badge-pending',Refunded:'badge-refunded'} as Record<string,string>)[s] ?? 'badge-pending'; }
  cancelClass(s: string): string    { return ({Pending:'cs-pending',Approved:'cs-approved',Rejected:'cs-rejected'} as Record<string,string>)[s] ?? 'cs-pending'; }
  stars(n: number): boolean[]       { return [1,2,3,4,5].map(s => s <= n); }
  actionClass(action: string): string {
    const a = action.toLowerCase();
    if (a.includes('created') || a.includes('requested')) return 'action-create';
    if (a.includes('updated') || a.includes('confirmed') || a.includes('completed')) return 'action-update';
    if (a.includes('deleted') || a.includes('deactivated') || a.includes('cancelled')) return 'action-delete';
    return 'action-default';
  }
}
