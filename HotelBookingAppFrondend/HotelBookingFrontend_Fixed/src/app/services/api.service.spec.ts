import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { APIService } from './api.service';
import { CreateBookingModel } from '../models/booking.model';
import { LoginModel } from '../models/login.model';
import { RegisterModel } from '../models/register.model';
import { environment } from '../../environments/environment';

const API = environment.apiUrl;

describe('APIService', () => {
  let service: APIService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service  = TestBed.inject(APIService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  // ── Auth ──────────────────────────────────────────────────────────────
  it('apiLogin should POST to /auth/login with credentials', () => {
    const model = new LoginModel();
    model.email = 'user@hotel.com'; model.password = 'Pass@123';
    service.apiLogin(model).subscribe();
    const req = httpMock.expectOne(`${API}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.email).toBe('user@hotel.com');
    req.flush({ token: 'fake-token' });
  });

  it('apiRegister should POST to /users/register', () => {
    const model = new RegisterModel();
    model.userName = 'John'; model.email = 'j@j.com'; model.password = 'Pass@1'; model.role = 'user';
    service.apiRegister(model).subscribe();
    const req = httpMock.expectOne(`${API}/users/register`);
    expect(req.request.method).toBe('POST');
    req.flush({ userId: 1 });
  });

  // ── Users ─────────────────────────────────────────────────────────────
  it('apiGetAllUsers should GET /users/GetAllUsers', () => {
    service.apiGetAllUsers().subscribe();
    const req = httpMock.expectOne(`${API}/users/GetAllUsers`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('apiGetUserById should GET /users/:id', () => {
    service.apiGetUserById(3).subscribe();
    const req = httpMock.expectOne(`${API}/users/3`);
    expect(req.request.method).toBe('GET');
    req.flush({ userId: 3 });
  });

  it('apiDeleteUser should DELETE /users/:id', () => {
    service.apiDeleteUser(5).subscribe();
    const req = httpMock.expectOne(`${API}/users/5`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  // ── Hotels ────────────────────────────────────────────────────────────
  it('apiGetHotelsPaged should POST to /hotel/paged', () => {
    service.apiGetHotelsPaged({ pageNumber: 1, pageSize: 10 }).subscribe();
    const req = httpMock.expectOne(`${API}/hotel/paged`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.pageNumber).toBe(1);
    req.flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 10 });
  });

  it('apiGetHotelById should GET /hotel/:id', () => {
    service.apiGetHotelById(1).subscribe();
    const req = httpMock.expectOne(`${API}/hotel/1`);
    expect(req.request.method).toBe('GET');
    req.flush({ hotelId: 1 });
  });

  it('apiSearchHotels should GET /hotel/search with location param', () => {
    service.apiSearchHotels('Mumbai').subscribe();
    const req = httpMock.expectOne(r => r.url.includes('hotel/search') && r.params.get('location') === 'Mumbai');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('apiFilterHotels should POST to /hotel/filter with page params', () => {
    service.apiFilterHotels({ location: 'Goa', minRating: 4 }, { pageNumber: 1, pageSize: 9 }).subscribe();
    const req = httpMock.expectOne(r => r.url.includes('hotel/filter'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body.location).toBe('Goa');
    expect(req.request.body.minRating).toBe(4);
    req.flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 9 });
  });

  it('apiCreateHotel should POST to /hotel', () => {
    service.apiCreateHotel({ hotelName: 'New Hotel', location: 'Delhi' }).subscribe();
    const req = httpMock.expectOne(`${API}/hotel`);
    expect(req.request.method).toBe('POST');
    req.flush({ hotelId: 99 });
  });

  it('apiUpdateHotel should PUT to /hotel/:id', () => {
    service.apiUpdateHotel(1, { hotelName: 'Updated' }).subscribe();
    const req = httpMock.expectOne(`${API}/hotel/1`);
    expect(req.request.method).toBe('PUT');
    req.flush({ hotelId: 1 });
  });

  it('apiDeleteHotel should DELETE /hotel/:id', () => {
    service.apiDeleteHotel(1).subscribe();
    const req = httpMock.expectOne(`${API}/hotel/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  // ── Rooms ─────────────────────────────────────────────────────────────
  it('apiGetRooms should GET /room with no params when hotelId not provided', () => {
    service.apiGetRooms().subscribe();
    const req = httpMock.expectOne(`${API}/room`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('apiGetRooms should include hotelId param when provided', () => {
    service.apiGetRooms(2).subscribe();
    const req = httpMock.expectOne(r => r.url.includes('/room') && r.params.get('hotelId') === '2');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('apiGetRoomById should GET /room/:id', () => {
    service.apiGetRoomById(10).subscribe();
    const req = httpMock.expectOne(`${API}/room/10`);
    expect(req.request.method).toBe('GET');
    req.flush({ roomId: 10 });
  });

  it('apiFilterRooms should POST to /room/filter', () => {
    service.apiFilterRooms({ onlyAvailable: true }).subscribe();
    const req = httpMock.expectOne(`${API}/room/filter`);
    expect(req.request.method).toBe('POST');
    req.flush([]);
  });

  it('apiCreateRoom should POST to /room', () => {
    service.apiCreateRoom({ hotelId: 1, roomType: 'Suite', pricePerNight: 5000 }).subscribe();
    const req = httpMock.expectOne(`${API}/room`);
    expect(req.request.method).toBe('POST');
    req.flush({ roomId: 55 });
  });

  it('apiDeleteRoom should DELETE /room/:id', () => {
    service.apiDeleteRoom(10).subscribe();
    const req = httpMock.expectOne(`${API}/room/10`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  // ── Bookings ──────────────────────────────────────────────────────────
  it('apiCreateBooking should POST to /booking', () => {
    const model = new CreateBookingModel();
    model.userId = 1; model.hotelId = 2; model.roomId = 3;
    service.apiCreateBooking(model).subscribe();
    const req = httpMock.expectOne(`${API}/booking`);
    expect(req.request.method).toBe('POST');
    req.flush({ bookingId: 1 });
  });

  it('apiGetBookingById should GET /booking/:id', () => {
    service.apiGetBookingById(42).subscribe();
    const req = httpMock.expectOne(`${API}/booking/42`);
    expect(req.request.method).toBe('GET');
    req.flush({ bookingId: 42 });
  });

  it('apiGetBookingsByUser should POST to /booking/user/:id/paged', () => {
    service.apiGetBookingsByUser(5, { pageNumber: 1, pageSize: 5 }).subscribe();
    const req = httpMock.expectOne(`${API}/booking/user/5/paged`);
    expect(req.request.method).toBe('POST');
    req.flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 5 });
  });

  it('apiGetBookingsByHotel should POST to /booking/hotel/:id/paged', () => {
    service.apiGetBookingsByHotel(1, { pageNumber: 1, pageSize: 10 }).subscribe();
    const req = httpMock.expectOne(`${API}/booking/hotel/1/paged`);
    expect(req.request.method).toBe('POST');
    req.flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 10 });
  });

  it('apiConfirmBooking should PUT to /booking/:id/confirm', () => {
    service.apiConfirmBooking(7).subscribe();
    const req = httpMock.expectOne(`${API}/booking/7/confirm`);
    expect(req.request.method).toBe('PUT');
    req.flush({ bookingId: 7, status: 'Confirmed' });
  });

  it('apiCancelBooking should PUT to /booking/:id/cancel', () => {
    service.apiCancelBooking(7).subscribe();
    const req = httpMock.expectOne(`${API}/booking/7/cancel`);
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('apiCompleteBooking should PUT to /booking/:id/complete', () => {
    service.apiCompleteBooking(7).subscribe();
    const req = httpMock.expectOne(`${API}/booking/7/complete`);
    expect(req.request.method).toBe('PUT');
    req.flush({ bookingId: 7, status: 'Completed' });
  });

  // ── Payments ──────────────────────────────────────────────────────────
  it('apiMakePayment should POST to /payment', () => {
    service.apiMakePayment(1, 5000, 'UPI').subscribe();
    const req = httpMock.expectOne(`${API}/payment`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.paymentMethod).toBe('UPI');
    req.flush({ paymentId: 1 });
  });

  it('apiGetAllPayments should GET /payment', () => {
    service.apiGetAllPayments().subscribe();
    const req = httpMock.expectOne(`${API}/payment`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('apiUpdatePaymentStatus should PUT to /payment/:id/status', () => {
    service.apiUpdatePaymentStatus(3, 'Completed').subscribe();
    const req = httpMock.expectOne(r => r.url.includes('payment/3/status'));
    expect(req.request.method).toBe('PUT');
    req.flush({ paymentStatus: 'Completed' });
  });

  // ── Reviews ───────────────────────────────────────────────────────────
  it('apiCreateReview should POST to /review', () => {
    service.apiCreateReview(1, 5, 4, 'Great stay!').subscribe();
    const req = httpMock.expectOne(`${API}/review`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.rating).toBe(4);
    expect(req.request.body.comment).toBe('Great stay!');
    req.flush({ reviewId: 1 });
  });

  it('apiGetReviewsPaged should POST to /review/paged', () => {
    service.apiGetReviewsPaged(1, { pageNumber: 1, pageSize: 10 }).subscribe();
    const req = httpMock.expectOne(r => r.url.includes('review/paged'));
    expect(req.request.method).toBe('POST');
    req.flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 10 });
  });

  it('apiDeleteReview should DELETE /review/:id', () => {
    service.apiDeleteReview(8).subscribe();
    const req = httpMock.expectOne(`${API}/review/8`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  // ── Amenities ─────────────────────────────────────────────────────────
  it('apiGetAmenities should GET /amenities', () => {
    service.apiGetAmenities().subscribe();
    const req = httpMock.expectOne(`${API}/amenities`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('apiCreateAmenity should POST to /amenities', () => {
    service.apiCreateAmenity('Pool', 'Swimming pool', '🏊').subscribe();
    const req = httpMock.expectOne(`${API}/amenities`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.name).toBe('Pool');
    req.flush({ amenityId: 1 });
  });

  it('apiDeleteAmenity should DELETE /amenities/:id', () => {
    service.apiDeleteAmenity(2).subscribe();
    const req = httpMock.expectOne(`${API}/amenities/2`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  // ── Wishlist ──────────────────────────────────────────────────────────
  it('apiAddToWishlist should POST to /wishlist', () => {
    service.apiAddToWishlist(5, 1).subscribe();
    const req = httpMock.expectOne(`${API}/wishlist`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.userId).toBe(5);
    expect(req.request.body.hotelId).toBe(1);
    req.flush({ wishlistId: 1 });
  });

  it('apiGetWishlist should GET /wishlist/user/:userId', () => {
    service.apiGetWishlist(5).subscribe();
    const req = httpMock.expectOne(`${API}/wishlist/user/5`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('apiRemoveWishlist should DELETE /wishlist/:id', () => {
    service.apiRemoveWishlist(10).subscribe();
    const req = httpMock.expectOne(`${API}/wishlist/10`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  // ── Notifications ─────────────────────────────────────────────────────
  it('apiGetMyNotifications should GET /notification/my', () => {
    service.apiGetMyNotifications().subscribe();
    const req = httpMock.expectOne(`${API}/notification/my`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('apiMarkNotificationRead should PUT /notification/:id/read', () => {
    service.apiMarkNotificationRead(3).subscribe();
    const req = httpMock.expectOne(`${API}/notification/3/read`);
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('apiDeleteNotification should DELETE /notification/:id', () => {
    service.apiDeleteNotification(4).subscribe();
    const req = httpMock.expectOne(`${API}/notification/4`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  // ── Cancellations ─────────────────────────────────────────────────────
  it('apiCreateCancellation should POST to /cancellation', () => {
    service.apiCreateCancellation(1, 'Changed plans').subscribe();
    const req = httpMock.expectOne(`${API}/cancellation`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.reason).toBe('Changed plans');
    req.flush({ cancellationId: 1 });
  });

  it('apiGetCancellationsByUser should POST /cancellation/user/:id/paged', () => {
    service.apiGetCancellationsByUser(5, { pageNumber: 1, pageSize: 5 }).subscribe();
    const req = httpMock.expectOne(`${API}/cancellation/user/5/paged`);
    expect(req.request.method).toBe('POST');
    req.flush({ data: [], totalRecords: 0, totalPages: 0, pageNumber: 1, pageSize: 5 });
  });

  // ── HotelAmenity ──────────────────────────────────────────────────────
  it('apiGetAllHotelAmenities should GET /hotelamenity', () => {
    service.apiGetAllHotelAmenities().subscribe();
    const req = httpMock.expectOne(`${API}/hotelamenity`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('apiGetHotelAmenitiesByHotel should GET /hotel/:id/amenities', () => {
    service.apiGetHotelAmenitiesByHotel(2).subscribe();
    const req = httpMock.expectOne(`${API}/hotel/2/amenities`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('apiGetHotelAmenityById should GET /hotelamenity/:id', () => {
    service.apiGetHotelAmenityById(1).subscribe();
    const req = httpMock.expectOne(`${API}/hotelamenity/1`);
    expect(req.request.method).toBe('GET');
    req.flush({ hotelAmenityId: 1, hotelId: 1, amenityId: 1 });
  });

  it('apiCreateHotelAmenity should POST to /hotelamenity with hotelId and amenityId', () => {
    const model = { hotelId: 1, amenityId: 2 };
    service.apiCreateHotelAmenity(model as any).subscribe();
    const req = httpMock.expectOne(`${API}/hotelamenity`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.hotelId).toBe(1);
    expect(req.request.body.amenityId).toBe(2);
    req.flush({ hotelAmenityId: 5, hotelId: 1, amenityId: 2 });
  });

  it('apiDeleteHotelAmenity should DELETE /hotelamenity/:id', () => {
    service.apiDeleteHotelAmenity(3).subscribe();
    const req = httpMock.expectOne(`${API}/hotelamenity/3`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

});
