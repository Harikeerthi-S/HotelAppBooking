import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { APIService } from '../services/api.service';
import { TokenService } from '../services/token.service';
import { ToastrService } from 'ngx-toastr';
import { RoomModel, CreateRoomModel } from '../models/room.model';
import { HotelModel } from '../models/hotel.model';
import { ImgUrlPipe } from '../shared/image.pipe';

@Component({
  selector: 'app-room',
  standalone: true,
  imports: [RouterLink, FormsModule,ImgUrlPipe],
  templateUrl: './room.html',
  styleUrl: './room.css'
})
export class Room implements OnInit {
  private apiService   = inject(APIService);
  private tokenService = inject(TokenService);
  private toastr       = inject(ToastrService);

  rooms         = signal<RoomModel[]>([]);
  hotels        = signal<HotelModel[]>([]);
  loading       = signal(false);
  saving        = signal(false);
  editRoomId    = signal<number | null>(null);
  filterHotelId = signal(0);

  readonly roomTypes = ['Standard', 'Deluxe', 'Suite', 'Single', 'Double'];

  /* Form state.
     IMPORTANT: rooms use imageUrl (backend RoomResponseDto.ImageUrl).
     Hotels use imagePath (backend HotelResponseDto.ImagePath). Never mix them. */
  rf = signal({
    hotelId:       0,
    roomNumber:    1,
    roomType:      'Standard',
    pricePerNight: 1000,
    capacity:      2,
    imageUrl:      ''
  });

  get isAdmin():   boolean { return this.tokenService.getRoleFromToken() === 'admin'; }
  get isManager(): boolean { return this.tokenService.getRoleFromToken() === 'hotelmanager'; }
  get backRoute(): string  { return this.isAdmin ? '/dashboard-admin' : '/dashboard-manager'; }

  get availableCount(): number { return this.rooms().filter(r =>  r.isAvailable).length; }
  get inactiveCount():  number { return this.rooms().filter(r => !r.isAvailable).length; }

  ngOnInit(): void {
    this.loadHotels();
    this.loadRooms();
  }

  /* GET /api/hotel/paged — populate hotel dropdown */
  loadHotels(): void {
    this.apiService.apiGetHotelsPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: res => this.hotels.set(res.data),
      error: ()  => {}
    });
  }

  /* POST /api/room/all/paged?hotelId={id} — rooms with optional hotel filter */
  loadRooms(): void {
    this.loading.set(true);
    const hotelId = this.filterHotelId() || undefined;
    this.apiService.apiGetRoomsPaged({ pageNumber: 1, pageSize: 100 }, hotelId).subscribe({
      next: res => { this.rooms.set(res.data ?? []); this.loading.set(false); },
      error: ()  => this.loading.set(false)
    });
  }

  applyHotelFilter(): void { this.loadRooms(); }

  /* POST /api/room   — create
     PUT  /api/room/{roomId} — update
     Payload matches backend CreateRoomDto:
       hotelId, roomNumber, roomType, pricePerNight, imageUrl?, capacity */
  saveRoom(): void {
    const f = this.rf();
    if (!f.hotelId)          { this.toastr.warning('Please select a hotel.');             return; }
    if (!f.roomNumber)       { this.toastr.warning('Room number is required.');           return; }
    if (f.pricePerNight <= 0){ this.toastr.warning('Price per night must be greater than 0.'); return; }
    if (f.capacity < 1)      { this.toastr.warning('Capacity must be at least 1.');       return; }

    this.saving.set(true);

    const payload: CreateRoomModel = {
      hotelId:       +f.hotelId,
      roomNumber:    +f.roomNumber,
      roomType:      f.roomType,
      pricePerNight: +f.pricePerNight,
      capacity:      +f.capacity,
      imageUrl:      f.imageUrl || undefined
    };

    const obs = this.editRoomId()
      ? this.apiService.apiUpdateRoom(this.editRoomId()!, payload)
      : this.apiService.apiCreateRoom(payload);

    obs.subscribe({
      next: result => {
        if (this.editRoomId()) {
          this.rooms.update(list => list.map(r => r.roomId === result.roomId ? result : r));
          this.toastr.success('Room updated successfully!');
        } else {
          this.rooms.update(list => [...list, result]);
          this.toastr.success('Room created successfully!');
        }
        this.resetRoom();
        this.saving.set(false);
      },
      error: (e) => {
        this.saving.set(false);
        this.toastr.error(e?.error?.message || e?.error?.Message || 'Error saving room. Please try again.');
      }
    });
  }

  editRoom(r: RoomModel): void {
    this.editRoomId.set(r.roomId);
    this.rf.set({
      hotelId:       r.hotelId,
      roomNumber:    r.roomNumber,
      roomType:      r.roomType,
      pricePerNight: r.pricePerNight,
      capacity:      r.capacity,
      imageUrl:      r.imageUrl ?? ''   // imageUrl for rooms (NOT imagePath)
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  /* DELETE /api/room/{roomId} — soft-deactivates on the backend */
  deactivateRoom(r: RoomModel): void {
    if (!confirm(`Deactivate Room #${r.roomNumber} (${r.roomType})?`)) return;
    this.apiService.apiDeleteRoom(r.roomId).subscribe({
      next: () => {
        this.rooms.update(list =>
          list.map(x => x.roomId === r.roomId ? { ...x, isAvailable: false } : x)
        );
        this.toastr.success('Room deactivated.');
      },
      error: (e) => this.toastr.error(e?.error?.message || 'Error deactivating room.')
    });
  }

  resetRoom(): void {
    this.editRoomId.set(null);
    this.rf.set({
      hotelId: 0, roomNumber: 1, roomType: 'Standard',
      pricePerNight: 1000, capacity: 2, imageUrl: ''
    });
  }

  getHotelName(hotelId: number): string {
    return this.hotels().find(h => h.hotelId === hotelId)?.hotelName ?? `Hotel #${hotelId}`;
  }

  getRoomIcon(type: string): string {
    const map: Record<string, string> = {
      standard: '🛏️', single: '🛏️', double: '🛋️', suite: '🏰', deluxe: '💎'
    };
    return map[type?.toLowerCase()] ?? '🛏️';
  }

  getStatusClass(isAvailable: boolean): string {
    return isAvailable ? 'badge-confirmed' : 'badge-cancelled';
  }
}
