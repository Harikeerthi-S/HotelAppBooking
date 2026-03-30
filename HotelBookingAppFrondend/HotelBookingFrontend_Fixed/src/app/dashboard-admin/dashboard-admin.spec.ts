import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardAdmin } from './dashboard-admin';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';

describe('DashboardAdmin', () => {
  let component: DashboardAdmin;
  let fixture: ComponentFixture<DashboardAdmin>;
  let httpMock: HttpTestingController;

  function flushAll(): void {
    httpMock.expectOne(r => r.url.includes('hotel/paged') && r.body?.pageSize === 10).flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 10 });
    httpMock.expectOne(r => r.url.includes('/room') && r.url.includes('paged')).flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 10 });
    httpMock.expectOne(r => r.url.includes('auditlog/all/paged')).flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 10 });
    httpMock.expectOne(r => r.url.includes('users/GetAllUsers')).flush([]);
    httpMock.expectOne(r => r.url.includes('amenities')).flush([]);
    httpMock.expectOne(r => r.url.includes('payment') && r.method === 'GET').flush([]);
    httpMock.expectOne(r => r.url.includes('hotel/paged') && r.body?.pageSize === 1000).flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 1000 });
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardAdmin],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(DashboardAdmin);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    flushAll();
    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('activeTab defaults to Hotels', () => {
    expect(component.activeTab()).toBe('Hotels');
  });

  it('tabs should contain all 6 management tabs', () => {
    expect(component.tabs).toContain('Hotels');
    expect(component.tabs).toContain('Rooms');
    expect(component.tabs).toContain('Users');
    expect(component.tabs).toContain('Bookings');
    expect(component.tabs).toContain('Amenities');
    expect(component.tabs).toContain('Payments');
  });

  it('saving defaults to false', () => {
    expect(component.saving()).toBeFalse();
  });

  it('editHotelId defaults to null', () => {
    expect(component.editHotelId()).toBeNull();
  });

  it('editRoomId defaults to null', () => {
    expect(component.editRoomId()).toBeNull();
  });

  it('editHotel should populate hf with hotel values', () => {
    const h = { hotelId: 1, hotelName: 'Test', location: 'Mumbai', address: 'Addr', starRating: 4, totalRooms: 10, contactNumber: '9999', imagePath: '' };
    component.editHotel(h as any);
    expect(component.editHotelId()).toBe(1);
    expect(component.hf().hotelName).toBe('Test');
    expect(component.hf().location).toBe('Mumbai');
  });

  it('resetHotel should clear form and editHotelId', () => {
    component.editHotelId.set(5);
    component.hf.set({ hotelName: 'Test', location: 'Mumbai', address: '', starRating: 4, totalRooms: 10, contactNumber: '', imagePath: '' });
    component.resetHotel();
    expect(component.editHotelId()).toBeNull();
    expect(component.hf().hotelName).toBe('');
  });

  it('editRoom should populate rf with room values', () => {
    const r = { roomId: 5, hotelId: 1, roomNumber: 101, roomType: 'Suite', pricePerNight: 5000, capacity: 4, isAvailable: true };
    component.editRoom(r as any);
    expect(component.editRoomId()).toBe(5);
    expect(component.rf().roomType).toBe('Suite');
  });

  it('resetRoom should clear form and editRoomId', () => {
    component.editRoomId.set(3);
    component.resetRoom();
    expect(component.editRoomId()).toBeNull();
    expect(component.rf().roomType).toBe('Standard');
  });

  it('getStatusClass should return correct class for Confirmed', () => {
    expect(component.getStatusClass('Confirmed')).toBe('badge-confirmed');
  });

  it('getStatusClass should return correct class for Cancelled', () => {
    expect(component.getStatusClass('Cancelled')).toBe('badge-cancelled');
  });

  it('getPayClass should return badge-confirmed for Completed payment', () => {
    expect(component.getPayClass('Completed')).toBe('badge-confirmed');
  });

  it('getPayClass should return badge-cancelled for Failed payment', () => {
    expect(component.getPayClass('Failed')).toBe('badge-cancelled');
  });

  it('should render tab buttons', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const buttons = compiled.querySelectorAll('button');
    expect(buttons.length).toBeGreaterThan(0);
  });
});
