import { Component, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { NotificationModel } from '../models/notification.model';

@Component({
  selector: 'app-notifications',
  imports: [CommonModule, DatePipe, RouterLink],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css'
})
export class Notifications {
  private apiService = inject(APIService);
  private toastr     = inject(ToastrService);

  notifications = signal<NotificationModel[]>([]);
  loading       = signal(true);
  filter        = signal<'all' | 'unread' | 'read'>('all');

  get unreadCount(): number {
    return this.notifications().filter(n => !n.isRead).length;
  }

  get filtered(): NotificationModel[] {
    const f = this.filter();
    if (f === 'unread') return this.notifications().filter(n => !n.isRead);
    if (f === 'read')   return this.notifications().filter(n => n.isRead);
    return this.notifications();
  }

  constructor() { this.loadNotifications(); }

  loadNotifications(): void {
    this.loading.set(true);
    // NOTE: route /notifications now protected by userGuard (see app.routes.ts)
    // Backend GET /notification/my is [Authorize(Roles="user")] only
    this.apiService.apiGetMyNotifications().subscribe({
      next: (list) => {
        const sorted = [...list].sort(
          (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        );
        this.notifications.set(sorted);
        this.loading.set(false);
      },
      error: (e) => {
        this.toastr.error(e?.error?.message || 'Failed to load notifications.', 'Error');
        this.loading.set(false);
      }
    });
  }

  markRead(n: NotificationModel): void {
    if (n.isRead) return;
    this.apiService.apiMarkNotificationRead(n.notificationId).subscribe({
      next: () => this.notifications.update(list =>
        list.map(x => x.notificationId === n.notificationId ? { ...x, isRead: true } : x)
      ),
      error: () => {}
    });
  }

  markAllRead(): void {
    if (!this.unreadCount) { this.toastr.info('All notifications are already read.'); return; }
    this.notifications().filter(n => !n.isRead).forEach(n => this.markRead(n));
    this.toastr.success('All notifications marked as read.');
  }

  deleteNotification(n: NotificationModel): void {
    this.apiService.apiDeleteNotification(n.notificationId).subscribe({
      next: () => {
        this.notifications.update(list => list.filter(x => x.notificationId !== n.notificationId));
        this.toastr.info('Notification deleted.');
      },
      error: () => {}
    });
  }

  setFilter(f: 'all' | 'unread' | 'read'): void { this.filter.set(f); }
}
