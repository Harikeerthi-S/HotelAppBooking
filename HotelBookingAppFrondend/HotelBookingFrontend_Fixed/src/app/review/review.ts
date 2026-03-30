import { Component, inject, signal, computed, OnDestroy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { ReviewModel } from '../models/review.model';
import { HotelModel } from '../models/hotel.model';
import { PagedResponse } from '../models/paged.model';
import { $userStatus, UserState } from '../dynamicCommunication/userObservable';
import { environment } from '../../environments/environment';

interface ReviewItem extends ReviewModel { hotelName: string; }

@Component({
  selector: 'app-review',
  standalone: true,
  imports: [RouterLink, CommonModule, DatePipe, FormsModule],
  templateUrl: './review.html',
  styleUrl: './review.css'
})
export class Review implements OnDestroy {
  private api   = inject(APIService);
  private http  = inject(HttpClient);
  private toast = inject(ToastrService);

  // ── Signals ────────────────────────────────────────────────────────────────
  reviews    = signal<ReviewItem[]>([]);
  hotels     = signal<HotelModel[]>([]);
  loading    = signal(true);
  submitting = signal(false);
  deletingId = signal(0);

  // ── Write-review form ──────────────────────────────────────────────────────
  selectedHotelId = signal(0);
  newRating       = signal(0);
  newComment      = signal('');
  hoveredStar     = signal(0);

  // ── Filters ───────────────────────────────────────────────────────────────
  filterRating  = signal(0);
  filterHotelId = signal(0);

  // ── User ──────────────────────────────────────────────────────────────────
  private currentUser = signal<UserState>({ userId: 0, userName: '', email: '', role: '' });
  get currentUserId(): number { return this.currentUser().userId; }
  get currentUserName(): string { return this.currentUser().userName; }
  private sub: Subscription;

  get isUser()    { return this.currentUser().role === 'user'; }
  get isAdmin()   { return this.currentUser().role === 'admin'; }
  get isManager() { return this.currentUser().role === 'hotelmanager'; }
  get canWrite()  { return this.isUser && this.currentUser().userId > 0; }
  get canDelete() { return this.isAdmin || this.isManager; }

  // ── Computed ───────────────────────────────────────────────────────────────
  filtered = computed(() => {
    let list = this.reviews();
    if (this.filterRating())  list = list.filter(r => r.rating  === this.filterRating());
    if (this.filterHotelId()) list = list.filter(r => r.hotelId === this.filterHotelId());
    return list;
  });

  avgRating = computed(() => {
    const list = this.filtered();
    if (!list.length) return 0;
    return +(list.reduce((s, r) => s + r.rating, 0) / list.length).toFixed(1);
  });

  ratingDist = computed(() =>
    [5, 4, 3, 2, 1].map(star => ({
      star,
      count: this.filtered().filter(r => r.rating === star).length,
      pct:   this.filtered().length
               ? Math.round(this.filtered().filter(r => r.rating === star).length / this.filtered().length * 100)
               : 0
    }))
  );

  constructor() {
    this.sub = $userStatus.subscribe(u => {
      this.currentUser.set(u);
      if (u.userId > 0) this.loadAll();
    });
  }

  ngOnDestroy(): void { this.sub.unsubscribe(); }

  // ── Load hotels + reviews ─────────────────────────────────────────────────
  loadAll(): void {
    this.loading.set(true);
    // Load hotel list first (for name lookup + write-review dropdown)
    this.api.apiGetHotelsPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: res => {
        this.hotels.set(res.data || []);
        this.loadReviews();
      },
      error: () => this.loadReviews()
    });
  }

  loadReviews(): void {
    // POST /api/review/paged?pageNumber=1&pageSize=100
    // Body: ReviewFilterDto — user sees own reviews, admin/manager see all
    const filter  = this.isUser ? { userId: this.currentUser().userId } : {};
    const params  = new HttpParams().set('pageNumber', 1).set('pageSize', 100);

    this.http.post<PagedResponse<ReviewModel>>(
      `${environment.apiUrl}/review/paged`,
      filter,
      { params }
    ).subscribe({
      next: res => {
        const enriched: ReviewItem[] = (res.data || []).map(r => ({
          ...r,
          hotelName: this.hotels().find(h => h.hotelId === r.hotelId)?.hotelName ?? `Hotel #${r.hotelId}`
        }));
        this.reviews.set(enriched);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  // ── Submit new review ──────────────────────────────────────────────────────
  submitReview(): void {
    if (!this.selectedHotelId())          { this.toast.warning('Please select a hotel.'); return; }
    if (!this.newRating())                 { this.toast.warning('Please select a star rating.'); return; }
    if (!this.newComment().trim())         { this.toast.warning('Please write a comment.'); return; }
    if (this.newComment().trim().length < 5) { this.toast.warning('Comment must be at least 5 characters.'); return; }

    const userId = this.currentUser().userId;
    if (!userId) { this.toast.error('Session expired. Please login again.'); return; }

    this.submitting.set(true);
    this.api.apiCreateReview(
      this.selectedHotelId(), userId, this.newRating(), this.newComment().trim()
    ).subscribe({
      next: (r) => {
        const hotelName = this.hotels().find(h => h.hotelId === r.hotelId)?.hotelName ?? `Hotel #${r.hotelId}`;
        this.reviews.update(list => [{ ...r, hotelName }, ...list]);
        this.selectedHotelId.set(0);
        this.newRating.set(0);
        this.newComment.set('');
        this.hoveredStar.set(0);
        this.submitting.set(false);
        this.toast.success('Review submitted successfully!', 'Review Submitted');
      },
      error: (e) => {
        this.submitting.set(false);
        if (e.status === 409)
          this.toast.warning('You have already reviewed this hotel.', 'Already Reviewed');
        else
          this.toast.error(e?.error?.message || 'Failed to submit review.', 'Error');
      }
    });
  }

  // ── Delete review (admin / hotelmanager) ──────────────────────────────────
  deleteReview(r: ReviewItem): void {
    if (!confirm(`Delete review for "${r.hotelName}"?`)) return;
    this.deletingId.set(r.reviewId);
    this.api.apiDeleteReview(r.reviewId).subscribe({
      next: () => {
        this.reviews.update(list => list.filter(x => x.reviewId !== r.reviewId));
        this.deletingId.set(0);
        this.toast.info('Review deleted.');
      },
      error: (e) => {
        this.deletingId.set(0);
        this.toast.error(e?.error?.message || 'Failed to delete review.', 'Error');
      }
    });
  }

  // ── Star helpers ──────────────────────────────────────────────────────────
  starsArr = [1, 2, 3, 4, 5];

  isStarFilled(star: number): boolean {
    return star <= (this.hoveredStar() || this.newRating());
  }
  setRating(s: number): void   { this.newRating.set(s); }
  hoverStar(s: number): void   { this.hoveredStar.set(s); }
  clearHover(): void           { this.hoveredStar.set(0); }

  ratingLabel(r: number): string {
    return ['', 'Poor', 'Fair', 'Good', 'Very Good', 'Excellent'][r] ?? '';
  }

  displayStars(n: number): boolean[] {
    return [1,2,3,4,5].map(s => s <= n);
  }

  getInitials(r: ReviewItem): string {
    if (r.userId === this.currentUserId) {
      return this.currentUserName?.charAt(0).toUpperCase() || 'U';
    }
    return 'G';
  }

  clearFilters(): void { this.filterRating.set(0); this.filterHotelId.set(0); }
}
