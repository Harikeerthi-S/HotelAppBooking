import { environment } from '../../environments/environment';

/**
 * Resolves a relative backend media path (e.g. /uploads/reviews/photo.png)
 * to a full URL using the configured mediaUrl.
 * Absolute URLs (http/https) are returned unchanged.
 */
export function resolveMediaUrl(path: string | null | undefined): string {
  if (!path) return '';
  if (path.startsWith('http://') || path.startsWith('https://')) return path;
  return `${environment.mediaUrl}${path}`;
}
