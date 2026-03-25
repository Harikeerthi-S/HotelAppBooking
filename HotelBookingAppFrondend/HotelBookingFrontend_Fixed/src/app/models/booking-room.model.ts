export class BookingRoomModel {
  bookingRoomId: number = 0;
  bookingId: number = 0;
  roomId: number = 0;
  pricePerNight: number = 0;
  numberOfRooms: number = 1;
}

export class CreateBookingRoomModel {
  bookingId: number = 0;
  roomId: number = 0;
  pricePerNight: number = 0;
  numberOfRooms: number = 1;
}
