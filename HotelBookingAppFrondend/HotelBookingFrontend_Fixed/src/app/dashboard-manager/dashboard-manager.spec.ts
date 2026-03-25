import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardManager } from './dashboard-manager';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';
import { BookingModel } from '../models/booking.model';

const fakeBookings: BookingModel[] = [
  { bookingId: 1, userId: 3, hotelId: 1, hotelName: 'Grand', roomId: 10, numberOfRooms: 1, checkIn: '2025-12-01', checkOut: '2025-12-03', totalAmount: 4000, status: 'Pending' },
  { bookingId: 2, userId: 4, hotelId: 1, hotelName: 'Grand', roomId: 11, numberOfRooms: 2, checkIn: '2025-12-05', checkOut: '2025-12-07', totalAmount: 6000, status: 'Confirmed' }
];

describe('DashboardManager', () => {
  let component: DashboardManager;
  let fixture: ComponentFixture<DashboardManager>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardManager],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(DashboardManager);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    const hotelPagedReq = httpMock.expectOne(r => r.url.includes('hotel/paged'));
    hotelPagedReq.flush({ data: [{ hotelId: 1, hotelName: 'Grand' }], totalRecords: 1, totalPages: 1, pageNumber: 1, pageSize: 1 });

    const bookingsReq = httpMock.expectOne(r => r.url.includes('booking/hotel/1/paged'));
    bookingsReq.flush({ data: fakeBookings, totalRecords: 2, totalPages: 1, pageNumber: 1, pageSize: 10 });

    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load bookings', () => {
    expect(component.bookings().length).toBe(2);
  });

  it('total should be 2', () => {
    expect(component.total).toBe(2);
  });

  it('pending count should be 1', () => {
    expect(component.pending).toBe(1);
  });

  it('confirmed count should be 1', () => {
    expect(component.confirmed).toBe(1);
  });

  it('revenue should sum all totalAmounts', () => {
    const total = (4000 + 6000).toLocaleString('en-IN');
    expect(component.revenue).toBe(total);
  });

  it('activeTab defaults to bookings', () => {
    expect(component.activeTab()).toBe('bookings');
  });

  it('filterStatus defaults to all', () => {
    expect(component.filterStatus()).toBe('all');
  });

  it('filtered returns all bookings when filter is all', () => {
    expect(component.filtered.length).toBe(2);
  });

  it('filtered returns only Pending when filter set', () => {
    component.filterStatus.set('Pending');
    expect(component.filtered.length).toBe(1);
    expect(component.filtered[0].status).toBe('Pending');
  });

  it('filtered returns only Confirmed', () => {
    component.filterStatus.set('Confirmed');
    expect(component.filtered.length).toBe(1);
    expect(component.filtered[0].status).toBe('Confirmed');
  });

  it('avgRating returns 0 when no reviews', () => {
    expect(component.avgRating).toBe('0');
  });

  it('getStatusClass returns badge-confirmed for Confirmed', () => {
    expect(component.getStatusClass('Confirmed')).toBe('badge-confirmed');
  });

  it('getStatusClass returns badge-pending for Pending', () => {
    expect(component.getStatusClass('Pending')).toBe('badge-pending');
  });

  it('getStars returns correct length array', () => {
    expect(component.getStars(4).length).toBe(4);
  });

  it('loading should be false after bookings loaded', () => {
    expect(component.loading()).toBeFalse();
  });
});
