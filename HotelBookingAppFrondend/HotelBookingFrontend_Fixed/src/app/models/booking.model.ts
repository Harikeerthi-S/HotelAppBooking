export class BookingModel {
  bookingId: number = 0;
  userId: number = 0;
  hotelId: number = 0;
  hotelName: string = '';
  roomId: number = 0;
  numberOfRooms: number = 1;
  checkIn: string = '';
  checkOut: string = '';
  totalAmount: number = 0;
  status: string = '';
}

export class CreateBookingModel {
  userId: number = 0;
  hotelId: number = 0;
  roomId: number = 0;
  numberOfRooms: number = 1;
  checkIn: string = '';
  checkOut: string = '';
}
