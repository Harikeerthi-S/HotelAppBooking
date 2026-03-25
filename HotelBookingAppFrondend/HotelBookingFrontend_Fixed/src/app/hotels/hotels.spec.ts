import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Hotels } from './hotels';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { of } from 'rxjs';
import { routes } from '../app.routes';
import { HotelModel } from '../models/hotel.model';

const fakeHotel: HotelModel = {
  hotelId: 1, hotelName: 'Grand Palace', imagePath: '',
  location: 'Mumbai', address: 'Marine Drive', totalRooms: 100,
  starRating: 4, contactNumber: '9876543210'
};

describe('Hotels', () => {
  let component: Hotels;
  let fixture: ComponentFixture<Hotels>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Hotels],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr(),
        {
          provide: ActivatedRoute,
          useValue: { queryParams: of({}) }
        }
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(Hotels);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    // flush constructor requests
    const pagedReq = httpMock.expectOne(r => r.url.includes('hotel/paged'));
    pagedReq.flush({ data: [fakeHotel], totalRecords: 1, totalPages: 1, pageNumber: 1, pageSize: 9 });
    const amenityReq = httpMock.expectOne(r => r.url.includes('amenities'));
    amenityReq.flush([]);

    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load hotels on init', () => {
    expect(component.hotels().length).toBe(1);
    expect(component.hotels()[0].hotelName).toBe('Grand Palace');
  });

  it('loading should be false after data loaded', () => {
    expect(component.loading()).toBeFalse();
  });

  it('paged response should have totalRecords', () => {
    expect(component.paged()?.totalRecords).toBe(1);
  });

  it('should default to page 1', () => {
    expect(component.page()).toBe(1);
  });

  it('should default sort to rating', () => {
    expect(component.sortBy()).toBe('rating');
  });

  it('sortHotels by name sorts alphabetically', () => {
    const hotelB: HotelModel = { ...fakeHotel, hotelId: 2, hotelName: 'Asha Inn', starRating: 3 };
    const sorted = component.sortHotels([fakeHotel, hotelB]);
    expect(sorted[0].hotelName).toBe('Asha Inn');
  });

  it('sortHotels by rating sorts descending', () => {
    component.sortBy.set('rating');
    const lowRated: HotelModel = { ...fakeHotel, hotelId: 2, hotelName: 'Budget Inn', starRating: 2 };
    const sorted = component.sortHotels([lowRated, fakeHotel]);
    expect(sorted[0].starRating).toBeGreaterThanOrEqual(sorted[1].starRating);
  });

  it('isWishlisted should return false for new hotel', () => {
    expect(component.isWishlisted(999)).toBeFalse();
  });

  it('getImage returns imagePath when it starts with http', () => {
    const h: HotelModel = { ...fakeHotel, imagePath: 'https://example.com/img.jpg' };
    expect(component.getImage(h)).toBe('https://example.com/img.jpg');
  });

  it('getImage returns unsplash URL for empty imagePath', () => {
    expect(component.getImage(fakeHotel)).toContain('unsplash.com');
  });

  it('getPrice returns string with rupee amounts', () => {
    const price = component.getPrice(fakeHotel);
    expect(parseInt(price.replace(/,/g, ''))).toBeGreaterThan(0);
  });

  it('getPages returns correct number of page buttons', () => {
    expect(component.getPages().length).toBe(1);
  });

  it('clearFilter resets filter fields', () => {
    component.fLocation.set('Mumbai');
    component.fMinRating.set('4');
    component.clearFilter();
    expect(component.fLocation()).toBe('');
    expect(component.fMinRating()).toBe('');
  });

  it('should render hotel cards', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.hotel-card')).toBeTruthy();
  });
});
