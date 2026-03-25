import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Home } from './home';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';

describe('Home', () => {
  let component: Home;
  let fixture: ComponentFixture<Home>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Home],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(Home);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    // Flush the automatic hotels request made in constructor
    const req = httpMock.expectOne(r => r.url.includes('hotel/paged'));
    req.flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 6 });

    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have 6 popular cities', () => {
    expect(component.cities.length).toBe(6);
  });

  it('should include Goa in cities', () => {
    const cityNames = component.cities.map(c => c.name);
    expect(cityNames).toContain('Goa');
  });

  it('should have 4 feature cards', () => {
    expect(component.features.length).toBe(4);
  });

  it('should have 4 stat items', () => {
    expect(component.stats.length).toBe(4);
  });

  it('loading should be false after data fetch', () => {
    expect(component.loading()).toBeFalse();
  });

  it('hotels signal should be empty when API returns no data', () => {
    expect(component.hotels().length).toBe(0);
  });

  it('getStartingPrice should return higher price for higher star rating', () => {
    const low  = { hotelId: 1, starRating: 2 } as any;
    const high = { hotelId: 2, starRating: 5 } as any;
    const pLow  = parseInt(component.getStartingPrice(low).replace(/,/g, ''));
    const pHigh = parseInt(component.getStartingPrice(high).replace(/,/g, ''));
    expect(pHigh).toBeGreaterThan(pLow);
  });

  it('getHotelImage should return imagePath when it starts with http', () => {
    const hotel = { hotelId: 1, imagePath: 'https://example.com/img.jpg', starRating: 4 } as any;
    expect(component.getHotelImage(hotel)).toBe('https://example.com/img.jpg');
  });

  it('getHotelImage should return unsplash URL when imagePath is not http', () => {
    const hotel = { hotelId: 1, imagePath: '', starRating: 4 } as any;
    expect(component.getHotelImage(hotel)).toContain('unsplash.com');
  });

  it('getStars should return array of correct length', () => {
    expect(component.getStars(4).length).toBe(4);
    expect(component.getStars(5).length).toBe(5);
  });

  it('today should be set to current date', () => {
    const today = new Date().toISOString().split('T')[0];
    expect(component.today).toBe(today);
  });

  it('getFeaturedHotels should populate hotels signal on success', () => {
    component.getFeaturedHotels();
    const req = httpMock.expectOne(r => r.url.includes('hotel/paged'));
    const fakeHotel = { hotelId: 1, hotelName: 'Test Hotel', starRating: 4, location: 'Mumbai', imagePath: '' };
    req.flush({ data: [fakeHotel], totalRecords: 1, totalPages: 1, pageNumber: 1, pageSize: 6 });
    expect(component.hotels().length).toBe(1);
    expect(component.hotels()[0].hotelName).toBe('Test Hotel');
  });

  it('should render hero section', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.hero-section')).toBeTruthy();
  });
});
