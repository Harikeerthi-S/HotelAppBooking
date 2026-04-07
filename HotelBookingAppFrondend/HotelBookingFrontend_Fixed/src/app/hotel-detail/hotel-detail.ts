import { Component, inject, signal, computed, OnDestroy, OnInit } from '@angular/core';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Subscription, interval, of } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';
import { APIService } from '../services/api.service';
import { TokenService } from '../services/token.service';
import { ToastrService } from 'ngx-toastr';
import { HotelModel } from '../models/hotel.model';
import { RoomModel } from '../models/room.model';
import { ReviewModel } from '../models/review.model';
import { HotelAmenityModel } from '../models/hotel-amenity.model';
import { $userStatus, UserState } from '../dynamicCommunication/userObservable';
import { ImgUrlPipe, resolveHotelImage } from '../shared/image.pipe';
@Component({
  selector: 'app-hotel-detail',
  imports: [RouterLink, FormsModule, CommonModule, ImgUrlPipe],
  templateUrl: './hotel-detail.html',
  styleUrl: './hotel-detail.css'
})
export class HotelDetail implements OnInit, OnDestroy {
  private apiService   = inject(APIService);
  private tokenService = inject(TokenService);
  private toastr       = inject(ToastrService);
  private route        = inject(ActivatedRoute);
  private router       = inject(Router);

  hotel        = signal<HotelModel | null>(null);
  rooms        = signal<RoomModel[]>([]);
  reviews      = signal<ReviewModel[]>([]);
  hotelAmenities = signal<HotelAmenityModel[]>([]);
  selectedRoom      = signal<RoomModel | null>(null);
  loading           = signal(true);
  wishlisted        = signal(false);
  roomJustSelected  = signal(false); // triggers animation

  checkIn    = signal('');
  checkOut   = signal('');
  numRooms   = signal(1);
  today      = new Date().toISOString().split('T')[0];

  // Date-range availability for the selected room
  dateAvailability = signal<boolean | null>(null); // null = not checked yet
  dateChecking     = signal(false);

  reviewRating  = signal(0);
  reviewComment = signal('');
  reviewLoading = signal(false);

  // Sidebar options — only 1 room per booking
  sidebarRoomOptions = computed(() => [1]);

  // Can proceed to book: room selected, dates set, not admin-deactivated, dates available
  canBook = computed(() => {
    const r = this.selectedRoom();
    if (!r || !r.isAvailable) return false;
    if (!this.checkIn() || !this.checkOut()) return false;
    return this.dateAvailability() === true;
  });

  isLoggedIn  = () => this.tokenService.isLoggedIn();
  getUserRole = () => this.tokenService.getRoleFromToken() ?? '';

  // FIX: read userId from Observable (localStorage) — not sessionStorage
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
    return (this.selectedRoom()?.pricePerNight ?? 0) * this.nights * this.numRooms();
  }

  constructor() {
    this.sub = $userStatus.subscribe(u => this.currentUser.set(u));
    const id = +this.route.snapshot.params['id'];
    this.loadHotel(id);
  }

  ngOnInit(): void {
    // Poll date-range availability every 20s for the selected room + current dates
    this.pollSub = interval(20000).pipe(
      switchMap(() => {
        const room = this.selectedRoom();
        const ci   = this.checkIn();
        const co   = this.checkOut();
        if (!room || !ci || !co) return of(null);
        return this.apiService.apiCheckRoomAvailability(room.roomId, ci, co).pipe(
          catchError(() => of(null))
        );
      })
    ).subscribe({
      next: res => {
        if (res === null) return;
        this.dateAvailability.set(res.isAvailable);
        if (!res.isAvailable && this.selectedRoom()) {
          this.toastr.warning(
            'The selected room is no longer available for your chosen dates.',
            'Dates Taken'
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

  loadHotel(id: number): void {
    this.apiService.apiGetHotelById(id).subscribe({
      next: h => {
        this.hotel.set(h);
        this.loading.set(false);
        this.loadRooms(id);
        this.loadReviews(id);
        this.checkWishlist(id);
        this.loadHotelAmenities(id);
      },
      error: () => this.loading.set(false)
    });
  }

  loadRooms(hotelId: number): void {
    this.apiService.apiGetRoomsPaged({ pageNumber: 1, pageSize: 100 }, hotelId).subscribe({
      next: r => this.rooms.set(r.data ?? []),
      error: () => {}
    });
  }

  loadHotelAmenities(hotelId: number): void {
    this.apiService.apiGetHotelAmenitiesByHotel(hotelId).subscribe({
      next: a => this.hotelAmenities.set(a ?? []),
      error: () => this.hotelAmenities.set([]) // 404 = no amenities assigned yet
    });
  }

  loadReviews(hotelId: number): void {
    this.apiService.apiGetReviewsPaged({ hotelId }, { pageNumber: 1, pageSize: 20 }).subscribe({
      next: r => this.reviews.set(r.data),
      error: () => {}
    });
  }

  checkWishlist(hotelId: number): void {
    if (!this.tokenService.isLoggedIn()) return;
    // FIX: use currentUser signal (localStorage) — not sessionStorage
    const userId = this.currentUser().userId;
    if (!userId) return;
    this.apiService.apiGetWishlist(userId).subscribe({
      next: list => this.wishlisted.set(list.some(w => w.hotelId === hotelId)),
      error: () => {}
    });
  }

  selectRoom(room: RoomModel): void {
    if (!room.isAvailable) return;
    this.selectedRoom.set(room);
    this.dateAvailability.set(null);
    this.checkDateAvailability();
    // trigger sidebar animation
    this.roomJustSelected.set(false);
    setTimeout(() => this.roomJustSelected.set(true), 10);
    setTimeout(() => this.roomJustSelected.set(false), 900);
  }

  checkDateAvailability(): void {
    const room = this.selectedRoom();
    const ci   = this.checkIn();
    const co   = this.checkOut();
    if (!room || !ci || !co) { this.dateAvailability.set(null); return; }
    this.dateChecking.set(true);
    this.apiService.apiCheckRoomAvailability(room.roomId, ci, co).subscribe({
      next: res => { this.dateAvailability.set(res.isAvailable); this.dateChecking.set(false); },
      error: ()  => { this.dateAvailability.set(null);           this.dateChecking.set(false); }
    });
  }

  toggleWishlist(): void {
    if (!this.tokenService.isLoggedIn()) {
      this.toastr.warning('Please login to save hotels.');
      const hotelDetailUrl = this.router.url;
      sessionStorage.setItem('hb_returnUrl', hotelDetailUrl);
      this.router.navigate(['/login'], { queryParams: { returnUrl: hotelDetailUrl } });
      return;
    }
    // FIX: use currentUser signal (localStorage)
    const userId  = this.currentUser().userId;
    const hotelId = this.hotel()!.hotelId;

    if (!userId) { this.toastr.error('Session expired. Please login again.'); return; }

    if (this.wishlisted()) {
      this.apiService.apiRemoveWishlistByHotel(userId, hotelId).subscribe({
        next: () => { this.wishlisted.set(false); this.toastr.info('Removed from wishlist.'); },
        error: (e) => this.toastr.error(e?.error?.message || 'Error removing from wishlist.')
      });
    } else {
      this.apiService.apiAddToWishlist(userId, hotelId).subscribe({
        next: () => { this.wishlisted.set(true); this.toastr.success('Saved to wishlist! ❤️'); },
        error: (e) => {
          // 409 = already in wishlist — treat as success
          if (e.status === 409) { this.wishlisted.set(true); this.toastr.info('Already in wishlist.'); }
          else this.toastr.error(e?.error?.message || 'Error saving to wishlist.');
        }
      });
    }
  }

  proceedToBook(): void {
    if (!this.canBook()) { this.toastr.warning('Please select a room and available dates.'); return; }

    if (!this.tokenService.isLoggedIn()) {
      this.toastr.warning('Please login to book a room.', 'Login Required');
      // After login, return to this hotel detail page so user can complete booking
      const hotelDetailUrl = this.router.url; // e.g. /hotels/1
      sessionStorage.setItem('hb_returnUrl', hotelDetailUrl);
      this.router.navigate(['/login'], { queryParams: { returnUrl: hotelDetailUrl } });
      return;
    }

    this.router.navigate(
      ['/booking', this.hotel()!.hotelId, this.selectedRoom()!.roomId],
      { queryParams: { checkIn: this.checkIn(), checkOut: this.checkOut(), rooms: this.numRooms() } }
    );
  }

  submitReview(): void {
    if (!this.reviewRating()) {
      this.toastr.warning('Please select a rating (1–5).'); return;
    }
    if (!this.reviewComment().trim()) {
      this.toastr.warning('Please write a comment.'); return;
    }
    if (this.reviewComment().trim().length < 5) {
      this.toastr.warning('Comment must be at least 5 characters.'); return;
    }

    // FIX: use currentUser signal (localStorage) — not sessionStorage
    const userId = this.currentUser().userId;
    if (!userId || userId < 1) {
      this.toastr.error('Session expired. Please login again.');
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    this.reviewLoading.set(true);
    this.apiService.apiCreateReview(
      this.hotel()!.hotelId,
      userId,
      this.reviewRating(),
      this.reviewComment().trim()
    ).subscribe({
      next: (r) => {
        // Prepend new review to the list immediately
        this.reviews.update(list => [r, ...list]);
        this.reviewRating.set(0);
        this.reviewComment.set('');
        this.reviewLoading.set(false);
        this.toastr.success('Review submitted! Thank you.', 'Review Submitted');
      },
      error: (e) => {
        this.reviewLoading.set(false);
        const msg = e?.error?.message || e?.error?.Message || 'Error submitting review.';
        if (e.status === 409) {
          // AlreadyExistsException — one review per hotel per user
          this.toastr.warning('You have already reviewed this hotel.', 'Already Reviewed');
        } else if (e.status === 400) {
          this.toastr.error(msg, 'Validation Error');
        } else {
          this.toastr.error(msg, 'Error');
        }
      }
    });
  }


  getStars(n: number): number[] { return Array.from({ length: Math.round(n) }, (_, i) => i); }

  getRoomIcon(type: string): string {
    const m: Record<string, string> = {
      single: '🛏️', double: '🛋️', suite: '🏰', deluxe: '💎', standard: '🛏️'
    };
    return m[type?.toLowerCase()] ?? '🛏️';
  }

  getImage(hotel: HotelModel): string {
    return resolveHotelImage(hotel.imagePath, hotel.hotelId);
  }
}
