import { Component, inject, signal, OnDestroy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of, Subscription } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { HotelModel } from '../models/hotel.model';
import { WishlistModel } from '../models/wishlist.model';
import { $userStatus } from '../dynamicCommunication/userObservable';

interface WishlistItem extends WishlistModel { hotel?: HotelModel; }

@Component({
  selector: 'app-wishlist',
  imports: [RouterLink],
  templateUrl: './wishlist.html',
  styleUrl: './wishlist.css'
})
export class Wishlist implements OnDestroy {
  private apiService = inject(APIService);
  private toastr     = inject(ToastrService);

  items   = signal<WishlistItem[]>([]);
  loading = signal(true);
  userId  = signal(0);

  private sub: Subscription;

  constructor() {
    // FIX: read userId from Observable backed by localStorage (survives refresh)
    // Old code used sessionStorage directly → userId=0 after refresh → 403
    this.sub = $userStatus.subscribe(u => {
      this.userId.set(u.userId);
      if (u.userId > 0) this.loadWishlist();
      else              this.loading.set(false);
    });
  }

  ngOnDestroy(): void { this.sub.unsubscribe(); }

  loadWishlist(): void {
    this.loading.set(true);
    this.apiService.apiGetWishlist(this.userId()).subscribe({
      next: (list) => {
        if (!list.length) { this.items.set([]); this.loading.set(false); return; }
        // Load hotel details in parallel
        const reqs = list.map(w =>
          this.apiService.apiGetHotelById(w.hotelId).pipe(
            map(hotel => ({ ...w, hotel } as WishlistItem)),
            catchError(() => of({ ...w } as WishlistItem))
          )
        );
        forkJoin(reqs).subscribe({
          next: enriched => { this.items.set(enriched); this.loading.set(false); },
          error: ()       => this.loading.set(false)
        });
      },
      error: (e) => {
        this.toastr.error(e?.error?.message || 'Failed to load wishlist.', 'Error');
        this.loading.set(false);
      }
    });
  }

  removeItem(item: WishlistItem): void {
    this.apiService.apiRemoveWishlist(item.wishlistId).subscribe({
      next: () => {
        this.items.update(list => list.filter(x => x.wishlistId !== item.wishlistId));
        this.toastr.info(`${item.hotel?.hotelName ?? 'Hotel'} removed from wishlist.`);
      },
      error: (e) => this.toastr.error(e?.error?.message || 'Error removing item.')
    });
  }

  clearAll(): void {
    if (!confirm('Remove all hotels from your wishlist?')) return;
    Promise.all(this.items().map(i =>
      this.apiService.apiRemoveWishlist(i.wishlistId).toPromise().catch(() => {})
    )).then(() => { this.items.set([]); this.toastr.success('Wishlist cleared.'); });
  }

  getImage(hotel?: HotelModel): string {
    const imgs = ['photo-1566073771259-6a8506099945','photo-1551882547-ff40c63fe5fa','photo-1571896349842-33c89424de2d'];
    if (hotel?.imagePath?.startsWith('http')) return hotel.imagePath;
    return `https://images.unsplash.com/${imgs[(hotel?.hotelId ?? 0) % 3]}?w=400&h=200&fit=crop`;
  }

  getPrice(hotel?: HotelModel): string {
    return (800 + (hotel?.starRating ?? 3) * 500).toLocaleString('en-IN');
  }
}
