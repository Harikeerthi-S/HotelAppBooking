import { Component, inject, signal, OnDestroy } from '@angular/core';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { APIService } from '../services/api.service';
import { TokenService } from '../services/token.service';
import { ToastrService } from 'ngx-toastr';
import { HotelModel } from '../models/hotel.model';
import { RoomModel } from '../models/room.model';
import { ReviewModel } from '../models/review.model';
import { $userStatus, UserState } from '../dynamicCommunication/userObservable';

@Component({
  selector: 'app-hotel-detail',
  imports: [RouterLink, FormsModule],
  templateUrl: './hotel-detail.html',
  styleUrl: './hotel-detail.css'
})
export class HotelDetail implements OnDestroy {
  private apiService   = inject(APIService);
  private tokenService = inject(TokenService);
  private toastr       = inject(ToastrService);
  private route        = inject(ActivatedRoute);
  private router       = inject(Router);

  hotel        = signal<HotelModel | null>(null);
  rooms        = signal<RoomModel[]>([]);
  reviews      = signal<ReviewModel[]>([]);
  selectedRoom = signal<RoomModel | null>(null);
  loading      = signal(true);
  wishlisted   = signal(false);

  checkIn    = signal('');
  checkOut   = signal('');
  numRooms   = signal(1);
  today      = new Date().toISOString().split('T')[0];

  reviewRating  = signal(0);
  reviewComment = signal('');
  reviewLoading = signal(false);

  isLoggedIn  = () => this.tokenService.isLoggedIn();
  getUserRole = () => this.tokenService.getRoleFromToken() ?? '';

  // FIX: read userId from Observable (localStorage) — not sessionStorage
  private currentUser = signal<UserState>({ userId: 0, userName: '', email: '', role: '' });
  private sub: Subscription;

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

  ngOnDestroy(): void { this.sub.unsubscribe(); }

  loadHotel(id: number): void {
    this.apiService.apiGetHotelById(id).subscribe({
      next: h => {
        this.hotel.set(h);
        this.loading.set(false);
        this.loadRooms(id);
        this.loadReviews(id);
        this.checkWishlist(id);
      },
      error: () => this.loading.set(false)
    });
  }

  loadRooms(hotelId: number): void {
    this.apiService.apiGetRooms(hotelId).subscribe({
      next: r => this.rooms.set(r),
      error: () => {}
    });
  }

  loadReviews(hotelId: number): void {
    this.apiService.apiGetReviewsPaged(hotelId, { pageNumber: 1, pageSize: 20 }).subscribe({
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
  }

  toggleWishlist(): void {
    if (!this.tokenService.isLoggedIn()) {
      this.toastr.warning('Please login to save hotels.');
      this.router.navigateByUrl('/login');
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
    if (!this.tokenService.isLoggedIn()) { this.router.navigateByUrl('/login'); return; }
    if (!this.selectedRoom())            { this.toastr.warning('Please select a room.'); return; }
    if (!this.checkIn() || !this.checkOut()) {
      this.toastr.warning('Please select check-in and check-out dates.'); return;
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
      this.router.navigateByUrl('/login');
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

  getImage(): string {
    const imgs = ['photo-1566073771259-6a8506099945','photo-1551882547-ff40c63fe5fa','photo-1571896349842-33c89424de2d'];
    const h = this.hotel();
    if (h?.imagePath?.startsWith('http')) return h.imagePath;
    return `https://images.unsplash.com/${imgs[(h?.hotelId ?? 0) % 3]}?w=1200&h=380&fit=crop`;
  }

  getStars(n: number): number[] { return Array.from({ length: Math.round(n) }, (_, i) => i); }

  getRoomIcon(type: string): string {
    const m: Record<string, string> = {
      single: '🛏️', double: '🛋️', suite: '🏰', deluxe: '💎', standard: '🛏️'
    };
    return m[type?.toLowerCase()] ?? '🛏️';
  }
}
