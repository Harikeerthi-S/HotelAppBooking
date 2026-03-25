import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Notifications } from './notifications';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';
import { NotificationModel } from '../models/notification.model';

const fakeNotifications: NotificationModel[] = [
  { notificationId: 1, userId: 5, message: 'Booking confirmed', isRead: false, createdAt: '2025-12-01T10:00:00' },
  { notificationId: 2, userId: 5, message: 'Payment received', isRead: true,  createdAt: '2025-11-30T09:00:00' },
  { notificationId: 3, userId: 5, message: 'New offer available', isRead: false, createdAt: '2025-11-29T08:00:00' }
];

describe('Notifications', () => {
  let component: Notifications;
  let fixture: ComponentFixture<Notifications>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Notifications],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(Notifications);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    const notifReq = httpMock.expectOne(r => r.url.includes('notification/my'));
    notifReq.flush(fakeNotifications);

    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load 3 notifications', () => {
    expect(component.notifications().length).toBe(3);
  });

  it('unreadCount should be 2', () => {
    expect(component.unreadCount).toBe(2);
  });

  it('loading should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('notifications should be sorted newest first', () => {
    const dates = component.notifications().map(n => new Date(n.createdAt).getTime());
    expect(dates[0]).toBeGreaterThan(dates[1]);
    expect(dates[1]).toBeGreaterThan(dates[2]);
  });

  it('markRead should set isRead to true', () => {
    const n = { ...fakeNotifications[0] };
    component.markRead(n);
    const req = httpMock.expectOne(r => r.url.includes('notification/1/read'));
    req.flush({});
    expect(component.notifications()[0].isRead).toBeTrue();
  });

  it('deleteNotification should remove notification from list', () => {
    component.deleteNotification(fakeNotifications[1]);
    const req = httpMock.expectOne(r => r.url.includes('notification/2'));
    req.flush({});
    expect(component.notifications().length).toBe(2);
    expect(component.notifications().find(n => n.notificationId === 2)).toBeUndefined();
  });

  it('markAllRead calls markRead for each unread notification', () => {
    component.markAllRead();
    const reqs = httpMock.match(r => r.url.includes('notification') && r.url.includes('/read'));
    reqs.forEach(r => r.flush({}));
    expect(reqs.length).toBe(2);
  });

  it('unreadCount should be 0 after marking all read', () => {
    component.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
    expect(component.unreadCount).toBe(0);
  });

  it('should render notification items', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('.notif-row').length).toBe(3);
  });

  it('should show unread dot for unread notifications', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('.unread-dot').length).toBe(2);
  });
});
