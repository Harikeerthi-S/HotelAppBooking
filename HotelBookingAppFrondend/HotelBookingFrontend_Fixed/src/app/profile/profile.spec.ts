import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Profile } from './profile';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';

describe('Profile', () => {
  let component: Profile;
  let fixture: ComponentFixture<Profile>;

  beforeEach(async () => {
    sessionStorage.setItem('hotel_user', JSON.stringify({
      userId: 7, userName: 'Alice', email: 'alice@hotel.com', role: 'user'
    }));

    await TestBed.configureTestingModule({
      imports: [Profile],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(Profile);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  afterEach(() => sessionStorage.removeItem('hotel_user'));

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load userName from sessionStorage', () => {
    expect(component.userName()).toBe('Alice');
  });

  it('should load email from sessionStorage', () => {
    expect(component.email()).toBe('alice@hotel.com');
  });

  it('should load role from sessionStorage', () => {
    expect(component.role()).toBe('user');
  });

  it('should load userId from sessionStorage', () => {
    expect(component.userId()).toBe(7);
  });

  it('getInitial returns first letter uppercase', () => {
    expect(component.getInitial()).toBe('A');
  });

  it('getRoleLabel returns Guest for user role', () => {
    expect(component.getRoleLabel()).toContain('Guest');
  });

  it('getRoleLabel returns Administrator for admin role', () => {
    component.role.set('admin');
    expect(component.getRoleLabel()).toContain('Administrator');
  });

  it('getRoleLabel returns Hotel Manager for hotelmanager role', () => {
    component.role.set('hotelmanager');
    expect(component.getRoleLabel()).toContain('Hotel Manager');
  });

  it('logout should clear sessionStorage', () => {
    component.logout();
    expect(sessionStorage.getItem('token')).toBeNull();
  });

  it('should render profile avatar', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.profile-avatar')).toBeTruthy();
  });
});
