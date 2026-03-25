import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Booking } from './booking';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';
import { HotelModel } from '../models/hotel.model';
import { RoomModel } from '../models/room.model';

const fakeHotel: HotelModel = {
  hotelId: 1, hotelName: 'Grand Palace', imagePath: '',
  location: 'Mumbai', address: 'Marine Drive', totalRooms: 50,
  starRating: 5, contactNumber: '9999999999'
};
const fakeRoom: RoomModel = {
  roomId: 10, hotelId: 1, roomNumber: 101,
  roomType: 'Deluxe', pricePerNight: 2500, capacity: 2, isAvailable: true
};

describe('Booking', () => {
  let component: Booking;
  let fixture: ComponentFixture<Booking>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.setItem('hotel_user', JSON.stringify({ userId: 5, userName: 'Test', email: 't@t.com', role: 'user' }));

    await TestBed.configureTestingModule({
      imports: [Booking],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              params: { hotelId: '1', roomId: '10' },
              queryParams: { checkIn: '2025-12-01', checkOut: '2025-12-03', rooms: '1' }
            }
          }
        }
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(Booking);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    const hotelReq = httpMock.expectOne(r => r.url.includes('hotel/1'));
    hotelReq.flush(fakeHotel);
    const roomReq = httpMock.expectOne(r => r.url.includes('room/10'));
    roomReq.flush(fakeRoom);

    await fixture.whenStable();
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.removeItem('hotel_user');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load hotel from query params', () => {
    expect(component.hotel()?.hotelId).toBe(1);
  });

  it('should load room from query params', () => {
    expect(component.room()?.roomId).toBe(10);
  });

  it('checkIn should be pre-filled from queryParams', () => {
    expect(component.checkIn()).toBe('2025-12-01');
  });

  it('checkOut should be pre-filled from queryParams', () => {
    expect(component.checkOut()).toBe('2025-12-03');
  });

  it('numRooms defaults to 1', () => {
    expect(component.numRooms()).toBe(1);
  });

  it('nights should calculate correctly', () => {
    expect(component.nights).toBe(2);
  });

  it('totalAmount should be pricePerNight × nights × rooms', () => {
    expect(component.totalAmount).toBe(2500 * 2 * 1);
  });

  it('totalAmount with multiple rooms', () => {
    component.numRooms.set(3);
    expect(component.totalAmount).toBe(2500 * 2 * 3);
  });

  it('nights should return 0 when no dates', () => {
    component.checkIn.set('');
    component.checkOut.set('');
    expect(component.nights).toBe(0);
  });

  it('loading starts as false', () => {
    expect(component.loading()).toBeFalse();
  });

  it('should render booking card', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.card')).toBeTruthy();
  });
});
