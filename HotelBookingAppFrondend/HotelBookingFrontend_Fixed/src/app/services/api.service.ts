import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import { LoginModel } from '../models/login.model';
import { RegisterModel } from '../models/register.model';
import { UserModel } from '../models/user.model';
import { HotelModel } from '../models/hotel.model';
import { RoomModel } from '../models/room.model';
import { BookingModel, CreateBookingModel } from '../models/booking.model';
import { PaymentModel } from '../models/payment.model';
import { CancellationModel } from '../models/cancellation.model';
import { ReviewModel } from '../models/review.model';
import { AmenityModel } from '../models/amenity.model';
import { WishlistModel } from '../models/wishlist.model';
import { NotificationModel } from '../models/notification.model';
import { PagedRequest, PagedResponse } from '../models/paged.model';
import { HotelFilter, RoomFilter } from '../models/filter.model';
import { HotelAmenityModel, CreateHotelAmenityModel } from '../models/hotel-amenity.model';
import { BookingRoomModel, CreateBookingRoomModel } from '../models/booking-room.model';

const API = environment.apiUrl;

@Injectable({ providedIn: 'root' })
export class APIService {
  private http = inject(HttpClient);

  /* ── Auth ── */
  apiLogin(model: LoginModel) {
    return this.http.post<{ token: string }>(`${API}/auth/login`, model);
  }
  apiRegister(model: RegisterModel) {
    return this.http.post(`${API}/users/register`, model);
  }

  /* ── Users ── */
  apiGetAllUsers() {
    return this.http.get<UserModel[]>(`${API}/users/GetAllUsers`);
  }
  apiGetUserById(userId: number) {
    return this.http.get<UserModel>(`${API}/users/${userId}`);
  }
  apiDeleteUser(userId: number) {
    return this.http.delete(`${API}/users/${userId}`);
  }

  /* ── Hotels ── */
  apiGetHotelsPaged(req: PagedRequest) {
    return this.http.post<PagedResponse<HotelModel>>(`${API}/hotel/paged`, req);
  }
  apiFilterHotels(filter: HotelFilter, req: PagedRequest) {
    const params = new HttpParams()
      .set('pageNumber', req.pageNumber)
      .set('pageSize', req.pageSize);
    return this.http.post<PagedResponse<HotelModel>>(`${API}/hotel/filter`, filter, { params });
  }
  apiGetHotelById(hotelId: number) {
    return this.http.get<HotelModel>(`${API}/hotel/${hotelId}`);
  }
  apiSearchHotels(location: string) {
    return this.http.get<HotelModel[]>(`${API}/hotel/search`, { params: { location } });
  }
  apiCreateHotel(model: Partial<HotelModel>) {
    return this.http.post<HotelModel>(`${API}/hotel`, model);
  }
  apiUpdateHotel(hotelId: number, model: Partial<HotelModel>) {
    return this.http.put<HotelModel>(`${API}/hotel/${hotelId}`, model);
  }
  apiDeleteHotel(hotelId: number) {
    return this.http.delete(`${API}/hotel/${hotelId}`);
  }

  /* ── Rooms ── */
  apiGetRooms(hotelId?: number) {
    const params = hotelId ? new HttpParams().set('hotelId', hotelId) : undefined;
    return this.http.get<RoomModel[]>(`${API}/room`, { params });
  }
  apiGetRoomById(roomId: number) {
    return this.http.get<RoomModel>(`${API}/room/${roomId}`);
  }
  apiFilterRooms(filter: RoomFilter) {
    return this.http.post<RoomModel[]>(`${API}/room/filter`, filter);
  }
  apiFilterRoomsPaged(filter: RoomFilter, req: PagedRequest) {
    const params = new HttpParams()
      .set('pageNumber', req.pageNumber)
      .set('pageSize', req.pageSize);
    return this.http.post<PagedResponse<RoomModel>>(`${API}/room/filter/paged`, filter, { params });
  }
  apiCreateRoom(model: Partial<RoomModel>) {
    return this.http.post<RoomModel>(`${API}/room`, model);
  }
  apiUpdateRoom(roomId: number, model: Partial<RoomModel>) {
    return this.http.put<RoomModel>(`${API}/room/${roomId}`, model);
  }
  apiDeleteRoom(roomId: number) {
    return this.http.delete(`${API}/room/${roomId}`);
  }

  /* ── Bookings ── */
  apiCreateBooking(model: CreateBookingModel) {
    return this.http.post<BookingModel>(`${API}/booking`, model);
  }
  apiGetBookingById(bookingId: number) {
    return this.http.get<BookingModel>(`${API}/booking/${bookingId}`);
  }
  /** GET all bookings paged — Admin only. Uses new POST /api/booking/all/paged */
  apiGetAllBookingsPaged(req: PagedRequest) {
  return this.http.post<any>(`${API}/booking/all/paged`, req);
  }

  apiGetBookingsByUser(userId: number, req: PagedRequest) {
    return this.http.post<PagedResponse<BookingModel>>(`${API}/booking/user/${userId}/paged`, req);
  }
  apiGetBookingsByHotel(hotelId: number, req: PagedRequest) {
    return this.http.post<PagedResponse<BookingModel>>(`${API}/booking/hotel/${hotelId}/paged`, req);
  }
  apiConfirmBooking(bookingId: number) {
    return this.http.put<BookingModel>(`${API}/booking/${bookingId}/confirm`, {});
  }
  apiCancelBooking(bookingId: number) {
    return this.http.put(`${API}/booking/${bookingId}/cancel`, {});
  }
  apiCompleteBooking(bookingId: number) {
    return this.http.put<BookingModel>(`${API}/booking/${bookingId}/complete`, {});
  }

  /* ── Payments ── */
  apiMakePayment(bookingId: number, amount: number, paymentMethod: string) {
    return this.http.post<PaymentModel>(`${API}/payment`, { bookingId, amount, paymentMethod });
  }
  apiGetAllPayments() {
    return this.http.get<PaymentModel[]>(`${API}/payment`);
  }
  apiGetPaymentById(paymentId: number) {
    return this.http.get<PaymentModel>(`${API}/payment/${paymentId}`);
  }
  /** GET payment by booking ID — uses new GET /api/payment/booking/{bookingId} */
  apiGetPaymentByBookingId(bookingId: number) {
    return this.http.get<PaymentModel>(`${API}/payment/booking/${bookingId}`);
  }
  apiGetPaymentsPaged(req: PagedRequest) {
    return this.http.post<PagedResponse<PaymentModel>>(`${API}/payment/paged`, req);
  }
  apiUpdatePaymentStatus(paymentId: number, status: string) {
    return this.http.put<PaymentModel>(`${API}/payment/${paymentId}/status`, null, { params: { status } });
  }

  /* ── Cancellations ── */
  apiCreateCancellation(bookingId: number, reason: string) {
    return this.http.post<CancellationModel>(`${API}/cancellation`, { bookingId, reason });
  }
  apiGetCancellationById(cancellationId: number) {
    return this.http.get<CancellationModel>(`${API}/cancellation/${cancellationId}`);
  }
  apiGetCancellationsByUser(userId: number, req: PagedRequest) {
    return this.http.post<PagedResponse<CancellationModel>>(`${API}/cancellation/user/${userId}/paged`, req);
  }
  /** GET all cancellations paged — Admin/Manager only. Uses new POST /api/cancellation/paged */
  apiGetAllCancellationsPaged(req: PagedRequest) {
    return this.http.post<PagedResponse<CancellationModel>>(`${API}/cancellation/paged`, req);
  }
  apiUpdateCancellationStatus(cancellationId: number, status: string, refundAmount: number = 0) {
    const params = new HttpParams().set('status', status).set('refundAmount', refundAmount);
    return this.http.put<CancellationModel>(`${API}/cancellation/${cancellationId}/status`, null, { params });
  }

  /* ── Reviews ── */
  apiCreateReview(hotelId: number, userId: number, rating: number, comment: string) {
    return this.http.post<ReviewModel>(`${API}/review`, { hotelId, userId, rating, comment });
  }
  apiGetReviewsPaged(hotelId: number, req: PagedRequest) {
    const params = new HttpParams()
      .set('pageNumber', req.pageNumber)
      .set('pageSize', req.pageSize);
    return this.http.post<PagedResponse<ReviewModel>>(`${API}/review/paged`, { hotelId }, { params });
  }
  /** GET all reviews paged (no hotel filter) — Admin/Manager only. Uses new POST /api/review/all/paged */
  apiGetAllReviewsPaged(req: PagedRequest) {
    return this.http.post<PagedResponse<ReviewModel>>(`${API}/review/paged`, req);
  }
  apiDeleteReview(reviewId: number) {
    return this.http.delete(`${API}/review/${reviewId}`);
  }

  /* ── Amenities ── */
  apiGetAmenities() {
    return this.http.get<AmenityModel[]>(`${API}/amenities`);
  }
  apiCreateAmenity(name: string, description: string, icon: string) {
    return this.http.post<AmenityModel>(`${API}/amenities`, { name, description, icon });
  }
  apiUpdateAmenity(id: number, name: string, description: string, icon: string) {
    return this.http.put(`${API}/amenities/${id}`, { name, description, icon });
  }
  apiDeleteAmenity(id: number) {
    return this.http.delete(`${API}/amenities/${id}`);
  }

  /* ── Wishlist ── */
  apiAddToWishlist(userId: number, hotelId: number) {
    return this.http.post<WishlistModel>(`${API}/wishlist`, { userId, hotelId });
  }
  apiGetWishlist(userId: number) {
    return this.http.get<WishlistModel[]>(`${API}/wishlist/user/${userId}`);
  }
  apiRemoveWishlist(wishlistId: number) {
    return this.http.delete(`${API}/wishlist/${wishlistId}`);
  }
  apiRemoveWishlistByHotel(userId: number, hotelId: number) {
    return this.http.delete(`${API}/wishlist/remove`, { params: { userId, hotelId } });
  }

  /* ── Notifications ── */
  apiGetMyNotifications() {
    return this.http.get<NotificationModel[]>(`${API}/notification/my`);
  }
  apiMarkNotificationRead(id: number) {
    return this.http.put(`${API}/notification/${id}/read`, {});
  }
  apiDeleteNotification(id: number) {
    return this.http.delete(`${API}/notification/${id}`);
  }

  /* ── HotelAmenity ── */
  apiGetAllHotelAmenities() {
    return this.http.get<HotelAmenityModel[]>(`${API}/hotelamenity`);
  }
  apiGetHotelAmenityById(id: number) {
    return this.http.get<HotelAmenityModel>(`${API}/hotelamenity/${id}`);
  }
  apiCreateHotelAmenity(model: CreateHotelAmenityModel) {
    return this.http.post<HotelAmenityModel>(`${API}/hotelamenity`, model);
  }
  apiDeleteHotelAmenity(id: number) {
    return this.http.delete(`${API}/hotelamenity/${id}`);
  }

  /* ── BookingRoom ── */
  apiCreateBookingRoom(model: CreateBookingRoomModel) {
    return this.http.post<BookingRoomModel>(`${API}/bookingroom`, model);
  }
  apiGetBookingRoomById(bookingRoomId: number) {
    return this.http.get<BookingRoomModel>(`${API}/bookingroom/${bookingRoomId}`);
  }
  apiGetBookingRoomsByBookingId(bookingId: number) {
    return this.http.get<BookingRoomModel[]>(`${API}/bookingroom/booking/${bookingId}`);
  }
  apiUpdateBookingRoom(bookingRoomId: number, model: CreateBookingRoomModel) {
    return this.http.put<BookingRoomModel>(`${API}/bookingroom/${bookingRoomId}`, model);
  }
  apiDeleteBookingRoom(bookingRoomId: number) {
    return this.http.delete(`${API}/bookingroom/${bookingRoomId}`);
  }
}
