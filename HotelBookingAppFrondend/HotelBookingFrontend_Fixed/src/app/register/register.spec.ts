import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Register } from './register';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';

describe('Register', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Register],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr()
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('form should be invalid when empty', () => {
    expect(component.registerForm.invalid).toBeTrue();
  });

  it('userName should require minimum 2 characters', () => {
    component.userName?.setValue('A');
    expect(component.userName?.errors?.['minlength']).toBeTruthy();
  });

  it('email should be required', () => {
    component.email?.setValue('');
    expect(component.email?.errors?.['required']).toBeTruthy();
  });

  it('email should reject invalid format', () => {
    component.email?.setValue('bad-email');
    expect(component.email?.errors?.['email']).toBeTruthy();
  });

  it('password should require uppercase letter and number', () => {
    component.password?.setValue('alllowercase');
    expect(component.password?.errors?.['pattern']).toBeTruthy();
  });

  it('password should enforce minimum 8 characters', () => {
    component.password?.setValue('Ab1');
    expect(component.password?.errors?.['minlength']).toBeTruthy();
  });

  it('form is valid with correct values', () => {
    component.userName?.setValue('John Doe');
    component.email?.setValue('john@example.com');
    component.password?.setValue('Password1');
    expect(component.registerForm.valid).toBeTrue();
  });

  it('loading starts as false', () => {
    expect(component.loading()).toBeFalse();
  });

  it('showPass starts as false', () => {
    expect(component.showPass()).toBeFalse();
  });

  it('should NOT have a role field in the form (role selector removed)', () => {
    expect(component.registerForm.get('role')).toBeNull();
  });

  it('should NOT have a setRole method (role selector removed)', () => {
    expect((component as any).setRole).toBeUndefined();
  });

  it('register() always submits with role = user', () => {
    component.userName?.setValue('Test User');
    component.email?.setValue('test@example.com');
    component.password?.setValue('Password1');
    component.register();
    const req = httpMock.expectOne(r => r.url.includes('users/register'));
    expect(req.request.body.role).toBe('user');
    req.flush({});
  });

  it('should mark all fields touched on invalid submit', () => {
    component.register();
    expect(component.userName?.touched).toBeTrue();
    expect(component.email?.touched).toBeTrue();
  });

  it('should NOT call API when form is invalid', () => {
    component.register();
    httpMock.expectNone(r => r.url.includes('users/register'));
  });

  it('should NOT render role selector cards', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const roleCards = compiled.querySelectorAll('.role-option');
    expect(roleCards.length).toBe(0);
  });

  it('should NOT render Guest or Hotel Manager role buttons', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const text = compiled.textContent ?? '';
    expect(text).not.toContain('Hotel Manager');
    expect(text).not.toContain('Guest');
  });

  it('should render name, email, phone and password fields', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('#regName')).toBeTruthy();
    expect(compiled.querySelector('#regEmail')).toBeTruthy();
    expect(compiled.querySelector('#regPhone')).toBeTruthy();
    expect(compiled.querySelector('#regPassword')).toBeTruthy();
  });

  it('should render sign-in link', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const link = compiled.querySelector('a[routerLink="/login"]');
    expect(link).toBeTruthy();
  });
});
