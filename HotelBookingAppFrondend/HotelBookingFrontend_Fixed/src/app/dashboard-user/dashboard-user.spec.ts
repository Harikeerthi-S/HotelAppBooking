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

const emptyPaged = { data: [] as unknown[], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 10 };

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

    httpMock.expectOne(r => r.url.includes('booking/user/5/paged')).flush({
      data: fakeBookings,
      totalRecords: 2,
      totalPages: 1,
      pageNumber: 1,
      pageSize: 10
    });

    httpMock.expectOne(r => r.url.includes('notification/my/paged')).flush(emptyPaged);

    httpMock.expectOne(r => r.url.includes('unread-count')).flush({ count: 0 });

    httpMock.expectOne(r => r.url.includes('cancellation/user/5/paged')).flush(emptyPaged);

    httpMock.expectOne(r => r.url.includes('/amenities')).flush([]);

    httpMock.expectOne(r => r.url.includes('/room/10/availability')).flush({
      roomId: 10, checkIn: '2025-12-01', checkOut: '2025-12-03', isAvailable: true
    });
    httpMock.expectOne(r => r.url.includes('/room/20/availability')).flush({
      roomId: 20, checkIn: '2025-11-15', checkOut: '2025-11-18', isAvailable: true
    });

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
    expect(component.userName).toBe('John');
  });

  it('should have 2 bookings after load', () => {
    expect(component.totalBookings()).toBe(2);
  });

  it('confirmedBookings count should be 1', () => {
    expect(component.confirmedBookings()).toBe(1);
  });

  it('pendingBookings count should be 1', () => {
    expect(component.pendingBookings()).toBe(1);
  });

  it('totalSpent should sum booking amounts on first page', () => {
    const total = 5000 + 9000;
    expect(component.totalSpent()).toBe(total);
  });

  it('unreadCount should be 0 when no notifications', () => {
    expect(component.unreadCount()).toBe(0);
  });

  it('showCancelForm defaults to false', () => {
    expect(component.showCancelForm()).toBeFalse();
  });

  it('openCancelForm should set bookingId and show form', () => {
    component.openCancelForm(fakeBookings[0]);
    expect(component.showCancelForm()).toBeTrue();
    expect(component.formBookingId()).toBe(1);
  });

  it('statusClass returns correct class for Confirmed', () => {
    expect(component.statusClass('Confirmed')).toBe('badge-confirmed');
  });

  it('statusClass returns correct class for Pending', () => {
    expect(component.statusClass('Pending')).toBe('badge-pending');
  });

  it('statusClass returns correct class for Cancelled', () => {
    expect(component.statusClass('Cancelled')).toBe('badge-cancelled');
  });

  it('should render stat cards', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('.stat-card').length).toBe(4);
  });
});
