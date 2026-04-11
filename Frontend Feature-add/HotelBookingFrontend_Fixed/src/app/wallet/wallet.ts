import { Component, inject, signal, computed, OnDestroy } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { WalletModel, WalletTransactionModel } from '../models/wallet.model';
import { $userStatus, UserState } from '../dynamicCommunication/userObservable';

@Component({
  selector: 'app-wallet',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  templateUrl: './wallet.html',
  styleUrl: './wallet.css'
})
export class Wallet implements OnDestroy {
  private api   = inject(APIService);
  private toast = inject(ToastrService);

  private currentUser = signal<UserState>({ userId: 0, userName: '', email: '', role: '' });
  private sub: Subscription;

  wallet  = signal<WalletModel | null>(null);
  loading = signal(true);

  // Filter
  filterType = signal<'All' | 'Credit' | 'Debit'>('All');

  // Pagination
  page     = signal(1);
  readonly PS = 10;

  filtered = computed(() => {
    const txs = this.wallet()?.transactions ?? [];
    return this.filterType() === 'All'
      ? txs
      : txs.filter(t => t.type === this.filterType());
  });

  paged = computed(() => {
    const list = this.filtered();
    return list.slice((this.page() - 1) * this.PS, this.page() * this.PS);
  });

  totalPages = computed(() => Math.ceil(this.filtered().length / this.PS) || 1);

  totalCredits = computed(() =>
    (this.wallet()?.transactions ?? [])
      .filter(t => t.type === 'Credit')
      .reduce((s, t) => s + t.amount, 0)
  );

  totalDebits = computed(() =>
    (this.wallet()?.transactions ?? [])
      .filter(t => t.type === 'Debit')
      .reduce((s, t) => s + t.amount, 0)
  );

  constructor() {
    this.sub = $userStatus.subscribe(u => {
      this.currentUser.set(u);
      if (u.userId > 0) this.loadWallet(u.userId);
    });
  }

  ngOnDestroy(): void { this.sub.unsubscribe(); }

  loadWallet(userId: number): void {
    this.loading.set(true);
    this.api.apiGetWallet(userId).subscribe({
      next: (w: WalletModel) => { this.wallet.set(w); this.loading.set(false); },
      error: () => { this.loading.set(false); this.toast.error('Failed to load wallet.'); }
    });
  }

  refresh(): void {
    const uid = this.currentUser().userId;
    if (uid > 0) this.loadWallet(uid);
  }

  readonly filterOptions: Array<'All' | 'Credit' | 'Debit'> = ['All', 'Credit', 'Debit'];

  setFilter(f: 'All' | 'Credit' | 'Debit'): void {
    this.filterType.set(f);
    this.page.set(1);
  }

  txIcon(type: string): string {
    return type === 'Credit' ? '↑' : '↓';
  }

  txDesc(tx: WalletTransactionModel): string {
    return tx.description || (tx.type === 'Credit' ? 'Credit' : 'Debit');
  }
}
