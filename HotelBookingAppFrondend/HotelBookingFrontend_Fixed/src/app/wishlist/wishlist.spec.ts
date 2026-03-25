import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Wishlist } from './wishlist';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';

describe('Wishlist', () => {
  let component: Wishlist;
  let fixture: ComponentFixture<Wishlist>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.setItem('hotel_user', JSON.stringify({ userId: 5, userName: 'Bob', email: 'b@b.com', role: 'user' }));

    await TestBed.configureTestingModule({
      imports: [Wishlist],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(Wishlist);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    const wishReq = httpMock.expectOne(r => r.url.includes('wishlist/user/5'));
    wishReq.flush([]);

    await fixture.whenStable();
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.removeItem('hotel_user');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load userId from sessionStorage', () => {
    expect(component.userId()).toBe(5);
  });

  it('items should be empty when wishlist is empty', () => {
    expect(component.items().length).toBe(0);
  });

  it('loading should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('getImage should return imagePath when it starts with http', () => {
    const hotel = { hotelId: 1, imagePath: 'https://example.com/img.jpg', starRating: 4 } as any;
    expect(component.getImage(hotel)).toBe('https://example.com/img.jpg');
  });

  it('getImage should return unsplash URL for empty imagePath', () => {
    const hotel = { hotelId: 1, imagePath: '', starRating: 3 } as any;
    expect(component.getImage(hotel)).toContain('unsplash.com');
  });

  it('getPrice should return higher price for 5-star hotel', () => {
    const low  = { starRating: 1 } as any;
    const high = { starRating: 5 } as any;
    const pLow  = parseInt(component.getPrice(low).replace(/,/g, ''));
    const pHigh = parseInt(component.getPrice(high).replace(/,/g, ''));
    expect(pHigh).toBeGreaterThan(pLow);
  });

  it('getPrice returns default for undefined hotel', () => {
    const price = parseInt(component.getPrice(undefined).replace(/,/g, ''));
    expect(price).toBeGreaterThan(0);
  });

  it('should render empty state when wishlist is empty', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.empty-state')).toBeTruthy();
  });
});
