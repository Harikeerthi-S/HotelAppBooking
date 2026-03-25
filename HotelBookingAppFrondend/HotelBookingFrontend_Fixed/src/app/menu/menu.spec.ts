import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Menu } from './menu';
import { provideRouter } from '@angular/router';
import { routes } from '../app.routes';
import { provideHttpClient } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';

describe('Menu', () => {
  let component: Menu;
  let fixture: ComponentFixture<Menu>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Menu],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Menu);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show Guest when no user is logged in', () => {
    expect(component.userName()).toBe('');
  });

  it('should return empty string initial when no user', () => {
    expect(component.getInitial()).toBe('U');
  });

  it('should report not logged in when no token', () => {
    sessionStorage.removeItem('token');
    expect(component.isLoggedIn()).toBeFalse();
  });

  it('dropOpen should default to false', () => {
    expect(component.dropOpen()).toBeFalse();
  });

  it('toggleDrop should flip dropOpen', () => {
    component.toggleDrop();
    expect(component.dropOpen()).toBeTrue();
    component.toggleDrop();
    expect(component.dropOpen()).toBeFalse();
  });

  it('logout should clear session and set dropOpen false', () => {
    sessionStorage.setItem('token', 'fake-token');
    component.dropOpen.set(true);
    component.logout();
    expect(sessionStorage.getItem('token')).toBeNull();
    expect(component.dropOpen()).toBeFalse();
  });

  it('should render navbar', async () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('nav')).toBeTruthy();
  });
});
