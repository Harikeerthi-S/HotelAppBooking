import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../environments/environment';

const SERVER_BASE = environment.apiUrl.replace(/\/api\/?$/, '');

// Unsplash fallbacks
const HOTEL_IMGS = [
  'https://images.unsplash.com/photo-1566073771259-6a8506099945?w=600&h=400&fit=crop&auto=format',
  'https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=600&h=400&fit=crop&auto=format',
  'https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=600&h=400&fit=crop&auto=format',
  'https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?w=600&h=400&fit=crop&auto=format',
  'https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?w=600&h=400&fit=crop&auto=format',
];

const ROOM_IMGS = [
  'https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=400&h=260&fit=crop&auto=format',
  'https://images.unsplash.com/photo-1618773928121-c32242e63f39?w=400&h=260&fit=crop&auto=format',
  'https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=400&h=260&fit=crop&auto=format',
  'https://images.unsplash.com/photo-1595576508898-0ad5c879a061?w=400&h=260&fit=crop&auto=format',
];

function isExternalUrl(p: string): boolean {
  return p.startsWith('http://') || p.startsWith('https://') || p.startsWith('//');
}

/** Resolve hotel image — Unsplash fallback for any non-http path */
export function resolveHotelImage(imagePath: string | null | undefined, hotelId = 0): string {
  if (imagePath && isExternalUrl(imagePath)) return imagePath;
  return HOTEL_IMGS[Math.abs(hotelId) % HOTEL_IMGS.length];
}

/** Resolve room image — Unsplash fallback for any non-http path */
export function resolveRoomImage(imageUrl: string | null | undefined, roomId = 0): string {
  if (imageUrl && isExternalUrl(imageUrl)) return imageUrl;
  return ROOM_IMGS[Math.abs(roomId) % ROOM_IMGS.length];
}

/**
 * Angular pipe — use in templates: {{ room.imageUrl | imgUrl : room.roomId }}
 */
@Pipe({ name: 'imgUrl', standalone: true, pure: true })
export class ImgUrlPipe implements PipeTransform {
  transform(value: string | null | undefined, id = 0): string {
    return resolveRoomImage(value, id);
  }
}
