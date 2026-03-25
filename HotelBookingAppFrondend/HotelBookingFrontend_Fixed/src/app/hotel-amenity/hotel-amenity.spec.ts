import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HotelAmenity } from './hotel-amenity';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';
import { AmenityModel } from '../models/amenity.model';
import { HotelAmenityModel } from '../models/hotel-amenity.model';
import { HotelModel } from '../models/hotel.model';

const fakeHotelAmenities: HotelAmenityModel[] = [
  { hotelAmenityId: 1, hotelId: 1, amenityId: 1 },
  { hotelAmenityId: 2, hotelId: 1, amenityId: 2 },
  { hotelAmenityId: 3, hotelId: 2, amenityId: 1 }
];

const fakeAmenities: AmenityModel[] = [
  { amenityId: 1, name: 'WiFi',        description: 'Free WiFi', icon: '📶' },
  { amenityId: 2, name: 'Swimming Pool', description: 'Outdoor pool', icon: '🏊' },
  { amenityId: 3, name: 'Gym',         description: 'Fitness center', icon: '💪' }
];

const fakeHotels: HotelModel[] = [
  { hotelId: 1, hotelName: 'Grand Palace', imagePath: '', location: 'Mumbai', address: 'Addr', totalRooms: 50, starRating: 5, contactNumber: '9999' },
  { hotelId: 2, hotelName: 'Sea View', imagePath: '', location: 'Goa', address: 'Beach Rd', totalRooms: 30, starRating: 4, contactNumber: '8888' }
];

describe('HotelAmenity', () => {
  let component: HotelAmenity;
  let fixture: ComponentFixture<HotelAmenity>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HotelAmenity],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(HotelAmenity);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    const haReq = httpMock.expectOne(r => r.url.includes('hotelamenity') && r.method === 'GET' && !r.url.includes('/'));
    haReq.flush(fakeHotelAmenities);

    const amenityReq = httpMock.expectOne(r => r.url.includes('amenities'));
    amenityReq.flush(fakeAmenities);

    const hotelsReq = httpMock.expectOne(r => r.url.includes('hotel/paged'));
    hotelsReq.flush({ data: fakeHotels, totalRecords: 2, totalPages: 1, pageNumber: 1, pageSize: 100 });

    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load hotel amenities on init', () => {
    expect(component.hotelAmenities().length).toBe(3);
  });

  it('should load all amenities', () => {
    expect(component.allAmenities().length).toBe(3);
  });

  it('should load all hotels', () => {
    expect(component.allHotels().length).toBe(2);
  });

  it('enrichedList should have same length as hotelAmenities', () => {
    component.buildEnriched();
    expect(component.enrichedList().length).toBe(3);
  });

  it('enrichedList should resolve hotel names', () => {
    component.buildEnriched();
    const first = component.enrichedList()[0];
    expect(first.hotelName).toBe('Grand Palace');
  });

  it('enrichedList should resolve amenity names', () => {
    component.buildEnriched();
    const first = component.enrichedList()[0];
    expect(first.amenityName).toBe('WiFi');
  });

  it('getUniqueHotelCount should count distinct hotels', () => {
    expect(component.getUniqueHotelCount()).toBe(2);
  });

  it('getHotelAmenities should filter by hotelId', () => {
    expect(component.getHotelAmenities(1).length).toBe(2);
    expect(component.getHotelAmenities(2).length).toBe(1);
  });

  it('getUnassignedAmenities should exclude already assigned amenities', () => {
    // Hotel 1 has amenity 1 and 2, so only amenity 3 is unassigned
    const unassigned = component.getUnassignedAmenities(1);
    expect(unassigned.length).toBe(1);
    expect(unassigned[0].amenityId).toBe(3);
  });

  it('getAmenityIcon should return icon for known amenity', () => {
    expect(component.getAmenityIcon(1)).toBe('📶');
    expect(component.getAmenityIcon(2)).toBe('🏊');
  });

  it('getAmenityIcon should return default for unknown amenity', () => {
    expect(component.getAmenityIcon(999)).toBe('⭐');
  });

  it('selectedHotelId defaults to 0', () => {
    expect(component.selectedHotelId()).toBe(0);
  });

  it('selectedAmenityId defaults to 0', () => {
    expect(component.selectedAmenityId()).toBe(0);
  });

  it('loading should be false after data loaded', () => {
    expect(component.loading()).toBeFalse();
  });

  it('saving defaults to false', () => {
    expect(component.saving()).toBeFalse();
  });

  it('assign should not call API when hotel not selected', () => {
    component.selectedHotelId.set(0);
    component.selectedAmenityId.set(1);
    component.assign();
    httpMock.expectNone(r => r.url.includes('hotelamenity') && r.method === 'POST');
  });

  it('assign should not call API when amenity not selected', () => {
    component.selectedHotelId.set(1);
    component.selectedAmenityId.set(0);
    component.assign();
    httpMock.expectNone(r => r.url.includes('hotelamenity') && r.method === 'POST');
  });

  it('should render assignments table', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('table')).toBeTruthy();
  });
});
