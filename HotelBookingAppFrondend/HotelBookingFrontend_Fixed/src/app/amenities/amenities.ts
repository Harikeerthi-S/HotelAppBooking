import { Component, inject, signal, computed, OnDestroy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of, Subscription } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { AmenityModel } from '../models/amenity.model';
import { HotelModel } from '../models/hotel.model';
import { HotelAmenityModel } from '../models/hotel-amenity.model';
import { $userStatus, UserState } from '../dynamicCommunication/userObservable';

// Enrich HotelAmenity with names for display
interface AssignmentItem extends HotelAmenityModel {
  hotelName:   string;
  amenityName: string;
  amenityIcon: string;
}

@Component({
  selector: 'app-amenities',
  standalone: true,
  imports: [RouterLink, CommonModule, FormsModule],
  templateUrl: './amenities.html',
  styleUrl: './amenities.css'
})
export class Amenities implements OnDestroy {
  private api   = inject(APIService);
  private toast = inject(ToastrService);

  // ── Data signals ───────────────────────────────────────────────────────────
  amenities   = signal<AmenityModel[]>([]);
  hotels      = signal<HotelModel[]>([]);
  assignments = signal<AssignmentItem[]>([]);
  loading     = signal(true);

  // ── Active tab ─────────────────────────────────────────────────────────────
  // 'browse' — public view  |  'manage' — admin CRUD  |  'assign' — admin hotel mapping
  activeTab = signal<'browse' | 'manage' | 'assign'>('browse');

  // ── Create / Edit amenity form ─────────────────────────────────────────────
  editMode     = signal(false);   // false = create, true = edit
  editId       = signal(0);
  formName     = signal('');
  formDesc     = signal('');
  formIcon     = signal('');
  submitting   = signal(false);
  showAmenForm = signal(false);

  // ── Delete ─────────────────────────────────────────────────────────────────
  deletingId = signal(0);

  // ── Assign amenity to hotel ────────────────────────────────────────────────
  assignHotelId   = signal(0);
  assignAmenityId = signal(0);
  assigning       = signal(false);
  removingId      = signal(0);   // hotelAmenityId being removed

  // ── Search / filter ────────────────────────────────────────────────────────
  searchTerm = signal('');

  // ── User ──────────────────────────────────────────────────────────────────
  private currentUser = signal<UserState>({ userId: 0, userName: '', email: '', role: '' });
  private sub: Subscription;

  get isAdmin()    { return this.currentUser().role === 'admin'; }
  get isLoggedIn() { return this.currentUser().userId > 0; }

  // ── Computed ───────────────────────────────────────────────────────────────
  filteredAmenities = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    return term
      ? this.amenities().filter(a =>
          a.name.toLowerCase().includes(term) ||
          (a.description ?? '').toLowerCase().includes(term)
        )
      : this.amenities();
  });

  constructor() {
    // Load amenities immediately (public — [AllowAnonymous])
    this.loadAmenities();

    this.sub = $userStatus.subscribe(u => {
      this.currentUser.set(u);
      // Admin: also load hotels + assignments for the Assign tab
      if (u.role === 'admin') {
        this.loadHotels();
        this.loadAssignments();
      }
    });
  }

  ngOnDestroy(): void { this.sub.unsubscribe(); }

  // ── Load amenities  GET /api/amenities  [AllowAnonymous] ──────────────────
  loadAmenities(): void {
    this.loading.set(true);
    this.api.apiGetAmenities().subscribe({
      next: list => { this.amenities.set(list || []); this.loading.set(false); },
      error: ()   => this.loading.set(false)
    });
  }

  // ── Load hotels for assignment dropdown (admin) ────────────────────────────
  loadHotels(): void {
    this.api.apiGetHotelsPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: res => this.hotels.set(res.data || []),
      error: ()  => {}
    });
  }

  // ── Load all hotel-amenity assignments (admin/hotelmanager) ───────────────
  loadAssignments(): void {
    this.api.apiGetAllHotelAmenities().subscribe({
      next: list => {
        if (!list.length) { this.assignments.set([]); return; }
        // Enrich each entry with hotel name + amenity name
        const reqs = list.map(ha =>
          forkJoin({
            hotel:   this.api.apiGetHotelById(ha.hotelId).pipe(catchError(() => of(null))),
            amenity: of(this.amenities().find(a => a.amenityId === ha.amenityId) ?? null)
          }).pipe(
            map(({ hotel, amenity }) => ({
              ...ha,
              hotelName:   hotel?.hotelName   ?? `Hotel #${ha.hotelId}`,
              amenityName: amenity?.name       ?? `Amenity #${ha.amenityId}`,
              amenityIcon: amenity?.icon       ?? '✨'
            } as AssignmentItem))
          )
        );
        forkJoin(reqs).subscribe({
          next: enriched => this.assignments.set(enriched),
          error: ()       => {}
        });
      },
      error: () => {}
    });
  }

  // ── Open create form ───────────────────────────────────────────────────────
  openCreate(): void {
    this.editMode.set(false);
    this.editId.set(0);
    this.formName.set(''); this.formDesc.set(''); this.formIcon.set('');
    this.showAmenForm.set(true);
  }

  // ── Open edit form ─────────────────────────────────────────────────────────
  openEdit(a: AmenityModel): void {
    this.editMode.set(true);
    this.editId.set(a.amenityId);
    this.formName.set(a.name);
    this.formDesc.set(a.description ?? '');
    this.formIcon.set(a.icon ?? '');
    this.showAmenForm.set(true);
  }

  closeAmenForm(): void { this.showAmenForm.set(false); }

  // ── Submit create or edit ──────────────────────────────────────────────────
  submitAmenity(): void {
    if (!this.formName().trim()) { this.toast.warning('Name is required.'); return; }

    this.submitting.set(true);

    if (this.editMode()) {
      // PUT /api/amenities/{id}  body: CreateAmenityDto  [Authorize(Roles="admin")]
      // NOTE: api.service has no apiUpdateAmenity — we use HttpClient directly
      // The method is added to api.service in the updated version delivered with this fix
      this.api.apiUpdateAmenity(this.editId(), this.formName().trim(), this.formDesc().trim(), this.formIcon().trim()).subscribe({
        next: () => {
          this.amenities.update(list => list.map(a =>
            a.amenityId === this.editId()
              ? { ...a, name: this.formName().trim(), description: this.formDesc().trim(), icon: this.formIcon().trim() }
              : a
          ));
          this.submitting.set(false);
          this.showAmenForm.set(false);
          this.toast.success('Amenity updated.', 'Updated');
        },
        error: e => {
          this.submitting.set(false);
          this.toast.error(e?.error?.message || 'Failed to update.', 'Error');
        }
      });
    } else {
      // POST /api/amenities  body: CreateAmenityDto  [Authorize(Roles="admin")]
      this.api.apiCreateAmenity(this.formName().trim(), this.formDesc().trim(), this.formIcon().trim()).subscribe({
        next: created => {
          this.amenities.update(list => [...list, created]);
          this.submitting.set(false);
          this.showAmenForm.set(false);
          this.toast.success(`"${created.name}" added.`, 'Amenity Created');
        },
        error: e => {
          this.submitting.set(false);
          if (e.status === 409) this.toast.warning('An amenity with this name already exists.', 'Duplicate');
          else this.toast.error(e?.error?.message || 'Failed to create.', 'Error');
        }
      });
    }
  }

  // ── Delete amenity ─────────────────────────────────────────────────────────
  deleteAmenity(a: AmenityModel): void {
    if (!confirm(`Delete amenity "${a.name}"? This will also remove all hotel assignments.`)) return;
    this.deletingId.set(a.amenityId);
    // DELETE /api/amenities/{id}  [Authorize(Roles="admin")]
    this.api.apiDeleteAmenity(a.amenityId).subscribe({
      next: () => {
        this.amenities.update(list => list.filter(x => x.amenityId !== a.amenityId));
        this.assignments.update(list => list.filter(x => x.amenityId !== a.amenityId));
        this.deletingId.set(0);
        this.toast.info(`"${a.name}" deleted.`);
      },
      error: e => {
        this.deletingId.set(0);
        this.toast.error(e?.error?.message || 'Failed to delete.', 'Error');
      }
    });
  }

  // ── Assign amenity to hotel ────────────────────────────────────────────────
  assignToHotel(): void {
    if (!this.assignHotelId())   { this.toast.warning('Please select a hotel.'); return; }
    if (!this.assignAmenityId()) { this.toast.warning('Please select an amenity.'); return; }

    // Check if already assigned
    const alreadyExists = this.assignments().some(
      a => a.hotelId === this.assignHotelId() && a.amenityId === this.assignAmenityId()
    );
    if (alreadyExists) { this.toast.warning('This amenity is already assigned to the selected hotel.'); return; }

    this.assigning.set(true);
    // POST /api/hotelamenity  body: CreateHotelAmenityDto  [Authorize(Roles="admin")]
    this.api.apiCreateHotelAmenity({ hotelId: this.assignHotelId(), amenityId: this.assignAmenityId() }).subscribe({
      next: ha => {
        const hotel   = this.hotels().find(h => h.hotelId === ha.hotelId);
        const amenity = this.amenities().find(a => a.amenityId === ha.amenityId);
        const item: AssignmentItem = {
          ...ha,
          hotelName:   hotel?.hotelName   ?? `Hotel #${ha.hotelId}`,
          amenityName: amenity?.name       ?? `Amenity #${ha.amenityId}`,
          amenityIcon: amenity?.icon       ?? '✨'
        };
        this.assignments.update(list => [...list, item]);
        this.assignHotelId.set(0);
        this.assignAmenityId.set(0);
        this.assigning.set(false);
        this.toast.success(`"${item.amenityName}" assigned to ${item.hotelName}.`, 'Assigned');
      },
      error: e => {
        this.assigning.set(false);
        if (e.status === 409) this.toast.warning('Already assigned.', 'Duplicate');
        else this.toast.error(e?.error?.message || 'Failed to assign.', 'Error');
      }
    });
  }

  // ── Remove hotel-amenity assignment ───────────────────────────────────────
  removeAssignment(item: AssignmentItem): void {
    if (!confirm(`Remove "${item.amenityName}" from ${item.hotelName}?`)) return;
    this.removingId.set(item.hotelAmenityId);
    // DELETE /api/hotelamenity/{id}  [Authorize(Roles="admin")]
    this.api.apiDeleteHotelAmenity(item.hotelAmenityId).subscribe({
      next: () => {
        this.assignments.update(list => list.filter(x => x.hotelAmenityId !== item.hotelAmenityId));
        this.removingId.set(0);
        this.toast.info(`"${item.amenityName}" removed from ${item.hotelName}.`);
      },
      error: e => {
        this.removingId.set(0);
        this.toast.error(e?.error?.message || 'Failed to remove.', 'Error');
      }
    });
  }

  // ── Helper: assignments grouped by hotel for display ──────────────────────
  assignmentsByHotel(): { hotelId: number; hotelName: string; items: AssignmentItem[] }[] {
    const map = new Map<number, AssignmentItem[]>();
    this.assignments().forEach(a => {
      if (!map.has(a.hotelId)) map.set(a.hotelId, []);
      map.get(a.hotelId)!.push(a);
    });
    return Array.from(map.entries()).map(([hotelId, items]) => ({
      hotelId,
      hotelName: items[0].hotelName,
      items
    }));
  }

  // ── Icon preview helper ────────────────────────────────────────────────────
  iconPreview(icon: string): string { return icon?.trim() || '✨'; }
}
