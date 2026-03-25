import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardUser } from './dashboard-user';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';
import { BookingModel } from '../models/booking.model';

const fakeBookings: BookingModel[] = [
  { bookingId: 1, userId: 5, hotelId: 1, hotelName: 'Grand Palace', roomId: 10, numberOfRooms: 1, checkIn: '2025-12-01', checkOut: '2025-12-03', totalAmount: 5000, status: 'Confirmed' },
  { bookingId: 2, userId: 5, hotelId: 2, hotelName: 'Sea View', roomId: 20, numberOfRooms: 2, checkIn: '2025-11-15', checkOut: '2025-11-18', totalAmount: 9000, status: 'Pending' }
];

describe('DashboardUser', () => {
  let component: DashboardUser;
  let fixture: ComponentFixture<DashboardUser>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.setItem('hotel_user', JSON.stringify({ userId: 5, userName: 'John', email: 'j@j.com', role: 'user' }));

    await TestBed.configureTestingModule({
      imports: [DashboardUser],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(DashboardUser);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    const bookingsReq = httpMock.expectOne(r => r.url.includes('booking/user/5/paged'));
    bookingsReq.flush({ data: fakeBookings, totalRecords: 2, totalPages: 1, pageNumber: 1, pageSize: 5 });

    const notifReq = httpMock.expectOne(r => r.url.includes('notification/my'));
    notifReq.flush([]);

    const cancelReq = httpMock.expectOne(r => r.url.includes('cancellation/user/5/paged'));
    cancelReq.flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 5 });

    await fixture.whenStable();
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.removeItem('hotel_user');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load userName from sessionStorage', () => {
    expect(component.userName()).toBe('John');
  });

  it('should load userId from sessionStorage', () => {
    expect(component.userId()).toBe(5);
  });

  it('should have 2 bookings after load', () => {
    expect(component.totalBookings).toBe(2);
  });

  it('confirmedBookings count should be 1', () => {
    expect(component.confirmedBookings).toBe(1);
  });

  it('pendingBookings count should be 1', () => {
    expect(component.pendingBookings).toBe(1);
  });

  it('totalSpent should sum all booking amounts', () => {
    const total = 5000 + 9000;
    expect(component.totalSpent).toBe(total.toLocaleString('en-IN'));
  });

  it('unreadCount should be 0 when no notifications', () => {
    expect(component.unreadCount).toBe(0);
  });

  it('showCancelModal defaults to false', () => {
    expect(component.showCancelModal()).toBeFalse();
  });

  it('openCancelModal should set bookingId and show modal', () => {
    component.openCancelModal(fakeBookings[0]);
    expect(component.showCancelModal()).toBeTrue();
    expect(component.cancelBookingId()).toBe(1);
  });

  it('getStatusClass returns correct class for Confirmed', () => {
    expect(component.getStatusClass('Confirmed')).toBe('badge-confirmed');
  });

  it('getStatusClass returns correct class for Pending', () => {
    expect(component.getStatusClass('Pending')).toBe('badge-pending');
  });

  it('getStatusClass returns correct class for Cancelled', () => {
    expect(component.getStatusClass('Cancelled')).toBe('badge-cancelled');
  });

  it('should render stat cards', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('.stat-card').length).toBe(4);
  });
});
