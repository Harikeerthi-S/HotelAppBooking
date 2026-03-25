import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Amenities } from './amenities';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';

describe('Amenities', () => {
  let component: Amenities;
  let fixture:   ComponentFixture<Amenities>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Amenities],
      providers: [provideHttpClient(), provideRouter([]), provideAnimations(), provideToastr()]
    }).compileComponents();
    fixture   = TestBed.createComponent(Amenities);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('default tab is browse', () => expect(component.activeTab()).toBe('browse'));

  it('searchTerm filters amenities', () => {
    (component as any).amenities.set([
      { amenityId: 1, name: 'Free Wi-Fi',    description: 'Wireless internet', icon: '📶' },
      { amenityId: 2, name: 'Swimming Pool', description: 'Outdoor pool',      icon: '🏊' },
      { amenityId: 3, name: 'Gym',           description: 'Fitness centre',     icon: '🏋️' },
    ]);
    component.searchTerm.set('pool');
    expect(component.filteredAmenities().length).toBe(1);
    expect(component.filteredAmenities()[0].name).toBe('Swimming Pool');
  });

  it('empty searchTerm shows all amenities', () => {
    (component as any).amenities.set([
      { amenityId: 1, name: 'Wi-Fi',  description: '', icon: '📶' },
      { amenityId: 2, name: 'Pool',   description: '', icon: '🏊' },
    ]);
    component.searchTerm.set('');
    expect(component.filteredAmenities().length).toBe(2);
  });

  it('openCreate resets form fields and shows form', () => {
    component.formName.set('Old'); component.formDesc.set('Old'); component.formIcon.set('Old');
    component.openCreate();
    expect(component.showAmenForm()).toBeTrue();
    expect(component.editMode()).toBeFalse();
    expect(component.formName()).toBe('');
    expect(component.formDesc()).toBe('');
    expect(component.formIcon()).toBe('');
  });

  it('openEdit populates form with amenity values', () => {
    const a = { amenityId: 5, name: 'Spa', description: 'Relaxing spa', icon: '💆' };
    component.openEdit(a);
    expect(component.showAmenForm()).toBeTrue();
    expect(component.editMode()).toBeTrue();
    expect(component.editId()).toBe(5);
    expect(component.formName()).toBe('Spa');
    expect(component.formDesc()).toBe('Relaxing spa');
    expect(component.formIcon()).toBe('💆');
  });

  it('closeAmenForm hides form', () => {
    component.openCreate();
    component.closeAmenForm();
    expect(component.showAmenForm()).toBeFalse();
  });

  it('iconPreview returns default emoji when blank', () => {
    expect(component.iconPreview('')).toBe('✨');
    expect(component.iconPreview('🏊')).toBe('🏊');
  });

  it('assignmentsByHotel groups correctly', () => {
    (component as any).assignments.set([
      { hotelAmenityId: 1, hotelId: 1, amenityId: 1, hotelName: 'H1', amenityName: 'Wi-Fi', amenityIcon: '📶' },
      { hotelAmenityId: 2, hotelId: 1, amenityId: 2, hotelName: 'H1', amenityName: 'Pool',  amenityIcon: '🏊' },
      { hotelAmenityId: 3, hotelId: 2, amenityId: 1, hotelName: 'H2', amenityName: 'Wi-Fi', amenityIcon: '📶' },
    ]);
    const groups = component.assignmentsByHotel();
    expect(groups.length).toBe(2);
    expect(groups.find(g => g.hotelId === 1)?.items.length).toBe(2);
    expect(groups.find(g => g.hotelId === 2)?.items.length).toBe(1);
  });
});
