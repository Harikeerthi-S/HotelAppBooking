import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Login } from './login';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('form should be invalid when empty', () => {
    expect(component.loginForm.invalid).toBeTrue();
  });

  it('email field should be required', () => {
    component.email?.setValue('');
    expect(component.email?.errors?.['required']).toBeTruthy();
  });

  it('email field should reject invalid format', () => {
    component.email?.setValue('not-an-email');
    expect(component.email?.errors?.['email']).toBeTruthy();
  });

  it('password field should be required', () => {
    component.password?.setValue('');
    expect(component.password?.errors?.['required']).toBeTruthy();
  });

  it('password should enforce minimum 6 characters', () => {
    component.password?.setValue('abc');
    expect(component.password?.errors?.['minlength']).toBeTruthy();
  });

  it('form should be valid with correct values', () => {
    component.email?.setValue('user@hotel.com');
    component.password?.setValue('Password1');
    expect(component.loginForm.valid).toBeTrue();
  });

  it('loading starts as false', () => {
    expect(component.loading()).toBeFalse();
  });

  it('showPass starts as false', () => {
    expect(component.showPass()).toBeFalse();
  });

  it('should NOT call API when form is invalid', () => {
    component.login();
    httpMock.expectNone(r => r.url.includes('auth/login'));
  });

  it('should mark all fields touched on invalid submit', () => {
    component.login();
    expect(component.email?.touched).toBeTrue();
    expect(component.password?.touched).toBeTrue();
  });

  it('should NOT have a fillDemo method (demo buttons removed)', () => {
    expect((component as any).fillDemo).toBeUndefined();
  });

  it('should NOT have a role field (no role selection)', () => {
    expect(component.loginForm.get('role')).toBeNull();
  });

  it('should render email and password fields only', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('#loginEmail')).toBeTruthy();
    expect(compiled.querySelector('#loginPassword')).toBeTruthy();
  });

  it('should NOT render demo account buttons', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(compiled.querySelectorAll('button'));
    const demoBtn = buttons.find(b =>
      b.textContent?.includes('Admin') ||
      b.textContent?.includes('Manager') ||
      b.textContent?.includes('🛡️') ||
      b.textContent?.includes('🏨') ||
      b.textContent?.includes('🧳')
    );
    expect(demoBtn).toBeUndefined();
  });

  it('should render register link', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const link = compiled.querySelector('a[routerLink="/register"]');
    expect(link).toBeTruthy();
  });

  it('should render sign-in button', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const submitBtn = compiled.querySelector('button[type="submit"]');
    expect(submitBtn).toBeTruthy();
  });
});
