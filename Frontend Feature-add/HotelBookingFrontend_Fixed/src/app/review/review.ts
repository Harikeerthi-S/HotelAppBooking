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
import { resolveMediaUrl } from '../shared/media.util';

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

  reviews    = signal<ReviewItem[]>([]);
  hotels     = signal<HotelModel[]>([]);
  loading    = signal(true);
  submitting = signal(false);
  deletingId = signal(0);

  // ── Write-review form ─────────────────────────────────────────────────────
  selectedHotelId = signal(0);
  newRating       = signal(0);
  newComment      = signal('');
  hoveredStar     = signal(0);

  // Photo upload
  selectedPhoto   = signal<File | null>(null);
  photoPreview    = signal<string | null>(null);
  uploadingPhoto  = signal(false);
  pendingReviewId = signal(0);   // reviewId waiting for photo upload

  // ── Filters ───────────────────────────────────────────────────────────────
  filterRating  = signal(0);
  filterHotelId = signal(0);

  // ── User ──────────────────────────────────────────────────────────────────
  private currentUser = signal<UserState>({ userId: 0, userName: '', email: '', role: '' });
  get currentUserId(): number  { return this.currentUser().userId; }
  get currentUserName(): string { return this.currentUser().userName; }
  private sub: Subscription;

  get isUser()    { return this.currentUser().role === 'user'; }
  get isAdmin()   { return this.currentUser().role === 'admin'; }
  get isManager() { return this.currentUser().role === 'hotelmanager'; }
  get canWrite()  { return this.isUser && this.currentUser().userId > 0; }
  get canDelete() { return this.isAdmin || this.isManager; }

  // ── Computed ──────────────────────────────────────────────────────────────
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

  loadAll(): void {
    this.loading.set(true);
    this.api.apiGetHotelsPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: res => { this.hotels.set(res.data || []); this.loadReviews(); },
      error: () => this.loadReviews()
    });
  }

  loadReviews(): void {
    const filter = this.isUser ? { userId: this.currentUser().userId } : {};
    const params = new HttpParams().set('pageNumber', 1).set('pageSize', 100);
    this.http.post<PagedResponse<ReviewModel>>(
      `${environment.apiUrl}/review/paged`, filter, { params }
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

  // ── Photo picker ──────────────────────────────────────────────────────────
  onPhotoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    const allowed = ['image/jpeg', 'image/png', 'image/webp'];
    if (!allowed.includes(file.type)) {
      this.toast.warning('Only JPG, PNG or WebP images are allowed.');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.toast.warning('Image must be ≤ 5 MB.');
      return;
    }
    this.selectedPhoto.set(file);
    const reader = new FileReader();
    reader.onload = e => this.photoPreview.set(e.target?.result as string);
    reader.readAsDataURL(file);
  }

  removePhoto(): void {
    this.selectedPhoto.set(null);
    this.photoPreview.set(null);
  }

  // ── Submit review ─────────────────────────────────────────────────────────
  submitReview(): void {
    if (!this.selectedHotelId())             { this.toast.warning('Please select a hotel.'); return; }
    if (!this.newRating())                   { this.toast.warning('Please select a star rating.'); return; }
    if (!this.newComment().trim())           { this.toast.warning('Please write a comment.'); return; }
    if (this.newComment().trim().length < 5) { this.toast.warning('Comment must be at least 5 characters.'); return; }

    const userId = this.currentUser().userId;
    if (!userId) { this.toast.error('Session expired. Please login again.'); return; }

    this.submitting.set(true);

    this.api.apiCreateReview(
      this.selectedHotelId(), userId, this.newRating(), this.newComment().trim()
    ).subscribe({
      next: (r) => {
        const hotelName = this.hotels().find(h => h.hotelId === r.hotelId)?.hotelName ?? `Hotel #${r.hotelId}`;

        // If a photo was selected, upload it now
        if (this.selectedPhoto()) {
          this.pendingReviewId.set(r.reviewId);
          this.uploadingPhoto.set(true);
          this.api.apiUploadReviewPhoto(r.reviewId, this.selectedPhoto()!).subscribe({
            next: (updated) => {
              this.reviews.update(list => [{ ...updated, hotelName }, ...list]);
              this.toast.success(`Review submitted! 📸 +${updated.coinsEarned} coins added to your wallet.`, 'Review + Photo');
              this.resetForm();
            },
            error: () => {
              // Review saved, photo failed — still show review
              this.reviews.update(list => [{ ...r, hotelName }, ...list]);
              this.toast.warning('Review saved, but photo upload failed. Try uploading the photo again.', 'Partial Success');
              this.resetForm();
            }
          });
        } else {
          this.reviews.update(list => [{ ...r, hotelName }, ...list]);
          this.toast.success('Review submitted!', 'Done');
          this.resetForm();
        }
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

  private resetForm(): void {
    this.selectedHotelId.set(0);
    this.newRating.set(0);
    this.newComment.set('');
    this.hoveredStar.set(0);
    this.selectedPhoto.set(null);
    this.photoPreview.set(null);
    this.uploadingPhoto.set(false);
    this.pendingReviewId.set(0);
    this.submitting.set(false);
  }

  // ── Upload photo to existing review ──────────────────────────────────────
  uploadPhotoForReview(r: ReviewItem, event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.uploadingPhoto.set(true);
    this.api.apiUploadReviewPhoto(r.reviewId, file).subscribe({
      next: (updated) => {
        this.reviews.update(list => list.map(x =>
          x.reviewId === r.reviewId ? { ...updated, hotelName: r.hotelName } : x
        ));
        this.uploadingPhoto.set(false);
        this.toast.success(`📸 Photo added! +${updated.coinsEarned} coins credited to your wallet.`, 'Photo Uploaded');
      },
      error: (e) => {
        this.uploadingPhoto.set(false);
        this.toast.error(e?.error?.message || 'Photo upload failed.', 'Error');
      }
    });
  }

  // ── Delete ────────────────────────────────────────────────────────────────
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
  isStarFilled(star: number): boolean { return star <= (this.hoveredStar() || this.newRating()); }
  setRating(s: number): void   { this.newRating.set(s); }
  hoverStar(s: number): void   { this.hoveredStar.set(s); }
  clearHover(): void           { this.hoveredStar.set(0); }
  ratingLabel(r: number): string { return ['', 'Poor', 'Fair', 'Good', 'Very Good', 'Excellent'][r] ?? ''; }
  displayStars(n: number): boolean[] { return [1,2,3,4,5].map(s => s <= n); }

  getInitials(r: ReviewItem): string {
    return r.userId === this.currentUserId
      ? (this.currentUserName?.charAt(0).toUpperCase() || 'U')
      : 'G';
  }

  clearFilters(): void { this.filterRating.set(0); this.filterHotelId.set(0); }

  getPhotoSrc(url: string | null): string {
    return resolveMediaUrl(url);
  }
}
