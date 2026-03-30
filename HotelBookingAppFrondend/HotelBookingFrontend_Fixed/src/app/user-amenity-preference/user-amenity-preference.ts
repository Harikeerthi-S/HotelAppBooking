import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { AmenityModel } from '../models/amenity.model';
import { UserAmenityPreferenceModel } from '../models/user-amenity-preference.model';

@Component({
  selector: 'app-user-amenity-preference',
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './user-amenity-preference.html',
  styleUrl: './user-amenity-preference.css'
})
export class UserAmenityPreference implements OnInit {
  private api    = inject(APIService);
  private toastr = inject(ToastrService);

  preferences  = signal<UserAmenityPreferenceModel[]>([]);
  amenities    = signal<AmenityModel[]>([]);
  loading      = signal(true);
  submitting   = signal(false);
  selectedId   = signal<number | null>(null);
  filterStatus = signal<string>('all');

  private userId = 0;
  private role   = '';

  ngOnInit(): void {
    this.userId = Number(localStorage.getItem('userId') ?? 0);
    this.role   = localStorage.getItem('role') ?? '';
    this.loadAmenities();
    this.loadPreferences();
  }

  get isAdmin(): boolean { return this.role === 'admin'; }

  get filtered(): UserAmenityPreferenceModel[] {
    const s = this.filterStatus();
    if (s === 'all') return this.preferences();
    return this.preferences().filter(p => p.status.toLowerCase() === s);
  }

  get pendingCount(): number {
    return this.preferences().filter(p => p.status === 'Pending').length;
  }

  loadAmenities(): void {
    this.api.apiGetAmenities().subscribe({
      next: list => this.amenities.set(list),
      error: () => {}
    });
  }

  loadPreferences(): void {
    this.loading.set(true);
    const obs = this.isAdmin
      ? this.api.apiGetAllAmenityPreferences()
      : this.api.apiGetMyAmenityPreferences(this.userId);

    obs.subscribe({
      next: list => { this.preferences.set(list); this.loading.set(false); },
      error: (e) => {
        this.toastr.error(e?.error?.message || 'Failed to load preferences.', 'Error');
        this.loading.set(false);
      }
    });
  }

  addPreference(): void {
    const amenityId = this.selectedId();
    if (!amenityId) { this.toastr.warning('Please select an amenity.'); return; }

    const already = this.preferences().some(p => p.amenityId === amenityId);
    if (already) { this.toastr.warning('You already have this amenity preference.'); return; }

    this.submitting.set(true);
    this.api.apiAddAmenityPreference(this.userId, amenityId).subscribe({
      next: (pref) => {
        this.preferences.update(list => [pref, ...list]);
        this.selectedId.set(null);
        this.toastr.success('Amenity preference added!');
        this.submitting.set(false);
      },
      error: (e) => {
        this.toastr.error(e?.error?.message || 'Failed to add preference.', 'Error');
        this.submitting.set(false);
      }
    });
  }

  remove(pref: UserAmenityPreferenceModel): void {
    this.api.apiRemoveAmenityPreference(pref.preferenceId).subscribe({
      next: () => {
        this.preferences.update(list => list.filter(p => p.preferenceId !== pref.preferenceId));
        this.toastr.info('Preference removed.');
      },
      error: (e) => this.toastr.error(e?.error?.message || 'Failed to remove.', 'Error')
    });
  }

  approve(pref: UserAmenityPreferenceModel): void {
    this.api.apiApproveAmenityPreference(pref.preferenceId).subscribe({
      next: (updated) => {
        this.preferences.update(list =>
          list.map(p => p.preferenceId === pref.preferenceId ? updated : p)
        );
        this.toastr.success(`Approved: ${pref.amenityName}`);
      },
      error: (e) => this.toastr.error(e?.error?.message || 'Failed to approve.', 'Error')
    });
  }

  reject(pref: UserAmenityPreferenceModel): void {
    this.api.apiRejectAmenityPreference(pref.preferenceId).subscribe({
      next: (updated) => {
        this.preferences.update(list =>
          list.map(p => p.preferenceId === pref.preferenceId ? updated : p)
        );
        this.toastr.warning(`Rejected: ${pref.amenityName}`);
      },
      error: (e) => this.toastr.error(e?.error?.message || 'Failed to reject.', 'Error')
    });
  }

  statusBadge(status: string): string {
    return status === 'Approved' ? 'badge-approved'
         : status === 'Rejected' ? 'badge-rejected'
         : 'badge-pending';
  }

  statusIcon(status: string): string {
    return status === 'Approved' ? 'bi-check-circle-fill'
         : status === 'Rejected' ? 'bi-x-circle-fill'
         : 'bi-hourglass-split';
  }

  availableAmenities(): AmenityModel[] {
    const taken = new Set(this.preferences().map(p => p.amenityId));
    return this.amenities().filter(a => !taken.has(a.amenityId));
  }
}
