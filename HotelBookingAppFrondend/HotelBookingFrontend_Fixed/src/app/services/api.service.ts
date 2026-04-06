import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginModel } from '../models/login.model';
import { RegisterModel } from '../models/register.model';
import { UserModel } from '../models/user.model';
import { HotelModel } from '../models/hotel.model';
import { RoomModel, CreateRoomModel } from '../models/room.model';
import { BookingModel, CreateBookingModel } from '../models/booking.model';
import { PaymentModel } from '../models/payment.model';
import { ChatRequestModel, ChatResponseModel, ChatHistoryModel } from '../models/chat.model';
import { CancellationModel } from '../models/cancellation.model';
import { ReviewModel } from '../models/review.model';
import { AmenityModel } from '../models/amenity.model';
import { WishlistModel } from '../models/wishlist.model';
import { NotificationModel } from '../models/notification.model';
import { PagedRequest, PagedResponse } from '../models/paged.model';
import { HotelFilter, RoomFilter, ReviewFilter, AuditLogFilter } from '../models/filter.model';
import { HotelAmenityModel, CreateHotelAmenityModel } from '../models/hotel-amenity.model';
import { AuditLogModel, CreateAuditLogModel } from '../models/audit-log.model';

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
  apiGetUsersPaged(req: PagedRequest) {
    return this.http.post<PagedResponse<UserModel>>(`${API}/users/paged`, req);
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
  apiGetRoomsPaged(req: PagedRequest, hotelId?: number) {
    const params = hotelId ? new HttpParams().set('hotelId', hotelId) : undefined;
    return this.http.post<PagedResponse<RoomModel>>(`${API}/room/all/paged`, req, { params });
  }
  apiGetRoomById(roomId: number) {
    return this.http.get<RoomModel>(`${API}/room/${roomId}`);
  }
  apiCheckRoomAvailability(roomId: number, checkIn: string, checkOut: string) {
    return this.http.get<{ roomId: number; checkIn: string; checkOut: string; isAvailable: boolean }>(
      `${API}/room/${roomId}/availability`, { params: { checkIn, checkOut } }
    );
  }
  apiFilterRooms(filter: RoomFilter) {
    return this.http.post<RoomModel[]>(`${API}/room/filter`, filter);
  }
  apiCreateRoom(model: CreateRoomModel) {
    return this.http.post<RoomModel>(`${API}/room`, model);
  }
  apiUpdateRoom(roomId: number, model: CreateRoomModel) {
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
  apiGetAllBookingsPaged(req: PagedRequest) {
    return this.http.post<PagedResponse<BookingModel>>(`${API}/booking/all/paged`, req);
  }
  apiGetBookingsByUser(userId: number, req: PagedRequest) {
    return this.http.post<PagedResponse<BookingModel>>(`${API}/booking/user/${userId}/paged`, req);
  }
  apiGetBookingsByHotel(hotelId: number, req: PagedRequest) {
    return this.http.post<PagedResponse<BookingModel>>(`${API}/booking/hotel/${hotelId}/paged`, req);
  }
  apiGetPendingBookingsByHotel(hotelId: number) {
    return this.http.get<BookingModel[]>(`${API}/booking/hotel/${hotelId}/pending`);
  }
  apiConfirmBooking(bookingId: number) {
    return this.http.put<BookingModel>(`${API}/booking/${bookingId}/confirm`, {});
  }
  apiCompleteBooking(bookingId: number) {
    return this.http.put<BookingModel>(`${API}/booking/${bookingId}/complete`, {});
  }
  apiCancelBooking(bookingId: number) {
    return this.http.put(`${API}/booking/${bookingId}/cancel`, {});
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
  apiGetPaymentByBookingId(bookingId: number) {
    return this.http.get<PaymentModel>(`${API}/payment/booking/${bookingId}`).pipe(
      catchError(err => err.status === 404 ? of(null) : of(null))
    );
  }
  apiUpdatePaymentStatus(paymentId: number, status: string) {
    return this.http.put<PaymentModel>(`${API}/payment/${paymentId}/status`, null, { params: { status } });
  }
  apiGetPaymentsPaged(req: PagedRequest) {
    return this.http.post<PagedResponse<PaymentModel>>(`${API}/payment/paged`, req);
  }
  apiGetPaymentsByUserPaged(userId: number, req: PagedRequest) {
    return this.http.post<PagedResponse<PaymentModel>>(`${API}/payment/user/${userId}/paged`, req);
  }

  /* ── Cancellations ── */
  apiCreateCancellation(bookingId: number, reason: string) {
    return this.http.post<CancellationModel>(`${API}/cancellation`, { bookingId, reason });
  }
  apiGetCancellationById(cancellationId: number) {
    return this.http.get<CancellationModel>(`${API}/cancellation/${cancellationId}`);
  }
  apiGetAllCancellationsPaged(req: PagedRequest) {
    return this.http.post<PagedResponse<CancellationModel>>(`${API}/cancellation/paged`, req);
  }
  apiGetCancellationsByUser(userId: number, req: PagedRequest) {
    return this.http.post<PagedResponse<CancellationModel>>(`${API}/cancellation/user/${userId}/paged`, req);
  }
  apiUpdateCancellationStatus(cancellationId: number, status: string, refundAmount: number = 0) {
    const params = new HttpParams().set('status', status).set('refundAmount', refundAmount);
    return this.http.put<CancellationModel>(`${API}/cancellation/${cancellationId}/status`, null, { params });
  }

  /* ── Reviews ── */
  apiCreateReview(hotelId: number, userId: number, rating: number, comment: string) {
    return this.http.post<ReviewModel>(`${API}/review`, { hotelId, userId, rating, comment });
  }
  apiGetReviewById(reviewId: number) {
    return this.http.get<ReviewModel>(`${API}/review/${reviewId}`);
  }
  apiGetReviewsPaged(filter: ReviewFilter, req: PagedRequest) {
    const params = new HttpParams()
      .set('pageNumber', req.pageNumber)
      .set('pageSize', req.pageSize);
    return this.http.post<PagedResponse<ReviewModel>>(`${API}/review/paged`, filter, { params });
  }
  apiDeleteReview(reviewId: number) {
    return this.http.delete(`${API}/review/${reviewId}`);
  }

  /* ── Amenities ── */
  apiGetAmenities() {
    return this.http.get<AmenityModel[]>(`${API}/amenities`);
  }
  apiGetAmenityById(amenityId: number) {
    return this.http.get<AmenityModel>(`${API}/amenities/${amenityId}`);
  }
  apiCreateAmenity(name: string, description: string, icon: string) {
    return this.http.post<AmenityModel>(`${API}/amenities`, { name, description, icon });
  }
  apiUpdateAmenity(id: number, name: string, description: string, icon: string) {
    return this.http.put<AmenityModel>(`${API}/amenities/${id}`, { name, description, icon });
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
  apiCreateNotification(userId: number, message: string) {
    return this.http.post<NotificationModel>(`${API}/notification`, { userId, message });
  }
  apiGetAllNotifications() {
    return this.http.get<NotificationModel[]>(`${API}/notification/all`);
  }
  apiGetNotificationsByUser(userId: number) {
    return this.http.get<NotificationModel[]>(`${API}/notification/user/${userId}`);
  }
  apiGetMyNotifications() {
    return this.http.get<NotificationModel[]>(`${API}/notification/my`);
  }
  apiGetNotificationById(notificationId: number) {
    return this.http.get<NotificationModel>(`${API}/notification/${notificationId}`);
  }
  apiMarkNotificationRead(id: number) {
    return this.http.put(`${API}/notification/${id}/read`, {});
  }
  apiDeleteNotification(id: number) {
    return this.http.delete(`${API}/notification/${id}`);
  }
  apiGetNotificationsPaged(req: PagedRequest) {
    return this.http.post<PagedResponse<NotificationModel>>(`${API}/notification/paged`, req);
  }
  apiGetNotificationsByUserPaged(userId: number, req: PagedRequest) {
    return this.http.post<PagedResponse<NotificationModel>>(`${API}/notification/user/${userId}/paged`, req);
  }
  apiGetMyNotificationsPaged(req: PagedRequest) {
    return this.http.post<PagedResponse<NotificationModel>>(`${API}/notification/my/paged`, req);
  }
  apiGetMyUnreadNotificationCount() {
    return this.http.get<{ count: number }>(`${API}/notification/my/unread-count`);
  }
  apiGetAllUnreadNotificationCount() {
    return this.http.get<{ count: number }>(`${API}/notification/all/unread-count`);
  }

  /* ── HotelAmenity ── */
  apiGetAllHotelAmenities() {
    return this.http.get<HotelAmenityModel[]>(`${API}/hotelamenity`);
  }
  apiGetHotelAmenitiesByHotel(hotelId: number) {
    return this.http.get<HotelAmenityModel[]>(`${API}/hotel/${hotelId}/amenities`);
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

  /* ── AuditLog (Admin only) ── */
  apiCreateAuditLog(model: CreateAuditLogModel) {
    return this.http.post<AuditLogModel>(`${API}/auditlog`, model);
  }
  apiGetAuditLogById(auditLogId: number) {
    return this.http.get<AuditLogModel>(`${API}/auditlog/${auditLogId}`);
  }
  apiGetAllAuditLogsPaged(req: PagedRequest) {
    return this.http.post<PagedResponse<AuditLogModel>>(`${API}/auditlog/all/paged`, req);
  }
  apiFilterAuditLogs(filter: AuditLogFilter) {
    const clean: Record<string, unknown> = {};
    Object.entries(filter).forEach(([k, v]) => {
      if (v !== '' && v !== null && v !== undefined) clean[k] = v;
    });
    return this.http.post<AuditLogModel[]>(`${API}/auditlog/filter`, clean);
  }
  apiFilterAuditLogsPaged(filter: AuditLogFilter | Record<string, unknown>, req: PagedRequest) {
    // If filter already contains pageNumber/pageSize (from admin dashboard), use as-is
    // Otherwise merge with req
    const hasPage = 'pageNumber' in filter;
    const body = hasPage ? filter : { ...filter, pageNumber: req.pageNumber, pageSize: req.pageSize };
    return this.http.post<PagedResponse<AuditLogModel>>(`${API}/auditlog/filter/paged`, body);
  }
  apiGetAuditLogsByEntity(entityName: string, entityId: number) {
    return this.http.get<AuditLogModel[]>(`${API}/auditlog/entity`, { params: { entityName, entityId } });
  }
  apiGetAuditLogsByUser(userId: number) {
    return this.http.get<AuditLogModel[]>(`${API}/auditlog/user/${userId}`);
  }
  apiDeleteAuditLog(auditLogId: number) {
    return this.http.delete(`${API}/auditlog/${auditLogId}`);
  }

  /* ── Chat / AI Support ── */
  apiChatMessage(model: ChatRequestModel) {
    return this.http.post<ChatResponseModel>(`${API}/chat/message`, model);
  }
  apiGetChatHistory(sessionId: string) {
    return this.http.get<ChatHistoryModel[]>(`${API}/chat/history/${sessionId}`);
  }

  /* ── User Amenity Preferences ── */
  /** POST empty JSON — matches ASP.NET Core [HttpPost("…/approve")] */
}
