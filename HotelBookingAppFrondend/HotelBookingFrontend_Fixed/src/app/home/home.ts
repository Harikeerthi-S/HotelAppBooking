import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { APIService } from '../services/api.service';

import { HotelModel } from '../models/hotel.model';
import { AmenityModel } from '../models/amenity.model';

@Component({
  selector: 'app-home',
  imports: [RouterLink, FormsModule],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home {
  private apiService = inject(APIService);
  private router     = inject(Router);

  hotels  = signal<HotelModel[]>([]);
  loading = signal(false);

  // 🔍 SEARCH
  selectedCity    = signal('');
  selectedAmenity = signal<number | null>(null);

  // 🎛️ AMENITIES
  amenities = signal<AmenityModel[]>([]);

  // 🌆 POPULAR CITIES
  cities = [
    { name: 'Mumbai', emoji: '🌆', count: 1240 },
    { name: 'Delhi', emoji: '🏛️', count: 980 },
    { name: 'Goa', emoji: '🏖️', count: 650 },
    { name: 'Bangalore', emoji: '🌿', count: 870 },
    { name: 'Jaipur', emoji: '🏰', count: 430 },
    { name: 'Chennai', emoji: '🌊', count: 560 }
  ];

  constructor() {
    this.getFeaturedHotels();

    // ✅ Load amenities
    this.apiService.apiGetAmenities().subscribe({
      next: res => this.amenities.set(res),
      error: () => {}
    });
  }

  // =====================
  // HOTELS
  // =====================
  getFeaturedHotels() {
    this.loading.set(true);

    this.apiService.apiGetHotelsPaged({ pageNumber: 1, pageSize: 6 }).subscribe({
      next: res => {
        this.hotels.set(res.data || []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  // =====================
  // SEARCH
  // =====================
  onCityChange(city: string) {
    this.selectedCity.set(city);
  }

  onAmenityChange(id: string) {
    this.selectedAmenity.set(id ? +id : null);
  }

  search() {
    const query: any = {};

    if (this.selectedCity()) {
      query.location = this.selectedCity();
    }

    if (this.selectedAmenity()) {
      query.amenityId = this.selectedAmenity();
    }

    this.router.navigate(['/hotels'], { queryParams: query });
  }

  // 🔥 CLICK CITY
  searchByCity(city: string) {
    this.router.navigate(['/hotels'], {
      queryParams: { location: city }
    });
  }

  // =====================
  // IMAGE
  // =====================
  getHotelImage(hotel: HotelModel): string {
    const imgs = [
      'photo-1566073771259-6a8506099945',
      'photo-1551882547-ff40c63fe5fa',
      'photo-1571896349842-33c89424de2d'
    ];

    if (hotel.imagePath?.startsWith('http')) return hotel.imagePath;

    return `https://images.unsplash.com/${imgs[hotel.hotelId % 3]}?w=400&h=200&fit=crop`;
  }
}