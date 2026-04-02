import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { APIService } from '../services/api.service';
import { TokenService } from '../services/token.service';
import { $userStatus } from '../dynamicCommunication/userObservable';
import { HotelModel } from '../models/hotel.model';
import { AmenityModel } from '../models/amenity.model';
import { BookingModel } from '../models/booking.model';
import { HotelAmenityModel } from '../models/hotel-amenity.model';


@Component({
  selector: 'app-home',
  imports: [RouterLink, FormsModule, CommonModule],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home {
  private apiService = inject(APIService);
  private tokenService = inject(TokenService);
  private router = inject(Router);

  hotels   = signal<HotelModel[]>([]);
  loading  = signal(false);
  amenities = signal<AmenityModel[]>([]);
  upcomingBookings = signal<BookingModel[]>([]);
  hotelAmenityMap = signal<Record<number, HotelAmenityModel[]>>({});
  isLoggedIn = () => this.tokenService.isLoggedIn();

  // ── Search ────────────────────────────────────────────────────────────────
  selectedCity    = signal('');
  selectedAmenity = signal('');  // amenity label e.g. "Pool"
  checkIn         = signal('');
  checkOut        = signal('');
  guests          = signal(1);
  today           = new Date().toISOString().split('T')[0];

  // ── Top amenity quick filters ─────────────────────────────────────────────
  readonly topAmenities = [
    { icon: '🏊', label: 'Pool' },
    { icon: '💆', label: 'Spa' },
    { icon: '🐾', label: 'Pet-friendly' },
    { icon: '🏋️', label: 'Gym' },
    { icon: '📶', label: 'Free Wi-Fi' },
    { icon: '🍳', label: 'Breakfast' },
    { icon: '🅿️', label: 'Parking' },
    { icon: '🌊', label: 'Beach Access' },
  ];
  activeAmenityFilter = signal('');

  cities = [
    { name: 'Mumbai', emoji: '🌆', count: 2 },
    { name: 'Delhi', emoji: '🏛️', count: 2 },
    { name: 'Goa', emoji: '🏖️', count: 2 },
    { name: 'Bangalore', emoji: '🌿', count: 2 },
    { name: 'Jaipur', emoji: '🏰', count: 2},
    { name: 'Chennai', emoji: '🌊', count: 2 }
  ];

  constructor() {
    this.getFeaturedHotels();
    this.apiService.apiGetAmenities().subscribe({ next: res => this.amenities.set(res), error: () => {} });
    $userStatus.subscribe(u => {
      if (u.userId > 0 && u.role === 'user') this.loadUpcomingBookings(u.userId);
    });
  }

  loadUpcomingBookings(userId: number): void {
    this.apiService.apiGetBookingsByUser(userId, { pageNumber: 1, pageSize: 3 }).subscribe({
      next: res => this.upcomingBookings.set(
        (res.data ?? []).filter(b => b.status === 'Pending' || b.status === 'Confirmed').slice(0, 3)
      ),
      error: () => {}
    });
  }

  getFeaturedHotels(): void {
    this.loading.set(true);
    this.apiService.apiGetHotelsPaged({ pageNumber: 1, pageSize: 6 }).subscribe({
      next: res => {
        this.hotels.set(res.data || []);
        this.loading.set(false);
        // Load assigned amenities for each featured hotel
        (res.data || []).forEach(h => {
          this.apiService.apiGetHotelAmenitiesByHotel(h.hotelId).subscribe({
            next: a => this.hotelAmenityMap.update(m => ({ ...m, [h.hotelId]: a ?? [] })),
            error: () => {}
          });
        });
      },
      error: () => this.loading.set(false)
    });
  }

  onCityChange(city: string)    { this.selectedCity.set(city); }
  onAmenityChange(label: string) { this.selectedAmenity.set(label); }

  search(): void {
    const query: any = {};
    if (this.selectedCity())    query.location = this.selectedCity();
    if (this.selectedAmenity()) query.amenity  = this.selectedAmenity();
    if (this.checkIn())         query.checkIn   = this.checkIn();
    if (this.checkOut())        query.checkOut  = this.checkOut();
    if (this.guests() > 1)      query.guests    = this.guests();
    this.router.navigate(['/hotels'], { queryParams: query });
  }

  searchByCity(city: string): void {
    this.router.navigate(['/hotels'], { queryParams: { location: city } });
  }

  searchByAmenity(label: string): void {
    this.activeAmenityFilter.set(this.activeAmenityFilter() === label ? '' : label);
    this.router.navigate(['/hotels'], { queryParams: { amenity: label } });
  }

  openChat(): void { window.dispatchEvent(new CustomEvent('stayease:openchat')); }


  bookingStatusClass(s: string): string {
    return ({ Pending: 'badge-pending', Confirmed: 'badge-confirmed' } as Record<string,string>)[s] ?? 'badge-pending';
  }

  /** Always returns an array so templates avoid TS2532 on optional map entries. */
  getHotelAmenities(hotelId: number): HotelAmenityModel[] {
    return this.hotelAmenityMap()[hotelId] ?? [];
  }
}