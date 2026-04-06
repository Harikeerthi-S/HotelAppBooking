import { Component, inject, signal, computed } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { CommonModule, DatePipe } from '@angular/common';
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
interface CancellationItem extends CancellationModel { hotelName: string; }

@Component({
  selector: 'app-dashboard-manager',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule, RouterLink],
  templateUrl: './dashboard-manager.html',
  styleUrl:    './dashboard-manager.css'
})
export class DashboardManager {
  private api   = inject(APIService);
  private toast = inject(ToastrService);

  activeTab = signal('Hotels');
  readonly tabs = ['Hotels','Rooms','Users','Bookings','Amenities','Hotel Amenities','Payments','Reviews','Cancellations'];
  saving = signal(false);

  hotels        = signal<HotelModel[]>([]);
  hotelsLoading = signal(false);
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
  hfLocation   = signal('');
  hfMinRating  = signal('');
  hfMinPrice   = signal('');
  hfMaxPrice   = signal('');
  hfPriceRange = signal('');

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
  hotelsTotalPages = signal(1);
  hotelsTotalRecords = signal(0);
  readonly MGR_PS = 10;

  hotelsForRoomForm = signal<HotelModel[]>([]);
  rooms         = signal<RoomModel[]>([]);
  roomsPage     = signal(1);
  roomsTotalPages = signal(1);
  roomsTotalRecords = signal(0);

  users         = signal<UserModel[]>([]);
  usersPage     = signal(1);
  readonly USERS_PS = 10;
  pagedUsers    = computed(() => this.users().slice((this.usersPage()-1)*this.USERS_PS, this.usersPage()*this.USERS_PS));
  usersTotalPages = computed(() => Math.max(1, Math.ceil(this.users().length / this.USERS_PS)));

  bookings      = signal<BookingModel[]>([]);
  bookingsPage  = signal(1);
  bookingsTotalPages = signal(1);
  bookingsTotalRecords = signal(0);

  amenities     = signal<AmenityModel[]>([]);
  payments      = signal<PaymentModel[]>([]);
  paymentsPage  = signal(1);
  readonly PAY_PS = 10;
  pagedPayments = computed(() => this.payments().slice((this.paymentsPage()-1)*this.PAY_PS, this.paymentsPage()*this.PAY_PS));
  paymentsTotalPages = computed(() => Math.max(1, Math.ceil(this.payments().length / this.PAY_PS)));
  reviews       = signal<ReviewModel[]>([]);
  reviewsPage   = signal(1);
  reviewsTotalPages = signal(1);
  reviewsTotalRecords = signal(0);

  cancellations = signal<CancellationItem[]>([]);
  cancelsPage   = signal(1);
  cancelsTotalPages = signal(1);
  cancelsTotalRecords = signal(0);
  cancellationApprovedRefundTotal = signal(0);

  roomsLoading    = signal(false);
  bookingsLoading = signal(false);
  reviewsLoading  = signal(false);
  cancelLoading   = signal(false);

  editHotelId = signal<number | null>(null);
  editRoomId  = signal<number | null>(null);
  hf = signal({ hotelName:'', location:'', address:'', starRating:3, totalRooms:10, contactNumber:'', imagePath:'' });
  rf = signal({ hotelId:0, roomNumber:1, roomType:'Standard', pricePerNight:1000, capacity:2, imageUrl:'' });
  af = signal({ name:'', description:'', icon:'' });

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

  totalRevenue = computed(() =>
    this.payments().filter(p => p.paymentStatus === 'Completed').reduce((s, p) => s + (p.amount ?? 0), 0)
  );
  totalRefunds = computed(() => this.cancellationApprovedRefundTotal());

  statHotelsCount = computed(() => this.hotels().length);
  statUsersCount  = computed(() => this.users().length);
  statBookingsCount = computed(() => this.bookings().length);
  statRoomsCount  = computed(() => this.rooms().length);

  constructor() { this.loadAll(); }

  loadAll(): void {
    this.loadHotels(1);
    this.loadRooms(1);
    this.refreshHotelsForRoomForm();
    this.api.apiGetAmenities().subscribe({ next: a => this.amenities.set(a ?? []), error: () => {} });
    this.api.apiGetAllUsers().subscribe({ next: u => this.users.set(u ?? []), error: () => {} });
    this.loadPayments();
    this.loadBookings(1);
    this.loadReviews(1);
    this.loadTotalApprovedRefunds();
  }
  private refreshHotelsForRoomForm(): void {
    this.api.apiGetHotelsPaged({ pageNumber: 1, pageSize: 1000 }).subscribe({
      next: r => this.hotelsForRoomForm.set(r.data ?? []),
      error: () => {}
    });
  }

  private loadTotalApprovedRefunds(): void {
    this.api.apiGetAllCancellationsPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: res => {
        const sum = (res.data ?? [])
          .filter(c => c.status === 'Approved')
          .reduce((s, c) => s + (c.refundAmount ?? 0), 0);
        this.cancellationApprovedRefundTotal.set(sum);
      },
      error: () => {}
    });
  }

  loadHotels(page = 1): void {
    this.hotelsLoading.set(true);
    this.hotelsPage.set(page);
    const loc = this.hfLocation().trim();
    const rat = this.hfMinRating() ? +this.hfMinRating() : undefined;
    const min = this.hfMinPrice()  ? +this.hfMinPrice()  : undefined;
    const max = this.hfMaxPrice()  ? +this.hfMaxPrice()  : undefined;
    const req = { pageNumber: page, pageSize: this.MGR_PS };
    const obs = (loc || rat || min || max)
      ? this.api.apiFilterHotels({ location: loc || undefined, minRating: rat, minPrice: min, maxPrice: max }, req)
      : this.api.apiGetHotelsPaged(req);
    obs.subscribe({
      next: r => {
        this.hotels.set(r.data ?? []);
        this.hotelsTotalPages.set(r.totalPages ?? 1);
        this.hotelsTotalRecords.set(r.totalRecords ?? 0);
        this.hotelsLoading.set(false);
      },
      error: () => this.hotelsLoading.set(false)
    });
  }

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

  loadRooms(page = 1): void {
    this.roomsLoading.set(true);
    this.roomsPage.set(page);
    const f = this.roomFilter();
    const hotelId = f.hotelId ? +f.hotelId : undefined;
    this.api.apiGetRoomsPaged({ pageNumber: page, pageSize: this.MGR_PS }, hotelId).subscribe({
      next: r => {
        let data = r.data ?? [];
        if (f.roomType)      data = data.filter(rm => rm.roomType === f.roomType);
        if (f.onlyAvailable) data = data.filter(rm => rm.isAvailable);
        this.rooms.set(data);
        this.roomsTotalPages.set(r.totalPages ?? 1);
        this.roomsTotalRecords.set(r.totalRecords ?? 0);
        this.roomsLoading.set(false);
      },
      error: () => this.roomsLoading.set(false)
    });
  }

  loadBookings(page = 1): void {
    this.bookingsLoading.set(true);
    this.bookingsPage.set(page);
    this.api.apiGetAllBookingsPaged({ pageNumber: page, pageSize: this.MGR_PS }).subscribe({
      next: r => {
        this.bookings.set(r.data ?? []);
        this.bookingsTotalPages.set(r.totalPages ?? 1);
        this.bookingsTotalRecords.set(r.totalRecords ?? 0);
        this.bookingsLoading.set(false);
        this.loadCancellations(1);
      },
      error: () => this.bookingsLoading.set(false)
    });
  }

  loadReviews(page = 1): void {
    this.reviewsLoading.set(true);
    this.reviewsPage.set(page);
    const f = this.reviewFilter();
    const filter = {
      hotelId: f.hotelId ? +f.hotelId : undefined,
      userId:  f.userId  ? +f.userId  : undefined,
      rating:  f.rating  ? +f.rating  : undefined
    };
    this.api.apiGetReviewsPaged(filter, { pageNumber: page, pageSize: this.MGR_PS }).subscribe({
      next: r => {
        this.reviews.set(r.data ?? []);
        this.reviewsTotalPages.set(r.totalPages ?? 1);
        this.reviewsTotalRecords.set(r.totalRecords ?? 0);
        this.reviewsLoading.set(false);
      },
      error: () => this.reviewsLoading.set(false)
    });
  }

  loadCancellations(page = 1): void {
    this.cancelLoading.set(true);
    this.cancelsPage.set(page);
    this.api.apiGetAllCancellationsPaged({ pageNumber: page, pageSize: this.MGR_PS }).subscribe({
      next: res => {
        this.cancellations.set((res.data ?? []).map(c => ({
          ...c,
          hotelName: this.bookings().find(b => b.bookingId === c.bookingId)?.hotelName ?? `Booking #${c.bookingId}`
        })));
        this.cancelsTotalPages.set(res.totalPages ?? 1);
        this.cancelsTotalRecords.set(res.totalRecords ?? 0);
        this.cancelLoading.set(false);
      },
      error: () => this.cancelLoading.set(false)
    });
  }

  loadPayments(): void {
    this.api.apiGetPaymentsPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: r => this.payments.set(r.data ?? []),
      error: () => {}
    });
  }

  switchTab(t: string): void {
    this.activeTab.set(t);
    if (t === 'Cancellations' && !this.cancellations().length && !this.cancelLoading() && this.bookings().length) {
      this.loadCancellations(1);
    }
  }





  saveHotel(): void {
    const f = this.hf();
    if (!f.hotelName.trim()) { this.toast.warning('Hotel name is required.'); return; }
    if (!f.location.trim())  { this.toast.warning('Location is required.');   return; }
    this.saving.set(true);
    const obs = this.editHotelId()
      ? this.api.apiUpdateHotel(this.editHotelId()!, { ...f, starRating:+f.starRating, totalRooms:+f.totalRooms })
      : this.api.apiCreateHotel({ ...f, starRating:+f.starRating, totalRooms:+f.totalRooms });
    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success(this.editHotelId() ? 'Hotel updated!' : 'Hotel created!');
        this.resetHotel();
        this.loadHotels(1);
        this.loadRooms(1);
        this.refreshHotelsForRoomForm();
        this.api.apiGetAllUsers().subscribe({ next: u => this.users.set(u ?? []), error: () => {} });
        this.loadPayments();
      },
      error: e  => { this.saving.set(false); this.toast.error(e?.error?.message || 'Failed to save hotel.', 'Error'); }
    });
  }
  editHotel(h: HotelModel): void {
    this.editHotelId.set(h.hotelId);
    this.hf.set({ hotelName:h.hotelName, location:h.location, address:h.address, starRating:h.starRating, totalRooms:h.totalRooms, contactNumber:h.contactNumber, imagePath:h.imagePath });
  }
  resetHotel(): void { this.editHotelId.set(null); this.hf.set({ hotelName:'', location:'', address:'', starRating:3, totalRooms:10, contactNumber:'', imagePath:'' }); }
  deleteHotel(h: HotelModel): void {
    this.openConfirm('Delete Hotel', `Are you sure you want to delete "${h.hotelName}"? This cannot be undone.`,
      () => this.api.apiDeleteHotel(h.hotelId).subscribe({
        next: () => { this.toast.success('Hotel deleted.'); this.loadHotels(1); this.refreshHotelsForRoomForm(); },
        error: e  => this.toast.error(e?.error?.message || 'Failed to delete hotel.', 'Error')
      })
    );
  }

  saveRoom(): void {
    const f = this.rf();
    if (!f.hotelId)         { this.toast.warning('Please select a hotel.'); return; }
    if (f.pricePerNight < 1){ this.toast.warning('Price per night must be at least ₹1.'); return; }
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
    this.openConfirm('Deactivate Room', `Are you sure you want to deactivate Room #${r.roomNumber}? It will no longer be available for booking.`,
      () => this.api.apiDeleteRoom(r.roomId).subscribe({
        next: () => { this.rooms.update(l => l.map(x => x.roomId === r.roomId ? { ...x, isAvailable: false } : x)); this.toast.success('Room deactivated.'); },
        error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
      }),
      'bi-slash-circle-fill', 'text-warning'
    );
  }

  deleteUser(u: UserModel): void {
    this.openConfirm('Delete User', `Are you sure you want to delete user "${u.userName}"? This will permanently remove the account.`,
      () => this.api.apiDeleteUser(u.userId).subscribe({
        next: () => { this.toast.success('User deleted.'); this.api.apiGetAllUsers().subscribe({ next: list => this.users.set(list ?? []), error: () => {} }); },
        error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
      })
    );
  }

  confirmBooking(b: BookingModel): void {
    this.api.apiConfirmBooking(b.bookingId).subscribe({
      next: () => {
        this.bookings.update(l => l.map(x => x.bookingId === b.bookingId ? { ...x, status:'Confirmed' } : x));
        this.toast.success('Booking confirmed!');
      },
      error: e => {
        this.toast.error(e?.error?.message || 'Failed to confirm booking.', 'Error');
        this.loadBookings(this.bookingsPage());
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
        this.loadBookings(this.bookingsPage());
      }
    });
  }

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
    this.openConfirm('Delete Amenity', `Are you sure you want to delete amenity "${a.name}"? It will be removed from all hotels.`,
      () => this.api.apiDeleteAmenity(a.amenityId).subscribe({
        next: () => { this.amenities.update(l => l.filter(x => x.amenityId !== a.amenityId)); this.toast.success('Amenity deleted.'); },
        error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
      })
    );
  }

  updatePaymentStatus(p: PaymentModel, e: Event): void {
    const s = (e.target as HTMLSelectElement).value;
    if (!s) return;
    this.api.apiUpdatePaymentStatus(p.paymentId, s).subscribe({
      next: u => {
        this.payments.update(l => l.map(x => x.paymentId === u.paymentId ? u : x));
        this.toast.success('Payment status updated!');
      },
      error: e => this.toast.error(e?.error?.message || 'Error.', 'Error')
    });
  }

  deleteReview(r: ReviewModel): void {
    this.openConfirm('Delete Review', 'Are you sure you want to delete this review? This action cannot be undone.',
      () => this.api.apiDeleteReview(r.reviewId).subscribe({
        next: () => { this.loadReviews(this.reviewsPage()); this.toast.success('Review deleted.'); },
        error: e  => this.toast.error(e?.error?.message || 'Error.', 'Error')
      })
    );
  }

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
        this.cancellations.update(l => l.map(x => x.cancellationId === u.cancellationId ? { ...x, status: u.status, refundAmount: u.refundAmount } : x));
        this.updatingCancel.set(false);
        this.closeCancelModal();
        this.toast.success(`Status updated to "${u.status}".`);
        this.loadTotalApprovedRefunds();
      },
      error: e => { this.updatingCancel.set(false); this.toast.error(e?.error?.message || 'Error.', 'Error'); }
    });
  }

  getStatusClass(s: string): string { return ({Pending:'badge-pending',Confirmed:'badge-confirmed',Completed:'badge-completed',Cancelled:'badge-cancelled'} as Record<string,string>)[s] ?? 'badge-pending'; }
  getPayClass(s: string): string    { return ({Completed:'badge-confirmed',Failed:'badge-cancelled',Pending:'badge-pending',Refunded:'badge-refunded'} as Record<string,string>)[s] ?? 'badge-pending'; }
  cancelClass(s: string): string    { return ({Pending:'cs-pending',Approved:'cs-approved',Rejected:'cs-rejected'} as Record<string,string>)[s] ?? 'cs-pending'; }
  stars(n: number): boolean[]       { return [1,2,3,4,5].map(s => s <= n); }
}
