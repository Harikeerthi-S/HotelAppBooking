import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { UserAmenityPreference } from './user-amenity-preference';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { of, throwError } from 'rxjs';
import { UserAmenityPreferenceModel } from '../models/user-amenity-preference.model';
import { AmenityModel } from '../models/amenity.model';
import { provideHttpClient } from '@angular/common/http';

const mockPref = (overrides: Partial<UserAmenityPreferenceModel> = {}): UserAmenityPreferenceModel =>
  Object.assign(new UserAmenityPreferenceModel(), {
    preferenceId: 1, userId: 1, userName: 'Alice',
    amenityId: 1, amenityName: 'Pool', amenityIcon: '🏊',
    createdAt: new Date().toISOString(), status: 'Pending',
    ...overrides
  });

const mockAmenity = (id = 1): AmenityModel =>
  ({ amenityId: id, name: `Amenity${id}`, icon: '✨', description: '' });

describe('UserAmenityPreference', () => {
  let component: UserAmenityPreference;
  let fixture: ComponentFixture<UserAmenityPreference>;
  let apiSpy: jasmine.SpyObj<APIService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('APIService', [
      'apiGetAmenities', 'apiGetMyAmenityPreferences', 'apiGetAllAmenityPreferences',
      'apiAddAmenityPreference', 'apiRemoveAmenityPreference',
      'apiApproveAmenityPreference', 'apiRejectAmenityPreference'
    ]);
    toastrSpy = jasmine.createSpyObj('ToastrService', ['success', 'error', 'warning', 'info']);

    apiSpy.apiGetAmenities.and.returnValue(of([mockAmenity(1), mockAmenity(2)]));
    apiSpy.apiGetMyAmenityPreferences.and.returnValue(of([mockPref()]));

    localStorage.setItem('userId', '1');
    localStorage.setItem('role', 'user');

    await TestBed.configureTestingModule({
      imports: [UserAmenityPreference],
      providers: [
        provideHttpClient(),
        { provide: APIService, useValue: apiSpy },
        { provide: ToastrService, useValue: toastrSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UserAmenityPreference);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => { localStorage.clear(); });

  it('should create', () => expect(component).toBeTruthy());

  it('should load preferences on init', () => {
    expect(apiSpy.apiGetMyAmenityPreferences).toHaveBeenCalledWith(1);
    expect(component.preferences().length).toBe(1);
  });

  it('should load amenities on init', () => {
    expect(apiSpy.apiGetAmenities).toHaveBeenCalled();
    expect(component.amenities().length).toBe(2);
  });

  it('isAdmin should be false for user role', () => {
    expect(component.isAdmin).toBeFalse();
  });

  it('isAdmin should be true for admin role', () => {
    localStorage.setItem('role', 'admin');
    component.ngOnInit();
    expect(component.isAdmin).toBeTrue();
  });

  it('should filter by status', () => {
    component.preferences.set([
      mockPref({ status: 'Pending' }),
      mockPref({ preferenceId: 2, status: 'Approved' })
    ]);
    component.filterStatus.set('approved');
    expect(component.filtered.length).toBe(1);
    expect(component.filtered[0].status).toBe('Approved');
  });

  it('should return all when filter is all', () => {
    component.preferences.set([mockPref(), mockPref({ preferenceId: 2, status: 'Approved' })]);
    component.filterStatus.set('all');
    expect(component.filtered.length).toBe(2);
  });

  it('pendingCount should count pending preferences', () => {
    component.preferences.set([
      mockPref({ status: 'Pending' }),
      mockPref({ preferenceId: 2, status: 'Approved' })
    ]);
    expect(component.pendingCount).toBe(1);
  });

  it('should warn when no amenity selected on add', () => {
    component.selectedId.set(null);
    component.addPreference();
    expect(toastrSpy.warning).toHaveBeenCalled();
    expect(apiSpy.apiAddAmenityPreference).not.toHaveBeenCalled();
  });

  it('should warn on duplicate amenity', () => {
    component.preferences.set([mockPref({ amenityId: 1 })]);
    component.selectedId.set(1);
    component.addPreference();
    expect(toastrSpy.warning).toHaveBeenCalled();
  });

  it('should add preference successfully', fakeAsync(() => {
    const newPref = mockPref({ preferenceId: 5, amenityId: 2 });
    apiSpy.apiAddAmenityPreference.and.returnValue(of(newPref));
    component.preferences.set([]);
    component.selectedId.set(2);
    component.addPreference();
    tick();
    expect(component.preferences().length).toBe(1);
    expect(toastrSpy.success).toHaveBeenCalled();
    expect(component.selectedId()).toBeNull();
  }));

  it('should handle add preference error', fakeAsync(() => {
    apiSpy.apiAddAmenityPreference.and.returnValue(throwError(() => ({ error: { message: 'Fail' } })));
    component.selectedId.set(2);
    component.addPreference();
    tick();
    expect(toastrSpy.error).toHaveBeenCalled();
    expect(component.submitting()).toBeFalse();
  }));

  it('should remove preference', fakeAsync(() => {
    const pref = mockPref();
    apiSpy.apiRemoveAmenityPreference.and.returnValue(of({}));
    component.preferences.set([pref]);
    component.remove(pref);
    tick();
    expect(component.preferences().length).toBe(0);
    expect(toastrSpy.info).toHaveBeenCalled();
  }));

  it('should approve preference (admin)', fakeAsync(() => {
    const pref    = mockPref({ status: 'Pending' });
    const updated = mockPref({ status: 'Approved' });
    apiSpy.apiApproveAmenityPreference.and.returnValue(of(updated));
    component.preferences.set([pref]);
    component.approve(pref);
    tick();
    expect(component.preferences()[0].status).toBe('Approved');
    expect(toastrSpy.success).toHaveBeenCalled();
  }));

  it('should reject preference (admin)', fakeAsync(() => {
    const pref    = mockPref({ status: 'Pending' });
    const updated = mockPref({ status: 'Rejected' });
    apiSpy.apiRejectAmenityPreference.and.returnValue(of(updated));
    component.preferences.set([pref]);
    component.reject(pref);
    tick();
    expect(component.preferences()[0].status).toBe('Rejected');
    expect(toastrSpy.warning).toHaveBeenCalled();
  }));

  it('statusBadge should return correct class', () => {
    expect(component.statusBadge('Approved')).toBe('badge-approved');
    expect(component.statusBadge('Rejected')).toBe('badge-rejected');
    expect(component.statusBadge('Pending')).toBe('badge-pending');
  });

  it('statusIcon should return correct icon', () => {
    expect(component.statusIcon('Approved')).toContain('check');
    expect(component.statusIcon('Rejected')).toContain('x-circle');
    expect(component.statusIcon('Pending')).toContain('hourglass');
  });

  it('availableAmenities should exclude already added', () => {
    component.amenities.set([mockAmenity(1), mockAmenity(2)]);
    component.preferences.set([mockPref({ amenityId: 1 })]);
    const available = component.availableAmenities();
    expect(available.length).toBe(1);
    expect(available[0].amenityId).toBe(2);
  });

  it('should use apiGetAllAmenityPreferences for admin', fakeAsync(() => {
    localStorage.setItem('role', 'admin');
    apiSpy.apiGetAllAmenityPreferences.and.returnValue(of([mockPref(), mockPref({ preferenceId: 2 })]));
    component.ngOnInit();
    tick();
    expect(apiSpy.apiGetAllAmenityPreferences).toHaveBeenCalled();
    expect(component.preferences().length).toBe(2);
  }));

  it('should handle load error gracefully', fakeAsync(() => {
    apiSpy.apiGetMyAmenityPreferences.and.returnValue(throwError(() => ({ error: { message: 'Server error' } })));
    component.ngOnInit();
    tick();
    expect(toastrSpy.error).toHaveBeenCalled();
    expect(component.loading()).toBeFalse();
  }));
});
