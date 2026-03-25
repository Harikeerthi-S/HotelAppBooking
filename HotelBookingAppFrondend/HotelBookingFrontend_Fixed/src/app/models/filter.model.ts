export class HotelFilter {
  location?: string;
  minRating?: number;
  minPrice?: number;
  maxPrice?: number;
  amenityId?: number;
}

export class RoomFilter {
  hotelId?: number;
  roomType?: string;
  minPrice?: number;
  maxPrice?: number;
  minCapacity?: number;
  maxCapacity?: number;
  onlyAvailable: boolean = true;
}
