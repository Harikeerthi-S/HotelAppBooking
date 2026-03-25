import { Component, inject, signal, OnDestroy } from '@angular/core';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { HotelModel } from '../models/hotel.model';
import { RoomModel } from '../models/room.model';
import { CreateBookingModel } from '../models/booking.model';
import { $userStatus, UserState } from '../dynamicCommunication/userObservable';

@Component({
  selector: 'app-booking',
  imports: [RouterLink, FormsModule],
  templateUrl: './booking.html',
  styleUrl: './booking.css'
})
export class Booking implements OnDestroy {
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

  // FIX: userId from Observable (localStorage) — not sessionStorage
  private currentUser = signal<UserState>({ userId: 0, userName: '', email: '', role: '' });
  private sub: Subscription;

  get nights(): number {
    if (!this.checkIn() || !this.checkOut()) return 0;
    return Math.max(0, Math.round(
      (new Date(this.checkOut()).getTime() - new Date(this.checkIn()).getTime()) / 86400000
    ));
  }

  get totalAmount(): number {
    return (this.room()?.pricePerNight ?? 0) * this.nights * this.numRooms();
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
    this.apiService.apiGetRoomById(roomId).subscribe({ next: r => this.room.set(r), error: () => {} });
  }

  ngOnDestroy(): void { this.sub.unsubscribe(); }

  confirmBooking(): void {
    if (!this.checkIn() || !this.checkOut()) { this.toastr.warning('Please select check-in and check-out dates.'); return; }
    if (this.nights < 1) { this.toastr.warning('Check-out must be after check-in.'); return; }

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
    // Date conversion YYYY-MM-DD → YYYY-MM-DDT00:00:00 handled in api.service
    model.checkIn  = this.checkIn();
    model.checkOut = this.checkOut();

    this.apiService.apiCreateBooking(model).subscribe({
      next: b => {
        this.loading.set(false);
        this.toastr.success('Booking created! Proceed to payment.', 'Booking Confirmed');
        this.router.navigateByUrl(`/payment/${b.bookingId}`);
      },
      error: (e) => {
        this.loading.set(false);
        this.toastr.error(e?.error?.message || e?.error?.Message || 'Booking failed.', 'Booking Error');
      }
    });
  }
}
