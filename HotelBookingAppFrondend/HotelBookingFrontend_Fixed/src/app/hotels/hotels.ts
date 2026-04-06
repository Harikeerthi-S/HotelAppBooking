import { Component, inject, signal, OnDestroy } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule, SlicePipe } from '@angular/common';
import { Subscription } from 'rxjs';
import { catchError, of } from 'rxjs';

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
  fAmenityId = signal<number | null>(null);

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

  onAmenityChange(val: string): void {
    const amenityId = val ? +val : null;
    this.fAmenityId.set(amenityId);
    
    // Update fAmenity for display purposes
    if (amenityId) {
      const amenity = this.amenities().find(a => a.amenityId === amenityId);
      this.fAmenity.set(amenity?.name || '');
    } else {
      this.fAmenity.set('');
    }
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
  private hasShownAmenityWarning = false;

  constructor() {

    // ✅ USER STATE + ROLE CHECK
    this.sub = $userStatus.subscribe(u => {
      this.currentUser.set(u);

      // 🔥 FIX: Only USER role loads wishlist
      if (u.role === 'user' && u.userId > 0 && this.tokenService.isLoggedIn()) {
        this.loadWishlist(u.userId);
      }
    });

    // ✅ QUERY PARAMS (FROM HOME SEARCH OR AMENITY CLICK)
    this.route.queryParams.subscribe(p => {
      if (p['location'])  this.fLocation.set(p['location']);
      if (p['amenity'])   this.fAmenity.set(p['amenity']);
      if (p['amenityId']) this.fAmenityId.set(+p['amenityId']);
      else                this.fAmenityId.set(null);
      this.loadHotels();
    });

    // ✅ LOAD AMENITIES - Only if authenticated
    this.loadAmenities();
  }

  // ✅ LOAD AMENITIES - Try to load for all users, fallback gracefully
  loadAmenities(): void {
    // Try to load amenities for the dropdown filter
    this.apiService.apiGetAmenities().subscribe({
      next: a => {
        console.log('Loaded amenities for dropdown:', a);
        this.amenities.set(a);
      },
      error: (err) => {
        console.log('Amenities unavailable (likely requires authentication):', err);
        this.amenities.set([]);
        // This is expected for unauthenticated users - the dropdown will be empty but hotels will still show their amenities
      }
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
    const amenityId = this.fAmenityId();

    // Build filter without amenityId (since backend might not support it properly)
    const f: HotelFilter = {
      location:  this.fLocation()  || undefined,
      minRating: this.fMinRating() ? +this.fMinRating() : undefined,
      minPrice:  this.fMinPrice()  ? +this.fMinPrice()  : undefined,
      maxPrice:  this.fMaxPrice()  ? +this.fMaxPrice()  : undefined
    };

    const hasBasicFilter = f.location || f.minRating || f.minPrice || f.maxPrice;

    const obs = hasBasicFilter
      ? this.apiService.apiFilterHotels(f, req)
      : this.apiService.apiGetHotelsPaged(req);

    obs.subscribe({
      next: res => {
        this.paged.set(res);
        let hotels = [...(res.data || [])];

        // Filter by amenity using individual hotel amenity calls (public endpoint)
        if (amenityId) {
          this.filterHotelsByAmenityFallback(hotels, amenityId);
        } else {
          const sorted = this.sortHotels(hotels);
          this.hotels.set(sorted);
          this.loading.set(false);
          this.loadHotelAmenities(sorted);
        }
      },
      error: () => {
        this.loading.set(false);
        this.toastr.error('Failed to load hotels.');
      }
    });
  }

  private loadHotelAmenities(hotels: HotelModel[]): void {
    console.log(`Loading amenities for ${hotels.length} hotels`);
    hotels.forEach(h => {
      this.apiService.apiGetHotelAmenitiesByHotel(h.hotelId).subscribe({
        next: a => {
          console.log(`Hotel ${h.hotelId} (${h.hotelName}) has ${a?.length || 0} amenities:`, a);
          this.hotelAmenityMap.update(m => ({ ...m, [h.hotelId]: a ?? [] }));
        },
        error: (err) => {
          console.error(`Failed to load amenities for hotel ${h.hotelId} (${h.hotelName}):`, err);
          this.hotelAmenityMap.update(m => ({ ...m, [h.hotelId]: [] }));
        }
      });
    });
  }

  private filterHotelsByAmenityFallback(hotels: HotelModel[], amenityId: number): void {
    console.log('Using fallback amenity filtering method');
    let processedCount = 0;
    const matchingHotels: HotelModel[] = [];
    
    if (hotels.length === 0) {
      this.hotels.set([]);
      this.loading.set(false);
      return;
    }

    hotels.forEach(hotel => {
      this.apiService.apiGetHotelAmenitiesByHotel(hotel.hotelId).subscribe({
        next: amenities => {
          processedCount++;
          const hasAmenity = amenities.some(a => a.amenityId === amenityId);
          if (hasAmenity) {
            matchingHotels.push(hotel);
          }
          
          // Update hotel amenity map
          this.hotelAmenityMap.update(m => ({ ...m, [hotel.hotelId]: amenities ?? [] }));
          
          // If all hotels processed, update the display
          if (processedCount === hotels.length) {
            console.log(`Fallback filter: Found ${matchingHotels.length} hotels with amenity ID ${amenityId}`);
            const sorted = this.sortHotels(matchingHotels);
            this.hotels.set(sorted);
            this.loading.set(false);
          }
        },
        error: () => {
          processedCount++;
          // If all hotels processed, update the display
          if (processedCount === hotels.length) {
            console.log(`Fallback filter completed with errors: Found ${matchingHotels.length} hotels`);
            const sorted = this.sortHotels(matchingHotels);
            this.hotels.set(sorted);
            this.loading.set(false);
          }
        }
      });
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
    this.fAmenityId.set(null);
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