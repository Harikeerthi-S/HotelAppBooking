export class HotelFilter {
  hotelId?: number;
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

export class ReviewFilter {
  hotelId?: number;
  userId?: number;
  rating?: number;
}

export class AuditLogFilter {
  userId?: number;
  action?: string;
  entityName?: string;
  entityId?: number;
  fromDate?: string;
  toDate?: string;
}
