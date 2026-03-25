import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HotelDetail } from './hotel-detail';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { of } from 'rxjs';
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
  roomType: 'Deluxe', pricePerNight: 3000,
  capacity: 2, isAvailable: true
};

describe('HotelDetail', () => {
  let component: HotelDetail;
  let fixture: ComponentFixture<HotelDetail>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HotelDetail],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { params: { id: '1' } } }
        }
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(HotelDetail);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    // flush hotel request
    const hotelReq = httpMock.expectOne(r => r.url.includes('hotel/1'));
    hotelReq.flush(fakeHotel);
    // flush rooms
    const roomsReq = httpMock.expectOne(r => r.url.includes('room') && r.params.has('hotelId'));
    roomsReq.flush([fakeRoom]);
    // flush reviews
    const reviewsReq = httpMock.expectOne(r => r.url.includes('review/paged'));
    reviewsReq.flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 10 });

    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load hotel data', () => {
    expect(component.hotel()?.hotelName).toBe('Grand Palace');
  });

  it('should load available rooms', () => {
    expect(component.rooms().length).toBe(1);
    expect(component.rooms()[0].roomType).toBe('Deluxe');
  });

  it('loading should be false after fetch', () => {
    expect(component.loading()).toBeFalse();
  });

  it('selectedRoom should default to null', () => {
    expect(component.selectedRoom()).toBeNull();
  });

  it('selectRoom should set selectedRoom', () => {
    component.selectRoom(fakeRoom);
    expect(component.selectedRoom()?.roomId).toBe(10);
  });

  it('selectRoom should not select unavailable room', () => {
    const unavail: RoomModel = { ...fakeRoom, isAvailable: false };
    component.selectRoom(unavail);
    expect(component.selectedRoom()).toBeNull();
  });

  it('nights should be 0 when dates not set', () => {
    expect(component.nights).toBe(0);
  });

  it('nights should calculate correctly', () => {
    component.checkIn.set('2025-12-01');
    component.checkOut.set('2025-12-05');
    expect(component.nights).toBe(4);
  });

  it('totalAmount should be 0 when no room selected', () => {
    expect(component.totalAmount).toBe(0);
  });

  it('totalAmount should compute price × nights × rooms', () => {
    component.selectRoom(fakeRoom);
    component.checkIn.set('2025-12-01');
    component.checkOut.set('2025-12-03');
    component.numRooms.set(2);
    expect(component.totalAmount).toBe(3000 * 2 * 2);
  });

  it('getImage should return unsplash URL for empty imagePath', () => {
    expect(component.getImage()).toContain('unsplash.com');
  });

  it('getStars should return correct length array', () => {
    expect(component.getStars(5).length).toBe(5);
    expect(component.getStars(3).length).toBe(3);
  });

  it('getRoomIcon should return emoji for known types', () => {
    expect(component.getRoomIcon('suite')).toBe('🏰');
    expect(component.getRoomIcon('deluxe')).toBe('💎');
    expect(component.getRoomIcon('single')).toBe('🛏️');
  });

  it('wishlisted should default to false', () => {
    expect(component.wishlisted()).toBeFalse();
  });
});
