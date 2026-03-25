import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
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
import { BookingRoomModel, CreateBookingRoomModel } from '../models/booking-room.model';

interface CancellationItem extends CancellationModel { hotelName: string; }

// Enrich BookingRoom with room label and booking info for display
interface BookingRoomItem extends BookingRoomModel {
  roomLabel:   string;   // e.g. "Room #101 — Standard"
  hotelName:   string;
  bookingStatus: string;
}

@Component({
  selector: 'app-dashboard-admin',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule, RouterLink],
  templateUrl: './dashboard-admin.html',
  styleUrl:    './dashboard-admin.css'
})
export class DashboardAdmin {
  private api   = inject(APIService);
  private toast = inject(ToastrService);

  activeTab = signal('Hotels');
  readonly tabs = [
    'Hotels','Rooms','Users','Bookings','Booking Rooms',
    'Amenities','Hotel Amenities','Payments','Reviews','Cancellations'
  ];
  saving = signal(false);

  // ── Data signals ───────────────────────────────────────────────────────────
  hotels        = signal<HotelModel[]>([]);
  rooms         = signal<RoomModel[]>([]);
  users         = signal<UserModel[]>([]);
  bookings      = signal<BookingModel[]>([]);
  amenities     = signal<AmenityModel[]>([]);
  payments      = signal<PaymentModel[]>([]);
  reviews       = signal<ReviewModel[]>([]);
  cancellations = signal<CancellationItem[]>([]);
  bookingRooms  = signal<BookingRoomItem[]>([]);

  // ── Loading guards ─────────────────────────────────────────────────────────
  bookingsLoaded      = signal(false);
  reviewsLoaded       = signal(false);
  cancellationsLoaded = signal(false);
  bookingRoomsLoaded  = signal(false);

  // ── Booking Rooms form ─────────────────────────────────────────────────────
  editBrId  = signal<number | null>(null);   // null = create mode
  brf = signal<CreateBookingRoomModel>({
    bookingId:     0,
    roomId:        0,
    pricePerNight: 0,
    numberOfRooms: 1
  });
  brFilterBookingId = signal(0);   // 0 = show all

  // ── Hotel / Room form signals ──────────────────────────────────────────────
  editHotelId = signal<number | null>(null);
  editRoomId  = signal<number | null>(null);

  hf = signal({ hotelName:'', location:'', address:'', starRating:3, totalRooms:10, contactNumber:'', imagePath:'' });
  rf = signal({ hotelId:0, roomNumber:1, roomType:'Standard', pricePerNight:1000, capacity:2, imageUrl:'' });
  af = signal({ name:'', description:'', icon:'' });

  // ── Cancellation modal ─────────────────────────────────────────────────────
  activeCancelId  = signal(0);
  modalStatus     = signal('');
  modalRefund     = signal(0);
  showCancelModal = signal(false);
  updatingCancel  = signal(false);

  // ── Computed stats ─────────────────────────────────────────────────────────
  totalRevenue = computed(() =>
    this.payments().filter(p => p.paymentStatus === 'Completed').reduce((s, p) => s + (p.amount ?? 0), 0)
  );
  totalRefunds = computed(() =>
    this.cancellations().filter(c => c.status === 'Approved').reduce((s, c) => s + (c.refundAmount ?? 0), 0)
  );

  /** Booking rooms filtered by booking ID (0 = all) */
  filteredBookingRooms = computed(() => {
    const fid = this.brFilterBookingId();
    return fid > 0
      ? this.bookingRooms().filter(br => br.bookingId === fid)
      : this.bookingRooms();
  });

  constructor() { this.loadAll(); }

  // ── Startup load (always-eager) ────────────────────────────────────────────
  loadAll(): void {
    this.api.apiGetHotelsPaged({ pageNumber:1, pageSize:100 }).subscribe({
      next: r => this.hotels.set(r.data ?? []), error: () => {}
    });
    this.api.apiGetRooms().subscribe({
      next: r => this.rooms.set(r ?? []), error: () => {}
    });
    this.api.apiGetAllUsers().subscribe({
      next: u => this.users.set(u ?? []), error: () => {}
    });
    this.api.apiGetAmenities().subscribe({
      next: a => this.amenities.set(a ?? []), error: () => {}
    });
    this.api.apiGetAllPayments().subscribe({
      next: p => this.payments.set(p ?? []), error: () => {}
    });
  }

  // ── Lazy loaders ───────────────────────────────────────────────────────────

  /** POST /api/booking/all/paged  — admin only */
  loadBookings(): void {
    if (this.bookingsLoaded()) return;
    this.bookingsLoaded.set(true);
    this.api.apiGetAllBookingsPaged({ pageNumber:1, pageSize:100 }).subscribe({
      next: r => this.bookings.set(r.data ?? []),
      error: () => this.bookingsLoaded.set(false)
    });
  }

  /** POST /api/review/all/paged  — admin/manager */
  loadReviews(): void {
    if (this.reviewsLoaded()) return;
    this.reviewsLoaded.set(true);
    this.api.apiGetAllReviewsPaged({ pageNumber:1, pageSize:10 }).subscribe({
      next: r => this.reviews.set(r.data ?? []),
      error: () => this.reviewsLoaded.set(false)
    });
  }

  /** POST /api/cancellation/paged  — admin/manager */
  loadCancellations(): void {
    if (this.cancellationsLoaded()) return;
    this.cancellationsLoaded.set(true);
    this.api.apiGetAllCancellationsPaged({ pageNumber:1, pageSize:10 }).subscribe({
      next: res => {
        const all = res.data ?? [];
        const enriched: CancellationItem[] = all.map(c => ({
          ...c,
          hotelName: this.bookings().find(b => b.bookingId === c.bookingId)?.hotelName
                     ?? `Booking #${c.bookingId}`
        }));
        this.cancellations.set(enriched);
      },
      error: () => this.cancellationsLoaded.set(false)
    });
  }

  /**
   * Booking Rooms loader.
   * Strategy:
   *   1. Take all loaded bookings (up to 50 for performance).
   *   2. For each booking call GET /api/bookingroom/booking/{bookingId}.
   *   3. Merge, enrich with room label + hotel name, sort by bookingId.
   * API: GET /api/bookingroom/booking/{bookingId}  [Authorize(Roles="user,admin,hotelmanager")]
   */
  loadBookingRooms(): void {
    if (this.bookingRoomsLoaded()) return;
    // Make sure bookings are loaded first so we can enrich
    if (!this.bookingsLoaded()) {
      this.loadBookings();
    }
    this.bookingRoomsLoaded.set(true);

    const bookingIds = this.bookings().map(b => b.bookingId).slice(0, 50);

    if (!bookingIds.length) {
      // No bookings loaded yet — wait then retry once
      setTimeout(() => {
        this.bookingRoomsLoaded.set(false);
        const ids = this.bookings().map(b => b.bookingId).slice(0, 50);
        if (!ids.length) { this.bookingRooms.set([]); return; }
        this._fetchBookingRoomsForIds(ids);
      }, 800);
      return;
    }
    this._fetchBookingRoomsForIds(bookingIds);
  }

  private _fetchBookingRoomsForIds(ids: number[]): void {
    const reqs = ids.map(id =>
      this.api.apiGetBookingRoomsByBookingId(id).pipe(
        catchError(() => of([] as BookingRoomModel[]))
      )
    );
    forkJoin(reqs).subscribe({
      next: results => {
        const flat = results.flat();
        const enriched: BookingRoomItem[] = flat.map(br => {
          const room    = this.rooms().find(r => r.roomId === br.roomId);
          const booking = this.bookings().find(b => b.bookingId === br.bookingId);
          return {
            ...br,
            roomLabel:     room    ? `#${room.roomNumber} — ${room.roomType}` : `Room #${br.roomId}`,
            hotelName:     booking?.hotelName ?? `Booking #${br.bookingId}`,
            bookingStatus: booking?.status    ?? '—'
          };
        });
        // Sort by bookingId for a predictable order
        enriched.sort((a, b) => a.bookingId - b.bookingId);
        this.bookingRooms.set(enriched);
      },
      error: () => this.bookingRoomsLoaded.set(false)
    });
  }

  // ── Tab switch ─────────────────────────────────────────────────────────────
  switchTab(t: string): void {
    this.activeTab.set(t);
    if (t === 'Bookings')      this.loadBookings();
    if (t === 'Booking Rooms') { this.loadBookings(); this.loadBookingRooms(); }
    if (t === 'Reviews')       this.loadReviews();
    if (t === 'Cancellations') { this.loadBookings(); this.loadCancellations(); }
  }

  // ── Booking Room CRUD ──────────────────────────────────────────────────────

  /** Populate the form when admin clicks Edit on a booking-room row. */
  editBookingRoom(br: BookingRoomItem): void {
    this.editBrId.set(br.bookingRoomId);
    this.brf.set({
      bookingId:     br.bookingId,
      roomId:        br.roomId,
      pricePerNight: br.pricePerNight,
      numberOfRooms: br.numberOfRooms
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  resetBrf(): void {
    this.editBrId.set(null);
    this.brf.set({ bookingId:0, roomId:0, pricePerNight:0, numberOfRooms:1 });
  }

  /**
   * Save booking-room (create or update).
   * POST /api/bookingroom          [Authorize(Roles="user,admin")]
   * PUT  /api/bookingroom/{id}     [Authorize(Roles="user,admin")]
   */
  saveBookingRoom(): void {
    const f = this.brf();
    if (!f.bookingId)        { this.toast.warning('Please select a booking.'); return; }
    if (!f.roomId)           { this.toast.warning('Please select a room.');    return; }
    if (f.pricePerNight <= 0){ this.toast.warning('Price per night must be > 0.'); return; }
    if (f.numberOfRooms < 1) { this.toast.warning('Number of rooms must be at least 1.'); return; }

    this.saving.set(true);

    const obs = this.editBrId()
      ? this.api.apiUpdateBookingRoom(this.editBrId()!, f)
      : this.api.apiCreateBookingRoom(f);

    obs.subscribe({
      next: result => {
        const room    = this.rooms().find(r => r.roomId === result.roomId);
        const booking = this.bookings().find(b => b.bookingId === result.bookingId);
        const item: BookingRoomItem = {
          ...result,
          roomLabel:     room    ? `#${room.roomNumber} — ${room.roomType}` : `Room #${result.roomId}`,
          hotelName:     booking?.hotelName ?? `Booking #${result.bookingId}`,
          bookingStatus: booking?.status    ?? '—'
        };

        if (this.editBrId()) {
          this.bookingRooms.update(l => l.map(x => x.bookingRoomId === item.bookingRoomId ? item : x));
          this.toast.success('Booking room updated!');
        } else {
          this.bookingRooms.update(l => [...l, item].sort((a, b) => a.bookingId - b.bookingId));
          this.toast.success('Booking room added!');
        }
        this.resetBrf();
        this.saving.set(false);
      },
      error: e => {
        this.saving.set(false);
        this.toast.error(e?.error?.message || 'Failed to save booking room.', 'Error');
      }
    });
  }

  /**
   * DELETE /api/bookingroom/{id}  [Authorize(Roles="user,admin")]
   */
  deleteBookingRoom(br: BookingRoomItem): void {
    if (!confirm(`Remove Room ${br.roomLabel} from Booking #${br.bookingId}?`)) return;
    this.api.apiDeleteBookingRoom(br.bookingRoomId).subscribe({
      next: () => {
        this.bookingRooms.update(l => l.filter(x => x.bookingRoomId !== br.bookingRoomId));
        this.toast.success('Booking room removed.');
      },
      error: e => this.toast.error(e?.error?.message || 'Error removing booking room.', 'Error')
    });
  }

  /** Convenience: auto-fill price when admin picks a room in the BR form */
  onBrRoomSelect(roomId: number): void {
    const room = this.rooms().find(r => r.roomId === roomId);
    this.brf.update(f => ({ ...f, roomId, pricePerNight: room?.pricePerNight ?? 0 }));
  }

  // ── Hotel CRUD ─────────────────────────────────────────────────────────────
  saveHotel(): void {
    const f = this.hf();
    if (!f.hotelName.trim()) { this.toast.warning('Hotel name is required.'); return; }
    if (!f.location.trim())  { this.toast.warning('Location is required.');   return; }
    this.saving.set(true);
    const obs = this.editHotelId()
      ? this.api.apiUpdateHotel(this.editHotelId()!, { ...f, starRating:+f.starRating, totalRooms:+f.totalRooms })
      : this.api.apiCreateHotel({ ...f, starRating:+f.starRating, totalRooms:+f.totalRooms });
    obs.subscribe({
      next: () => { this.saving.set(false); this.toast.success(this.editHotelId() ? 'Hotel updated!' : 'Hotel created!'); this.resetHotel(); this.loadAll(); },
      error: e  => { this.saving.set(false); this.toast.error(e?.error?.message || 'Failed to save hotel.', 'Error'); }
    });
  }
  editHotel(h: HotelModel): void {
    this.editHotelId.set(h.hotelId);
    this.hf.set({ hotelName:h.hotelName, location:h.location, address:h.address, starRating:h.starRating, totalRooms:h.totalRooms, contactNumber:h.contactNumber, imagePath:h.imagePath });
  }
  resetHotel(): void { this.editHotelId.set(null); this.hf.set({ hotelName:'', location:'', address:'', starRating:3, totalRooms:10, contactNumber:'', imagePath:'' }); }
  deleteHotel(h: HotelModel): void {
    if (!confirm(`Delete hotel "${h.hotelName}"? This cannot be undone.`)) return;
    this.api.apiDeleteHotel(h.hotelId).subscribe({
      next: () => { this.toast.success('Hotel deleted.'); this.loadAll(); },
      error: e  => this.toast.error(e?.error?.message || 'Failed to delete hotel.', 'Error')
    });
  }

  // ── Room CRUD ──────────────────────────────────────────────────────────────
  saveRoom(): void {
    const f = this.rf();
    if (!f.hotelId)         { this.toast.warning('Please select a hotel.'); return; }
    if (f.pricePerNight < 1){ this.toast.warning('Price per night must be at least ₹1.'); return; }
    this.saving.set(true);
    const obs = this.editRoomId()
      ? this.api.apiUpdateRoom(this.editRoomId()!, { ...f, hotelId:+f.hotelId, pricePerNight:+f.pricePerNight, capacity:+f.capacity })
      : this.api.apiCreateRoom({ ...f, hotelId:+f.hotelId, pricePerNight:+f.pricePerNight, capacity:+f.capacity });
    obs.subscribe({
      next: () => { this.saving.set(false); this.toast.success(this.editRoomId() ? 'Room updated!' : 'Room created!'); this.resetRoom(); this.loadAll(); },
      error: e  => { this.saving.set(false); this.toast.error(e?.error?.message || 'Failed to save room.', 'Error'); }
    });
  }
  editRoom(r: RoomModel): void {
    this.editRoomId.set(r.roomId);
    this.rf.set({ hotelId:r.hotelId, roomNumber:r.roomNumber, roomType:r.roomType, pricePerNight:r.pricePerNight, capacity:r.capacity, imageUrl:r.imageUrl ?? '' });
  }
  resetRoom(): void { this.editRoomId.set(null); this.rf.set({ hotelId:0, roomNumber:1, roomType:'Standard', pricePerNight:1000, capacity:2, imageUrl:'' }); }
  deleteRoom(r: RoomModel): void {
    if (!confirm('Deactivate this room?')) return;
    this.api.apiDeleteRoom(r.roomId).subscribe({
      next: () => { this.rooms.update(l => l.map(x => x.roomId === r.roomId ? { ...x, isAvailable: false } : x)); this.toast.success('Room deactivated.'); },
      error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
    });
  }

  // ── Users ──────────────────────────────────────────────────────────────────
  deleteUser(u: UserModel): void {
    if (!confirm(`Delete user "${u.userName}"? This cannot be undone.`)) return;
    this.api.apiDeleteUser(u.userId).subscribe({
      next: () => { this.users.update(l => l.filter(x => x.userId !== u.userId)); this.toast.success('User deleted.'); },
      error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
    });
  }

  // ── Booking actions ────────────────────────────────────────────────────────
  confirmBooking(b: BookingModel): void {
    this.api.apiConfirmBooking(b.bookingId).subscribe({
      next: () => { this.bookings.update(l => l.map(x => x.bookingId === b.bookingId ? { ...x, status:'Confirmed' } : x)); this.toast.success('Booking confirmed!'); },
      error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
    });
  }
  completeBooking(b: BookingModel): void {
    this.api.apiCompleteBooking(b.bookingId).subscribe({
      next: () => { this.bookings.update(l => l.map(x => x.bookingId === b.bookingId ? { ...x, status:'Completed' } : x)); this.toast.success('Booking completed!'); },
      error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
    });
  }

  // ── Amenities ──────────────────────────────────────────────────────────────
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
    if (!confirm(`Delete amenity "${a.name}"?`)) return;
    this.api.apiDeleteAmenity(a.amenityId).subscribe({
      next: () => { this.amenities.update(l => l.filter(x => x.amenityId !== a.amenityId)); this.toast.success('Amenity deleted.'); },
      error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
    });
  }

  // ── Payments ───────────────────────────────────────────────────────────────
  updatePaymentStatus(p: PaymentModel, e: Event): void {
    const s = (e.target as HTMLSelectElement).value;
    if (!s) return;
    this.api.apiUpdatePaymentStatus(p.paymentId, s).subscribe({
      next: u => { this.payments.update(l => l.map(x => x.paymentId === u.paymentId ? u : x)); this.toast.success('Payment status updated!'); },
      error: e => this.toast.error(e?.error?.message || 'Error.', 'Error')
    });
  }

  // ── Reviews ────────────────────────────────────────────────────────────────
  deleteReview(r: ReviewModel): void {
    if (!confirm('Delete this review?')) return;
    this.api.apiDeleteReview(r.reviewId).subscribe({
      next: () => { this.reviews.update(l => l.filter(x => x.reviewId !== r.reviewId)); this.toast.success('Review deleted.'); },
      error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
    });
  }

  // ── Cancellation modal ─────────────────────────────────────────────────────
  openCancelModal(c: CancellationItem): void {
    this.activeCancelId.set(c.cancellationId);
    this.modalStatus.set(c.status);
    this.modalRefund.set(c.refundAmount ?? 0);
    this.showCancelModal.set(true);
  }
  closeCancelModal(): void { this.showCancelModal.set(false); }

  updateCancelStatus(): void {
    if (!this.modalStatus()) { this.toast.warning('Please select a status.'); return; }
    if (this.modalRefund() < 0) { this.toast.warning('Refund cannot be negative.'); return; }
    this.updatingCancel.set(true);
    this.api.apiUpdateCancellationStatus(this.activeCancelId(), this.modalStatus(), this.modalRefund()).subscribe({
      next: u => {
        this.cancellations.update(l => l.map(x => x.cancellationId === u.cancellationId
          ? { ...x, status: u.status, refundAmount: u.refundAmount } : x
        ));
        this.updatingCancel.set(false);
        this.closeCancelModal();
        this.toast.success(`Status updated to "${u.status}".`);
      },
      error: e => { this.updatingCancel.set(false); this.toast.error(e?.error?.message || 'Error.', 'Error'); }
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  getStatusClass(s: string): string { return ({Pending:'badge-pending',Confirmed:'badge-confirmed',Completed:'badge-completed',Cancelled:'badge-cancelled'} as Record<string,string>)[s] ?? 'badge-pending'; }
  getPayClass(s: string): string    { return ({Completed:'badge-confirmed',Failed:'badge-cancelled',Pending:'badge-pending',Refunded:'badge-refunded'} as Record<string,string>)[s] ?? 'badge-pending'; }
  cancelClass(s: string): string    { return ({Pending:'cs-pending',Approved:'cs-approved',Rejected:'cs-rejected'} as Record<string,string>)[s] ?? 'cs-pending'; }
  brStatusClass(s: string): string  { return ({Pending:'badge-pending',Confirmed:'badge-confirmed',Completed:'badge-completed',Cancelled:'badge-cancelled'} as Record<string,string>)[s] ?? 'badge-pending'; }
  stars(n: number): boolean[]       { return [1,2,3,4,5].map(s => s <= n); }
}
