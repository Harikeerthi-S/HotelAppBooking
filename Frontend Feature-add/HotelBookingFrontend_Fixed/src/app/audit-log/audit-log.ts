import { Component, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription, interval } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { AuditLogModel } from '../models/audit-log.model';
import { AuditLogFilter } from '../models/filter.model';
import { PagedResponse } from '../models/paged.model';
const PAGE_SIZE = 10;

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule],
  templateUrl: './audit-log.html',
  styleUrl: './audit-log.css'
})
export class AuditLog implements OnInit, OnDestroy {
  private api   = inject(APIService);
  private toast = inject(ToastrService);
  private pollSub: Subscription | null = null;

  logs     = signal<AuditLogModel[]>([]);
  loading  = signal(false);
  page     = signal(1);
  pageMeta = signal<PagedResponse<AuditLogModel> | null>(null);

  filter = signal<AuditLogFilter>({
    userId: undefined,
    action: '',
    entityName: '',
    entityId: undefined,
    fromDate: '',
    toDate: ''
  });

  isFiltered = computed(() => {
    const f = this.filter();
    return !!(f.userId || f.action || f.entityName || f.entityId || f.fromDate || f.toDate);
  });

  readonly entityOptions = [
    'Hotel', 'Room', 'Booking', 'Payment', 'Review', 'Cancellation', 'User', 'Amenity'
  ];

  readonly actionOptions = [
    'BookingCreated', 'BookingConfirmed', 'BookingCompleted', 'BookingCancelled',
    'HotelCreated', 'HotelUpdated', 'HotelDeactivated',
    'RoomCreated', 'RoomUpdated', 'RoomDeactivated',
    'PaymentCreated', 'PaymentStatusUpdated',
    'CancellationRequested', 'CancellationStatusUpdated',
    'ReviewCreated', 'ReviewDeleted'
  ];

  totalLogs   = computed(() => this.pageMeta()?.totalRecords ?? 0);
  uniqueUsers = computed(() => new Set(this.logs().map(l => l.userId)).size);

  ngOnInit(): void {
    this.load(1);
    this.pollSub = interval(30000).pipe(
      switchMap(() => {
        const rawFilter = this.filter();
        const cleanFilter: Record<string, unknown> = {};
        if (rawFilter.action)     cleanFilter['action']     = rawFilter.action;
        if (rawFilter.entityName) cleanFilter['entityName'] = rawFilter.entityName;
        if (rawFilter.userId)     cleanFilter['userId']     = rawFilter.userId;
        if (rawFilter.entityId)   cleanFilter['entityId']   = rawFilter.entityId;
        if (rawFilter.fromDate)   cleanFilter['fromDate']   = rawFilter.fromDate + 'T00:00:00';
        if (rawFilter.toDate)     cleanFilter['toDate']     = rawFilter.toDate   + 'T23:59:59';
        const req = { pageNumber: this.page(), pageSize: PAGE_SIZE };
        const obs = Object.keys(cleanFilter).length > 0
          ? this.api.apiFilterAuditLogsPaged(cleanFilter, req)
          : this.api.apiGetAllAuditLogsPaged(req);
        return obs.pipe(catchError(() => of(null)));
      })
    ).subscribe({
      next: r => {
        if (r) {
          this.logs.set(r.data ?? []);
          this.pageMeta.set(r);
        }
      },
      error: () => {}
    });
  }

  ngOnDestroy(): void { this.pollSub?.unsubscribe(); }

  load(nextPage?: number): void {
    if (nextPage !== undefined) this.page.set(nextPage);

    this.loading.set(true);
    const req = { pageNumber: this.page(), pageSize: PAGE_SIZE };

    const rawFilter = this.filter();
    const cleanFilter: Record<string, unknown> = {};

    if (rawFilter.action)     cleanFilter['action']     = rawFilter.action;
    if (rawFilter.entityName) cleanFilter['entityName'] = rawFilter.entityName;
    if (rawFilter.userId)     cleanFilter['userId']     = rawFilter.userId;
    if (rawFilter.entityId)   cleanFilter['entityId']   = rawFilter.entityId;
    if (rawFilter.fromDate)   cleanFilter['fromDate']   = rawFilter.fromDate + 'T00:00:00';
    if (rawFilter.toDate)     cleanFilter['toDate']     = rawFilter.toDate   + 'T23:59:59';

    const hasFilter = Object.keys(cleanFilter).length > 0;
    const obs = hasFilter
      ? this.api.apiFilterAuditLogsPaged(cleanFilter, req)
      : this.api.apiGetAllAuditLogsPaged(req);

    obs.subscribe({
      next: r => {
        this.logs.set(r.data ?? []);
        this.pageMeta.set(r);
        this.loading.set(false);
      },
      error: (e) => {
        this.toast.error(e?.error?.message || 'Failed to load audit logs.', 'Error');
        this.loading.set(false);
      }
    });
  }

  applyFilter(): void {
    this.load(1);
  }

  clearFilter(): void {
    this.filter.set({ userId: undefined, action: '', entityName: '', entityId: undefined, fromDate: '', toDate: '' });
    this.load(1);
  }

  delete(log: AuditLogModel): void {
    if (!confirm(`Delete audit log #${log.auditLogId}?`)) return;
    const cur = this.page();
    const onlyRowOnPage = this.logs().length === 1;
    this.api.apiDeleteAuditLog(log.auditLogId).subscribe({
      next: () => {
        this.toast.success('Audit log deleted.');
        this.load(onlyRowOnPage && cur > 1 ? cur - 1 : undefined);
      },
      error: e => this.toast.error(e?.error?.message || 'Failed to delete.', 'Error')
    });
  }

  actionClass(action: string): string {
    const a = action.toLowerCase();
    if (a.includes('created') || a.includes('requested')) return 'action-create';
    if (a.includes('updated') || a.includes('confirmed') || a.includes('completed')) return 'action-update';
    if (a.includes('deleted') || a.includes('deactivated') || a.includes('cancelled')) return 'action-delete';
    if (a.includes('login') || a.includes('auth')) return 'action-auth';
    return 'action-default';
  }

  entityClass(entity: string): string {
    const map: Record<string, string> = {
      Hotel: 'entity-hotel', Room: 'entity-room', Booking: 'entity-booking',
      Payment: 'entity-payment', Review: 'entity-review', Cancellation: 'entity-cancel',
      User: 'entity-user', Amenity: 'entity-amenity'
    };
    return map[entity] ?? 'entity-default';
  }

  setFilterField(field: keyof AuditLogFilter, value: string): void {
    this.filter.update(f => ({ ...f, [field]: value || undefined }));
  }
  setFilterNum(field: keyof AuditLogFilter, value: string): void {
    this.filter.update(f => ({ ...f, [field]: value ? +value : undefined }));
  }
}
