export class HotelAmenityModel {
  hotelAmenityId: number = 0;
  hotelId: number = 0;
  amenityId: number = 0;
  amenityName: string = '';
  amenityIcon?: string;
  amenityDescription?: string;
}

export class CreateHotelAmenityModel {
  hotelId: number = 0;
  amenityId: number = 0;
}
