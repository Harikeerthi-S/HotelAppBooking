import { Component, inject, signal, computed, OnDestroy } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of, Subscription } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { CancellationModel } from '../models/cancellation.model';
import { BookingModel } from '../models/booking.model';
import { $userStatus, UserState } from '../dynamicCommunication/userObservable';

interface CancellationItem extends CancellationModel { hotelName: string; }

@Component({
  selector: 'app-cancellation',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule],
  templateUrl: './cancellation.html',
  styleUrl: './cancellation.css'
})
export class Cancellation implements OnDestroy {
  private api   = inject(APIService);
  private toast = inject(ToastrService);

  cancellations = signal<CancellationItem[]>([]);
  bookings      = signal<BookingModel[]>([]);
  loading       = signal(true);
  page          = signal(1);
  totalPages    = signal(1);

  formBookingId   = signal(0);
  formReason      = signal('');
  showForm        = signal(false);
  submitting      = signal(false);

  activeItem      = signal<CancellationItem | null>(null);
  modalStatus     = signal('');
  modalRefund     = signal(0);
  showStatusModal = signal(false);
  updating        = signal(false);

  filterStatus = signal('');

  // ── User state as plain signal — NO getter called with () ─────────────────
  private _user = signal<UserState>({ userId: 0, userName: '', email: '', role: '' });
  private sub: Subscription;

  // Plain boolean properties — template reads these as expressions, not calls
  get isUser():  boolean { return this._user().role === 'user'; }
  get isAdmin(): boolean { return this._user().role === 'admin'; }
  // userId as a property (not a no-arg method call)
  private get _uid(): number { return this._user().userId; }

  filtered = computed(() => {
    const s = this.filterStatus();
    return s ? this.cancellations().filter(c => c.status === s) : this.cancellations();
  });

  totalPending  = computed(() => this.cancellations().filter(c => c.status === 'Pending').length);
  totalApproved = computed(() => this.cancellations().filter(c => c.status === 'Approved').length);
  totalRefund   = computed(() =>
    this.cancellations().filter(c => c.status === 'Approved')
      .reduce((sum, c) => sum + (c.refundAmount ?? 0), 0)
  );

  readonly STATUS_OPTIONS = ['Pending', 'Approved', 'Rejected'];

  constructor() {
    this.sub = $userStatus.subscribe(u => {
      this._user.set(u);
      if (u.userId > 0) this.loadAll(1);
    });
  }

  ngOnDestroy(): void { this.sub.unsubscribe(); }

  loadAll(p: number): void {
    const uid = this._uid;
    if (!uid) return;
    this.loading.set(true);
    this.page.set(p);

    this.api.apiGetCancellationsByUser(uid, { pageNumber: p, pageSize: 10 }).subscribe({
      next: res => {
        this.totalPages.set(res.totalPages || 1);
        const items = res.data ?? [];
        if (!items.length) { this.cancellations.set([]); this.loading.set(false); return; }

        const reqs = items.map(c =>
          this.api.apiGetBookingById(c.bookingId).pipe(
            map(b => ({ ...c, hotelName: b.hotelName ?? `Booking #${c.bookingId}` } as CancellationItem)),
            catchError(() => of({ ...c, hotelName: `Booking #${c.bookingId}` } as CancellationItem))
          )
        );
        forkJoin(reqs).subscribe({
          next: enriched => { this.cancellations.set(enriched); this.loading.set(false); },
          error: ()       => this.loading.set(false)
        });
      },
      error: () => this.loading.set(false)
    });

    if (this.isUser) {
      this.api.apiGetBookingsByUser(uid, { pageNumber: 1, pageSize: 50 }).subscribe({
        next: res => this.bookings.set(
          (res.data ?? []).filter(b => b.status === 'Pending' || b.status === 'Confirmed')
        ),
        error: () => {}
      });
    }
  }

  goPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.loadAll(p);
  }

  openForm(): void  { this.formBookingId.set(0); this.formReason.set(''); this.showForm.set(true); }
  closeForm(): void { this.showForm.set(false); }

  submitCancellation(): void {
    if (!this.formBookingId())              { this.toast.warning('Please select a booking.'); return; }
    if (!this.formReason().trim())          { this.toast.warning('Please enter a reason.'); return; }
    if (this.formReason().trim().length < 5){ this.toast.warning('Reason must be at least 5 characters.'); return; }

    this.submitting.set(true);
    this.api.apiCreateCancellation(this.formBookingId(), this.formReason().trim()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.showForm.set(false);
        this.toast.success('Cancellation request submitted.', 'Request Sent');
        this.loadAll(1);
      },
      error: e => {
        this.submitting.set(false);
        this.toast.error(e?.error?.message || 'Failed to submit cancellation.', 'Error');
      }
    });
  }

  openStatusModal(item: CancellationItem): void {
    this.activeItem.set(item);
    this.modalStatus.set(item.status);
    this.modalRefund.set(item.refundAmount ?? 0);
    this.showStatusModal.set(true);
  }
  closeStatusModal(): void { this.showStatusModal.set(false); this.activeItem.set(null); }

  updateStatus(): void {
    const item = this.activeItem();
    if (!item)                  { this.toast.warning('No cancellation selected.'); return; }
    if (!this.modalStatus())    { this.toast.warning('Please select a status.'); return; }
    if (this.modalRefund() < 0) { this.toast.warning('Refund amount cannot be negative.'); return; }

    this.updating.set(true);
    this.api.apiUpdateCancellationStatus(item.cancellationId, this.modalStatus(), this.modalRefund()).subscribe({
      next: updated => {
        this.cancellations.update(list =>
          list.map(x => x.cancellationId === updated.cancellationId
            ? { ...x, status: updated.status, refundAmount: updated.refundAmount }
            : x
          )
        );
        this.updating.set(false);
        this.closeStatusModal();
        this.toast.success(`Status updated to "${updated.status}".`, 'Updated');
      },
      error: e => {
        this.updating.set(false);
        this.toast.error(e?.error?.message || 'Failed to update status.', 'Error');
      }
    });
  }

  statusClass(s: string): string {
    return ({ Pending: 'cs-pending', Approved: 'cs-approved', Rejected: 'cs-rejected' } as Record<string, string>)[s] ?? 'cs-pending';
  }
  statusIcon(s: string): string {
    return ({ Pending: '⏳', Approved: '✅', Rejected: '❌' } as Record<string, string>)[s] ?? '📋';
  }
  bookingLabel(b: BookingModel): string {
    return `#${b.bookingId} — ${b.hotelName ?? 'Hotel'} (${b.status})`;
  }
}
