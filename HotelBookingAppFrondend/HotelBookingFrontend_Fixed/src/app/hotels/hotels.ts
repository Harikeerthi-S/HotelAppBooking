import { Component, inject, signal, OnDestroy } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule, SlicePipe } from '@angular/common';
import { Subscription } from 'rxjs';

import { APIService } from '../services/api.service';
import { TokenService } from '../services/token.service';
import { ToastrService } from 'ngx-toastr';

import { HotelModel } from '../models/hotel.model';
import { AmenityModel } from '../models/amenity.model';
import { HotelAmenityModel } from '../models/hotel-amenity.model';
import { PagedResponse } from '../models/paged.model';
import { HotelFilter } from '../models/filter.model';
import { $userStatus, UserState } from '../dynamicCommunication/userObservable';
import { resolveHotelImage } from '../shared/image.pipe';

@Component({
  selector: 'app-hotels',
  imports: [RouterLink, FormsModule, CommonModule, SlicePipe],
  templateUrl: './hotels.html',
  styleUrl: './hotels.css'
})
export class Hotels implements OnDestroy {

  private apiService   = inject(APIService);
  private tokenService = inject(TokenService);
  private toastr       = inject(ToastrService);
  private route        = inject(ActivatedRoute);

  hotels      = signal<HotelModel[]>([]);
  amenities   = signal<AmenityModel[]>([]);
  // Map of hotelId → assigned amenities
  hotelAmenityMap = signal<Record<number, HotelAmenityModel[]>>({});
  paged       = signal<PagedResponse<HotelModel> | null>(null);
  loading     = signal(false);

  // ✅ Wishlist (User only)
  wishlistIds = signal<Set<number>>(new Set());

  sortBy   = signal('rating');

  filter     = signal<HotelFilter>({});
  fLocation  = signal('');
  fMinRating = signal('');
  fMinPrice  = signal('');
  fMaxPrice  = signal('');
  fAmenity   = signal('');

  readonly cityOptions = [
    'Mumbai', 'Delhi', 'Goa', 'Bangalore', 'Jaipur',
    'Chennai', 'Hyderabad', 'Kolkata', 'Pune', 'Ahmedabad'
  ];

  readonly priceRanges = [
    { label: 'Under ₹1,000',       min: '',     max: '1000'  },
    { label: '₹1,000 – ₹3,000',   min: '1000', max: '3000'  },
    { label: '₹3,000 – ₹6,000',   min: '3000', max: '6000'  },
    { label: '₹6,000 – ₹10,000',  min: '6000', max: '10000' },
    { label: 'Above ₹10,000',      min: '10000',max: ''      },
  ];

  selectedPriceRange = signal('');

  onPriceRangeChange(val: string): void {
    this.selectedPriceRange.set(val);
    if (!val) { this.fMinPrice.set(''); this.fMaxPrice.set(''); return; }
    const found = this.priceRanges.find(r => `${r.min}-${r.max}` === val);
    if (found) { this.fMinPrice.set(found.min); this.fMaxPrice.set(found.max); }
  }

  readonly amenityChips = [
    { icon: '🏊', label: 'Pool' },
    { icon: '💆', label: 'Spa' },
    { icon: '🐾', label: 'Pet-friendly' },
    { icon: '🏋️', label: 'Gym' },
    { icon: '📶', label: 'Free Wi-Fi' },
    { icon: '🍳', label: 'Breakfast' },
  ];

  // ✅ User State
  private currentUser = signal<UserState>({
    userId: 0,
    userName: '',
    email: '',
    role: ''
  });

  private sub: Subscription;

  constructor() {

    // ✅ USER STATE + ROLE CHECK
    this.sub = $userStatus.subscribe(u => {
      this.currentUser.set(u);

      // 🔥 FIX: Only USER role loads wishlist
      if (u.role === 'user' && u.userId > 0 && this.tokenService.isLoggedIn()) {
        this.loadWishlist(u.userId);
      }
    });

    // ✅ QUERY PARAMS (FROM HOME SEARCH)
    this.route.queryParams.subscribe(p => {
      if (p['location']) this.fLocation.set(p['location']);
      if (p['amenity'])  this.fAmenity.set(p['amenity']);
      this.loadHotels();
    });

    // ✅ LOAD AMENITIES
    this.apiService.apiGetAmenities().subscribe({
      next: a => this.amenities.set(a),
      error: () => {}
    });
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  // =====================
  // LOAD HOTELS
  // =====================
  loadHotels(): void {
    this.loading.set(true);

    const req = { pageNumber: 1, pageSize: 10000 };

    const f: HotelFilter = {
      location:  this.fLocation()  || undefined,
      minRating: this.fMinRating() ? +this.fMinRating() : undefined,
      minPrice:  this.fMinPrice()  ? +this.fMinPrice()  : undefined,
      maxPrice:  this.fMaxPrice()  ? +this.fMaxPrice()  : undefined
    };

    const hasFilter = f.location || f.minRating || f.minPrice || f.maxPrice;

    const obs = hasFilter
      ? this.apiService.apiFilterHotels(f, req)
      : this.apiService.apiGetHotelsPaged(req);

    obs.subscribe({
      next: res => {
        this.paged.set(res);
        const sorted = this.sortHotels([...res.data]);
        this.hotels.set(sorted);
        this.loading.set(false);
        // Load amenities for each hotel
        sorted.forEach(h => {
          this.apiService.apiGetHotelAmenitiesByHotel(h.hotelId).subscribe({
            next: a => this.hotelAmenityMap.update(m => ({ ...m, [h.hotelId]: a ?? [] })),
            error: () => {}
          });
        });
      },
      error: () => {
        this.loading.set(false);
        this.toastr.error('Failed to load hotels.');
      }
    });
  }

  applyFilter(): void {
    this.loadHotels();
  }

  clearFilter(): void {
    this.fLocation.set('');
    this.fMinRating.set('');
    this.fMinPrice.set('');
    this.fMaxPrice.set('');
    this.fAmenity.set('');
    this.selectedPriceRange.set('');
    this.loadHotels();
  }

  onSortChange(): void {
    this.hotels.update(h => this.sortHotels([...h]));
  }

  sortHotels(list: HotelModel[]): HotelModel[] {
    return this.sortBy() === 'rating'
      ? list.sort((a, b) => b.starRating - a.starRating)
      : list.sort((a, b) => a.hotelName.localeCompare(b.hotelName));
  }

  getHotelAmenities(hotelId: number) {
    return this.hotelAmenityMap()[hotelId] ?? [];
  }

  getImage(hotel: HotelModel): string {
    return resolveHotelImage(hotel.imagePath, hotel.hotelId);
  }

  // =====================
  // ✅ WISHLIST (USER ONLY)
  // =====================
  isUser(): boolean {
    return this.currentUser().role === 'user';
  }

  loadWishlist(userId: number): void {
    if (!this.isUser()) return;

    this.apiService.apiGetWishlist(userId).subscribe({
      next: list => {
        this.wishlistIds.set(new Set(list.map(w => w.hotelId)));
      },
      error: () => {}
    });
  }

  isWishlisted(hotelId: number): boolean {
    return this.wishlistIds().has(hotelId);
  }

  toggleWishlist(hotel: HotelModel, e: Event): void {
    e.stopPropagation();

    // 🔥 BLOCK ADMIN & MANAGER
    if (!this.isUser()) {
      this.toastr.warning('Only users can use wishlist.');
      return;
    }

    if (!this.tokenService.isLoggedIn()) {
      this.toastr.warning('Please login to save hotels.');
      return;
    }

    const userId = this.currentUser().userId;

    if (!userId) {
      this.toastr.warning('Please login to save hotels.');
      return;
    }

    if (this.isWishlisted(hotel.hotelId)) {
      this.apiService.apiRemoveWishlistByHotel(userId, hotel.hotelId).subscribe({
        next: () => {
          this.wishlistIds.update(s => {
            const n = new Set(s);
            n.delete(hotel.hotelId);
            return n;
          });
          this.toastr.info('Removed from wishlist.');
        },
        error: () => this.toastr.error('Error removing from wishlist.')
      });
    } else {
      this.apiService.apiAddToWishlist(userId, hotel.hotelId).subscribe({
        next: () => {
          this.wishlistIds.update(s => new Set([...s, hotel.hotelId]));
          this.toastr.success('Saved to wishlist! ❤️');
        },
        error: () => this.toastr.error('Error saving to wishlist.')
      });
    }
  }
}