import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { HotelModel } from '../models/hotel.model';
import { AmenityModel } from '../models/amenity.model';
import { HotelAmenityModel, CreateHotelAmenityModel } from '../models/hotel-amenity.model';
import { UserAmenityPreferenceModel } from '../models/user-amenity-preference.model';

@Component({
  selector: 'app-hotel-amenity',
  imports: [CommonModule, FormsModule],
  templateUrl: './hotel-amenity.html',
  styleUrl: './hotel-amenity.css'
})
export class HotelAmenity implements OnInit {
  private apiService = inject(APIService);
  private toastr     = inject(ToastrService);

  hotelAmenities = signal<HotelAmenityModel[]>([]);
  allAmenities   = signal<AmenityModel[]>([]);
  allHotels      = signal<HotelModel[]>([]);
  loading        = signal(false);
  saving         = signal(false);

  // Form state
  selectedHotelId   = signal(0);
  selectedAmenityId = signal(0);

  // Enriched view: join hotel and amenity names
  enrichedList = signal<{ ha: HotelAmenityModel; hotelName: string; amenityName: string }[]>([]);

  // User preferences (requested by users)
  userPreferences    = signal<UserAmenityPreferenceModel[]>([]);
  prefLoading        = signal(false);
  activeView         = signal<'assignments' | 'preferences'>('assignments');

  // Grouped preferences: amenityName → list of users who want it
  groupedPreferences = signal<{ amenityId: number; amenityName: string; icon: string; users: UserAmenityPreferenceModel[] }[]>([]);

  ngOnInit(): void {
    this.loadAll();
    this.loadUserPreferences();
  }

  loadAll(): void {
    this.loading.set(true);
    // Load hotel amenities
    this.apiService.apiGetAllHotelAmenities().subscribe({
      next: list => {
        this.hotelAmenities.set(list);
        this.buildEnriched();
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
    // Load amenities for dropdown
    this.apiService.apiGetAmenities().subscribe({
      next: a => { this.allAmenities.set(a); this.buildEnriched(); },
      error: () => {}
    });
    // Load hotels for dropdown
    this.apiService.apiGetHotelsPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: r => { this.allHotels.set(r.data); this.buildEnriched(); },
      error: () => {}
    });
  }

  buildEnriched(): void {
    const enriched = this.hotelAmenities().map(ha => ({
      ha,
      hotelName:   this.allHotels().find(h => h.hotelId === ha.hotelId)?.hotelName ?? `Hotel #${ha.hotelId}`,
      amenityName: this.allAmenities().find(a => a.amenityId === ha.amenityId)?.name ?? `Amenity #${ha.amenityId}`
    }));
    this.enrichedList.set(enriched);
  }

  assign(): void {
    if (!this.selectedHotelId() || !this.selectedAmenityId()) {
      this.toastr.warning('Please select both a hotel and an amenity.');
      return;
    }
    // Check duplicate
    const alreadyExists = this.hotelAmenities().some(
      ha => ha.hotelId === this.selectedHotelId() && ha.amenityId === this.selectedAmenityId()
    );
    if (alreadyExists) {
      this.toastr.warning('This amenity is already assigned to that hotel.');
      return;
    }
    this.saving.set(true);
    const model = new CreateHotelAmenityModel();
    model.hotelId   = this.selectedHotelId();
    model.amenityId = this.selectedAmenityId();

    this.apiService.apiCreateHotelAmenity(model).subscribe({
      next: created => {
        this.hotelAmenities.update(list => [...list, created]);
        this.buildEnriched();
        this.selectedHotelId.set(0);
        this.selectedAmenityId.set(0);
        this.saving.set(false);
        this.toastr.success('Amenity assigned to hotel successfully!');
      },
      error: (e) => {
        this.saving.set(false);
        this.toastr.error(e?.error?.message || 'Error assigning amenity.');
      }
    });
  }

  remove(ha: HotelAmenityModel): void {
    const hotelName   = this.allHotels().find(h => h.hotelId === ha.hotelId)?.hotelName ?? 'hotel';
    const amenityName = this.allAmenities().find(a => a.amenityId === ha.amenityId)?.name ?? 'amenity';
    if (!confirm(`Remove "${amenityName}" from "${hotelName}"?`)) return;

    this.apiService.apiDeleteHotelAmenity(ha.hotelAmenityId).subscribe({
      next: () => {
        this.hotelAmenities.update(list => list.filter(x => x.hotelAmenityId !== ha.hotelAmenityId));
        this.buildEnriched();
        this.toastr.success('Hotel amenity removed.');
      },
      error: (e) => this.toastr.error(e?.error?.message || 'Error removing hotel amenity.')
    });
  }

  getAmenityIcon(amenityId: number): string {
    return this.allAmenities().find(a => a.amenityId === amenityId)?.icon || '⭐';
  }

  // Filter: amenities not yet assigned to a hotel
  getUnassignedAmenities(hotelId: number): AmenityModel[] {
    const assigned = this.hotelAmenities()
      .filter(ha => ha.hotelId === hotelId)
      .map(ha => ha.amenityId);
    return this.allAmenities().filter(a => !assigned.includes(a.amenityId));
  }

  getUniqueHotelCount(): number {
    return new Set(this.hotelAmenities().map(ha => ha.hotelId)).size;
  }

  getHotelAmenities(hotelId: number): HotelAmenityModel[] {
    return this.hotelAmenities().filter(ha => ha.hotelId === hotelId);
  }

  // ── User Preferences ──────────────────────────────────────────────────────
  loadUserPreferences(): void {
    this.prefLoading.set(true);
    this.apiService.apiGetAllAmenityPreferences().subscribe({
      next: prefs => {
        this.userPreferences.set(prefs ?? []);
        this.buildGroupedPreferences(prefs ?? []);
        this.prefLoading.set(false);
      },
      error: () => this.prefLoading.set(false)
    });
  }

  buildGroupedPreferences(prefs: UserAmenityPreferenceModel[]): void {
    const map = new Map<number, UserAmenityPreferenceModel[]>();
    prefs.forEach(p => {
      if (!map.has(p.amenityId)) map.set(p.amenityId, []);
      map.get(p.amenityId)!.push(p);
    });
    const grouped = Array.from(map.entries()).map(([amenityId, users]) => ({
      amenityId,
      amenityName: users[0].amenityName,
      icon: users[0].amenityIcon ?? '✨',
      users
    })).sort((a, b) => b.users.length - a.users.length); // most requested first
    this.groupedPreferences.set(grouped);
  }

  // Assign amenity to a hotel directly from the preferences panel
  assignFromPreference(amenityId: number): void {
    this.selectedAmenityId.set(amenityId);
    this.activeView.set('assignments');
    this.toastr.info('Amenity pre-selected. Choose a hotel and click Assign.');
  }

  totalPreferenceRequests(): number {
    return this.userPreferences().length;
  }

  uniqueUsersCount(): number {
    return new Set(this.userPreferences().map(p => p.userId)).size;
  }

  userPrefStatusClass(s: string | undefined): string {
    const x = (s || 'Pending').toLowerCase();
    if (x === 'approved') return 'bg-success';
    if (x === 'rejected') return 'bg-danger';
    return 'bg-warning text-dark';
  }

  approveUserPref(p: UserAmenityPreferenceModel): void {
    this.apiService.apiApproveAmenityPreference(p.preferenceId).subscribe({
      next: updated => {
        this.userPreferences.update(arr => arr.map(x => x.preferenceId === p.preferenceId ? { ...x, ...updated } : x));
        this.buildGroupedPreferences(this.userPreferences());
        this.toastr.success(`Approved: ${p.userName} → ${p.amenityName}`);
      }
    });
  }

  rejectUserPref(p: UserAmenityPreferenceModel): void {
    this.apiService.apiRejectAmenityPreference(p.preferenceId).subscribe({
      next: updated => {
        this.userPreferences.update(arr => arr.map(x => x.preferenceId === p.preferenceId ? { ...x, ...updated } : x));
        this.buildGroupedPreferences(this.userPreferences());
        this.toastr.success(`Rejected: ${p.userName} → ${p.amenityName}`);
      }
    });
  }
}
