import { Component, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule, DecimalPipe } from '@angular/common';
import { Subscription, interval, of } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { HotelModel } from '../models/hotel.model';
import { RoomModel } from '../models/room.model';
import { AmenityModel } from '../models/amenity.model';
import { HotelAmenityModel } from '../models/hotel-amenity.model';
import { CreateBookingModel } from '../models/booking.model';
import { $userStatus, UserState } from '../dynamicCommunication/userObservable';

@Component({
  selector: 'app-booking',
  imports: [RouterLink, FormsModule, DecimalPipe],
  templateUrl: './booking.html',
  styleUrl: './booking.css'
})
export class Booking implements OnInit, OnDestroy {
  private apiService = inject(APIService);
  private toastr     = inject(ToastrService);
  private route      = inject(ActivatedRoute);
  private router     = inject(Router);

  hotel    = signal<HotelModel | null>(null);
  room     = signal<RoomModel | null>(null);
  loading  = signal(false);
  checkIn  = signal('');
  checkOut = signal('');
  numRooms = signal(1);
  today    = new Date().toISOString().split('T')[0];

  // Amenity selection
  availableAmenities = signal<HotelAmenityModel[]>([]);
  selectedAmenities = signal<number[]>([]);

  dateAvailability = signal<boolean | null>(null);
  dateChecking     = signal(false);

  availableOptions = computed(() => {
    const total = this.hotel()?.totalRooms ?? 1;
    const max   = Math.min(total, 5); // cap at 5 rooms per booking
    return Array.from({ length: max }, (_, i) => i + 1);
    // hotel with 10 rooms → [1, 2, 3, 4, 5]
    // hotel with 2 rooms  → [1, 2]
  });

  // FIX: userId from Observable (localStorage) — not sessionStorage
  private currentUser = signal<UserState>({ userId: 0, userName: '', email: '', role: '' });
  private sub: Subscription;
  private pollSub: Subscription | null = null;

  get nights(): number {
    if (!this.checkIn() || !this.checkOut()) return 0;
    return Math.max(0, Math.round(
      (new Date(this.checkOut()).getTime() - new Date(this.checkIn()).getTime()) / 86400000
    ));
  }

  get totalAmount(): number {
    // Remove amenity upcharge - amenities are now complimentary
    const baseAmount = (this.room()?.pricePerNight ?? 0) * this.nights * this.numRooms();
    return baseAmount;
  }

  get selectedAmenitiesDetails(): HotelAmenityModel[] {
    const selected = this.selectedAmenities();
    return this.availableAmenities().filter(a => selected.includes(a.hotelAmenityId));
  }

  constructor() {
    this.sub = $userStatus.subscribe(u => this.currentUser.set(u));

    const hotelId = +this.route.snapshot.params['hotelId'];
    const roomId  = +this.route.snapshot.params['roomId'];
    const q       = this.route.snapshot.queryParams;

    if (q['checkIn'])  this.checkIn.set(q['checkIn']);
    if (q['checkOut']) this.checkOut.set(q['checkOut']);
    if (q['rooms'])    this.numRooms.set(+q['rooms']);

    this.apiService.apiGetHotelById(hotelId).subscribe({ next: h => this.hotel.set(h), error: () => {} });
    this.apiService.apiGetRoomById(roomId).subscribe({
      next: r => { this.room.set(r); this.checkDateAvailability(); },
      error: () => {}
    });

    // Load available amenities for this hotel
    this.loadHotelAmenities(hotelId);
  }

  checkDateAvailability(): void {
    const room = this.room();
    const ci   = this.checkIn();
    const co   = this.checkOut();
    if (!room || !ci || !co) { this.dateAvailability.set(null); return; }
    this.dateChecking.set(true);
    this.apiService.apiCheckRoomAvailability(room.roomId, ci, co).subscribe({
      next: res => { this.dateAvailability.set(res.isAvailable); this.dateChecking.set(false); },
      error: ()  => { this.dateAvailability.set(null);           this.dateChecking.set(false); }
    });
  }

  ngOnInit(): void {
    // Poll date-range availability every 15s so UI reflects other users' bookings
    this.pollSub = interval(15000).pipe(
      switchMap(() => {
        const roomId = this.room()?.roomId;
        const ci = this.checkIn();
        const co = this.checkOut();
        if (!roomId || !ci || !co) return of(null);
        return this.apiService.apiCheckRoomAvailability(roomId, ci, co).pipe(
          catchError(() => of(null))
        );
      })
    ).subscribe({
      next: res => {
        if (res && !res.isAvailable) {
          this.toastr.warning(
            'This room has just been booked for your selected dates by another user.',
            'Dates No Longer Available'
          );
        }
      },
      error: () => {}
    });
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
    this.pollSub?.unsubscribe();
  }

  // ── AMENITY METHODS ───────────────────────────────────────────────────────
  loadHotelAmenities(hotelId: number): void {
    this.apiService.apiGetHotelAmenitiesByHotel(hotelId).subscribe({
      next: amenities => {
        // Show all hotel amenities (remove availability filter for now)
        const allAmenities = amenities || [];
        this.availableAmenities.set(allAmenities);
        console.log('Loaded hotel amenities for booking:', allAmenities);
      },
      error: (err) => {
        console.log('Could not load hotel amenities:', err);
        this.availableAmenities.set([]);
      }
    });
  }

  toggleAmenity(amenityId: number): void {
    const current = this.selectedAmenities();
    if (current.includes(amenityId)) {
      this.selectedAmenities.set(current.filter(id => id !== amenityId));
    } else {
      this.selectedAmenities.set([...current, amenityId]);
    }
  }

  isAmenitySelected(amenityId: number): boolean {
    return this.selectedAmenities().includes(amenityId);
  }

  // ── BULK AMENITY SELECTION METHODS ────────────────────────────────────────
  selectAllAmenities(): void {
    const allAmenityIds = this.availableAmenities().map(a => a.hotelAmenityId);
    this.selectedAmenities.set(allAmenityIds);
  }

  deselectAllAmenities(): void {
    this.selectedAmenities.set([]);
  }

  get isAllSelected(): boolean {
    const available = this.availableAmenities();
    const selected = this.selectedAmenities();
    return available.length > 0 && available.every(a => selected.includes(a.hotelAmenityId));
  }

  get isSomeSelected(): boolean {
    return this.selectedAmenities().length > 0 && !this.isAllSelected;
  }

  confirmBooking(): void {
    if (!this.checkIn() || !this.checkOut()) { this.toastr.warning('Please select check-in and check-out dates.'); return; }
    if (this.nights < 1) { this.toastr.warning('Check-out must be after check-in.'); return; }
    if (this.dateAvailability() === false) { this.toastr.error('This room is already booked for the selected dates.', 'Not Available'); return; }

    const userId = this.currentUser().userId;
    if (!userId || userId < 1) {
      this.toastr.error('Session expired. Please login again.', 'Session Error');
      this.router.navigateByUrl('/login');
      return;
    }

    this.loading.set(true);
    const model         = new CreateBookingModel();
    model.userId        = userId;
    model.hotelId       = this.hotel()!.hotelId;
    model.roomId        = this.room()!.roomId;
    model.numberOfRooms = this.numRooms();
    model.checkIn       = this.checkIn();
    model.checkOut      = this.checkOut();

    // Log selected amenities for future backend integration
    if (this.selectedAmenities().length > 0) {
      console.log('Booking includes selected amenities:', {
        amenityIds: this.selectedAmenities(),
        amenityDetails: this.selectedAmenitiesDetails,
        totalAmenityUpcharge: this.totalAmount - (this.room()!.pricePerNight * this.nights * this.numRooms())
      });
    }

    this.apiService.apiCreateBooking(model).subscribe({
      next: b => {
        this.loading.set(false);
        const amenityInfo = this.selectedAmenities().length > 0 
          ? ` with ${this.selectedAmenities().length} premium amenities` 
          : '';
        this.toastr.success(`Booking created${amenityInfo}! Proceed to payment.`, 'Booking Confirmed');
        this.router.navigateByUrl(`/payment/${b.bookingId}`);
      },
      error: e => {
        this.loading.set(false);
        if (e.status === 409) {
          this.toastr.error(e?.error?.message || 'You already have an active booking for this room on these dates.', 'Already Booked');
        } else if (e.status === 400) {
          this.toastr.error(e?.error?.message || 'This room is not available for the selected dates.', 'Not Available');
        } else {
          this.toastr.error(e?.error?.message || 'Booking failed.', 'Booking Error');
        }
      }
    });
  }
}
