import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { BookingRoomModel, CreateBookingRoomModel } from '../models/booking-room.model';
import { RoomModel } from '../models/room.model';
import { BookingModel } from '../models/booking.model';

@Component({
  selector: 'app-booking-room',
  imports: [CommonModule, DatePipe, FormsModule, RouterLink],
  templateUrl: './booking-room.html',
  styleUrl: './booking-room.css'
})
export class BookingRoom implements OnInit {
  private apiService = inject(APIService);
  private toastr     = inject(ToastrService);
  private route      = inject(ActivatedRoute);

  bookingRooms  = signal<BookingRoomModel[]>([]);
  availableRooms = signal<RoomModel[]>([]);
  booking       = signal<BookingModel | null>(null);
  loading       = signal(false);
  saving        = signal(false);
  editId        = signal<number | null>(null);

  bookingId = signal(0);

  // Form
  form = signal<CreateBookingRoomModel>({
    bookingId: 0,
    roomId: 0,
    pricePerNight: 0,
    numberOfRooms: 1
  });

  ngOnInit(): void {
    const id = +this.route.snapshot.params['bookingId'];
    if (id) {
      this.bookingId.set(id);
      this.form.update(f => ({ ...f, bookingId: id }));
      this.loadBookingRooms(id);
      this.loadBooking(id);
    }
    this.loadRooms();
  }

  loadBooking(bookingId: number): void {
    this.apiService.apiGetBookingById(bookingId).subscribe({
      next: b => this.booking.set(b),
      error: () => {}
    });
  }

  loadBookingRooms(bookingId: number): void {
    this.loading.set(true);
    this.apiService.apiGetBookingRoomsByBookingId(bookingId).subscribe({
      next: list => { this.bookingRooms.set(list); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  loadRooms(): void {
    this.apiService.apiGetRooms().subscribe({
      next: rooms => this.availableRooms.set(rooms.filter(r => r.isAvailable)),
      error: () => {}
    });
  }

  getRoomLabel(roomId: number): string {
    const r = this.availableRooms().find(x => x.roomId === roomId);
    return r ? `#${r.roomNumber} ${r.roomType} — ₹${r.pricePerNight}/night` : `Room #${roomId}`;
  }

  onRoomSelect(roomId: number): void {
    const room = this.availableRooms().find(r => r.roomId === roomId);
    this.form.update(f => ({
      ...f,
      roomId,
      pricePerNight: room?.pricePerNight ?? 0
    }));
  }

  save(): void {
    const f = this.form();
    if (!f.roomId) { this.toastr.warning('Please select a room.'); return; }
    if (f.numberOfRooms < 1) { this.toastr.warning('Number of rooms must be at least 1.'); return; }
    if (f.pricePerNight <= 0) { this.toastr.warning('Price per night must be greater than 0.'); return; }

    this.saving.set(true);
    const obs = this.editId()
      ? this.apiService.apiUpdateBookingRoom(this.editId()!, f)
      : this.apiService.apiCreateBookingRoom(f);

    obs.subscribe({
      next: result => {
        if (this.editId()) {
          this.bookingRooms.update(list => list.map(x => x.bookingRoomId === this.editId() ? result : x));
          this.toastr.success('Booking room updated!');
        } else {
          this.bookingRooms.update(list => [...list, result]);
          this.toastr.success('Room added to booking!');
        }
        this.resetForm();
        this.saving.set(false);
      },
      error: (e) => {
        this.saving.set(false);
        this.toastr.error(e?.error?.message || e?.error?.Message || 'Error saving booking room.');
      }
    });
  }

  edit(br: BookingRoomModel): void {
    this.editId.set(br.bookingRoomId);
    this.form.set({
      bookingId:     br.bookingId,
      roomId:        br.roomId,
      pricePerNight: br.pricePerNight,
      numberOfRooms: br.numberOfRooms
    });
  }

  delete(br: BookingRoomModel): void {
    if (!confirm('Remove this room from the booking?')) return;
    this.apiService.apiDeleteBookingRoom(br.bookingRoomId).subscribe({
      next: () => {
        this.bookingRooms.update(list => list.filter(x => x.bookingRoomId !== br.bookingRoomId));
        this.toastr.success('Room removed from booking.');
      },
      error: (e) => this.toastr.error(e?.error?.message || 'Error removing room.')
    });
  }

  resetForm(): void {
    this.editId.set(null);
    this.form.set({ bookingId: this.bookingId(), roomId: 0, pricePerNight: 0, numberOfRooms: 1 });
  }

  getTotalRooms(): number {
    return this.bookingRooms().reduce((s, br) => s + br.numberOfRooms, 0);
  }

  getTotalCost(): number {
    return this.bookingRooms().reduce((s, br) => s + br.pricePerNight * br.numberOfRooms, 0);
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'st-pending', Confirmed: 'st-confirmed',
      Completed: 'st-completed', Cancelled: 'st-cancelled', Refunded: 'st-refunded'
    };
    return map[status] ?? 'st-pending';
  }
}
