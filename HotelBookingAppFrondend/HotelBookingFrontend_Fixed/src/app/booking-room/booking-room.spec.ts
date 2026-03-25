import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BookingRoom } from './booking-room';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';
import { BookingRoomModel } from '../models/booking-room.model';
import { BookingModel } from '../models/booking.model';
import { RoomModel } from '../models/room.model';

const fakeBooking: BookingModel = {
  bookingId: 10, userId: 5, hotelId: 1, hotelName: 'Grand Palace',
  roomId: 1, numberOfRooms: 2, checkIn: '2025-12-01', checkOut: '2025-12-03',
  totalAmount: 6000, status: 'Confirmed'
};

const fakeRooms: RoomModel[] = [
  { roomId: 1, hotelId: 1, roomNumber: 101, roomType: 'Deluxe', pricePerNight: 2000, capacity: 2, isAvailable: true },
  { roomId: 2, hotelId: 1, roomNumber: 102, roomType: 'Suite',  pricePerNight: 5000, capacity: 4, isAvailable: true },
  { roomId: 3, hotelId: 1, roomNumber: 103, roomType: 'Single', pricePerNight: 800,  capacity: 1, isAvailable: false }
];

const fakeBookingRooms: BookingRoomModel[] = [
  { bookingRoomId: 1, bookingId: 10, roomId: 1, pricePerNight: 2000, numberOfRooms: 1 },
  { bookingRoomId: 2, bookingId: 10, roomId: 2, pricePerNight: 5000, numberOfRooms: 1 }
];

describe('BookingRoom', () => {
  let component: BookingRoom;
  let fixture: ComponentFixture<BookingRoom>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.setItem('hotel_user', JSON.stringify({ userId: 5, userName: 'Test', email: 't@t.com', role: 'user' }));

    await TestBed.configureTestingModule({
      imports: [BookingRoom],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { params: { bookingId: '10' } } }
        }
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(BookingRoom);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    const brReq = httpMock.expectOne(r => r.url.includes('bookingroom/booking/10'));
    brReq.flush(fakeBookingRooms);

    const bookingReq = httpMock.expectOne(r => r.url.includes('/booking/10') && !r.url.includes('paged'));
    bookingReq.flush(fakeBooking);

    const roomsReq = httpMock.expectOne(r => r.url.endsWith('/room'));
    roomsReq.flush(fakeRooms);

    await fixture.whenStable();
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.removeItem('hotel_user');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should set bookingId from route params', () => {
    expect(component.bookingId()).toBe(10);
  });

  it('should load booking details', () => {
    expect(component.booking()?.hotelName).toBe('Grand Palace');
    expect(component.booking()?.status).toBe('Confirmed');
  });

  it('should load booking rooms', () => {
    expect(component.bookingRooms().length).toBe(2);
  });

  it('should only show available rooms in dropdown', () => {
    // room 3 is unavailable, should be filtered out
    expect(component.availableRooms().length).toBe(2);
    expect(component.availableRooms().every(r => r.isAvailable)).toBeTrue();
  });

  it('loading should be false after data loaded', () => {
    expect(component.loading()).toBeFalse();
  });

  it('saving defaults to false', () => {
    expect(component.saving()).toBeFalse();
  });

  it('editId defaults to null', () => {
    expect(component.editId()).toBeNull();
  });

  it('getTotalRooms sums numberOfRooms across all booking rooms', () => {
    expect(component.getTotalRooms()).toBe(2);
  });

  it('getTotalCost sums pricePerNight × numberOfRooms', () => {
    expect(component.getTotalCost()).toBe(2000 * 1 + 5000 * 1);
  });

  it('getStatusClass returns correct class for Confirmed', () => {
    expect(component.getStatusClass('Confirmed')).toBe('st-confirmed');
  });

  it('getStatusClass returns correct class for Pending', () => {
    expect(component.getStatusClass('Pending')).toBe('st-pending');
  });

  it('getStatusClass returns correct class for Cancelled', () => {
    expect(component.getStatusClass('Cancelled')).toBe('st-cancelled');
  });

  it('getRoomLabel returns formatted label for known room', () => {
    const label = component.getRoomLabel(1);
    expect(label).toContain('101');
    expect(label).toContain('Deluxe');
  });

  it('getRoomLabel returns fallback for unknown room', () => {
    expect(component.getRoomLabel(999)).toContain('#999');
  });

  it('onRoomSelect auto-fills pricePerNight from room', () => {
    component.onRoomSelect(1);
    expect(component.form().pricePerNight).toBe(2000);
    expect(component.form().roomId).toBe(1);
  });

  it('edit should populate form with booking room values', () => {
    component.edit(fakeBookingRooms[0]);
    expect(component.editId()).toBe(1);
    expect(component.form().roomId).toBe(1);
    expect(component.form().pricePerNight).toBe(2000);
    expect(component.form().numberOfRooms).toBe(1);
  });

  it('resetForm should clear editId and reset form', () => {
    component.edit(fakeBookingRooms[0]);
    component.resetForm();
    expect(component.editId()).toBeNull();
    expect(component.form().roomId).toBe(0);
    expect(component.form().numberOfRooms).toBe(1);
  });

  it('save should not call API when no room selected', () => {
    component.resetForm();
    component.save();
    httpMock.expectNone(r => r.url.includes('bookingroom') && r.method === 'POST');
  });

  it('should render booking rooms list', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('.br-card').length).toBe(2);
  });
});
