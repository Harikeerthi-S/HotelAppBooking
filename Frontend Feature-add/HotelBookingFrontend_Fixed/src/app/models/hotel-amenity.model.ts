export class HotelAmenityModel {
  hotelAmenityId: number = 0;
  hotelId: number = 0;
  amenityId: number = 0;
  amenityName: string = '';
  amenityIcon?: string;
  amenityDescription?: string;
  isAvailable: boolean = true; // New field to track if this amenity is available for booking
}

export class CreateHotelAmenityModel {
  hotelId: number = 0;
  amenityId: number = 0;
}
