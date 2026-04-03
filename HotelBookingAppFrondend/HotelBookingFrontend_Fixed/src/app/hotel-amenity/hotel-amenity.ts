import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { HotelModel } from '../models/hotel.model';
import { AmenityModel } from '../models/amenity.model';
import { HotelAmenityModel, CreateHotelAmenityModel } from '../models/hotel-amenity.model';

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

  selectedHotelId   = signal(0);
  selectedAmenityId = signal(0);

  enrichedList = signal<{ ha: HotelAmenityModel; hotelName: string; amenityName: string }[]>([]);

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loading.set(true);
    this.apiService.apiGetAllHotelAmenities().subscribe({
      next: list => { this.hotelAmenities.set(list); this.buildEnriched(); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
    this.apiService.apiGetAmenities().subscribe({
      next: a => { this.allAmenities.set(a); this.buildEnriched(); },
      error: () => {}
    });
    this.apiService.apiGetHotelsPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: r => { this.allHotels.set(r.data); this.buildEnriched(); },
      error: () => {}
    });
  }

  buildEnriched(): void {
    this.enrichedList.set(this.hotelAmenities().map(ha => ({
      ha,
      hotelName:   this.allHotels().find(h => h.hotelId === ha.hotelId)?.hotelName ?? `Hotel #${ha.hotelId}`,
      amenityName: this.allAmenities().find(a => a.amenityId === ha.amenityId)?.name ?? `Amenity #${ha.amenityId}`
    })));
  }

  assign(): void {
    if (!this.selectedHotelId() || !this.selectedAmenityId()) {
      this.toastr.warning('Please select both a hotel and an amenity.');
      return;
    }
    if (this.hotelAmenities().some(ha => ha.hotelId === this.selectedHotelId() && ha.amenityId === this.selectedAmenityId())) {
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
      error: (e) => { this.saving.set(false); this.toastr.error(e?.error?.message || 'Error assigning amenity.'); }
    });
  }

  remove(ha: HotelAmenityModel): void {
    const hotelName   = this.allHotels().find(h => h.hotelId === ha.hotelId)?.hotelName ?? 'hotel';
    const amenityName = this.allAmenities().find(a => a.amenityId === ha.amenityId)?.name ?? 'amenity';
    if (!confirm(`Remove "${amenityName}" from "${hotelName}"?`)) return;
    this.apiService.apiDeleteHotelAmenity(ha.hotelAmenityId).subscribe({
      next: () => { this.hotelAmenities.update(list => list.filter(x => x.hotelAmenityId !== ha.hotelAmenityId)); this.buildEnriched(); this.toastr.success('Hotel amenity removed.'); },
      error: (e) => this.toastr.error(e?.error?.message || 'Error removing hotel amenity.')
    });
  }

  getAmenityIcon(amenityId: number): string {
    return this.allAmenities().find(a => a.amenityId === amenityId)?.icon || '⭐';
  }

  getUnassignedAmenities(hotelId: number): AmenityModel[] {
    const assigned = this.hotelAmenities().filter(ha => ha.hotelId === hotelId).map(ha => ha.amenityId);
    return this.allAmenities().filter(a => !assigned.includes(a.amenityId));
  }

  getUniqueHotelCount(): number {
    return new Set(this.hotelAmenities().map(ha => ha.hotelId)).size;
  }

  getHotelAmenities(hotelId: number): HotelAmenityModel[] {
    return this.hotelAmenities().filter(ha => ha.hotelId === hotelId);
  }
}
