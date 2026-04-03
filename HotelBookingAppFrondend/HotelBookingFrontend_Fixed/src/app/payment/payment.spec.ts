import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Payment } from './payment';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from '../app.routes';
import { BookingModel } from '../models/booking.model';

const fakeBooking: BookingModel = {
  bookingId: 42, userId: 5, hotelId: 1, hotelName: 'Grand Palace',
  roomId: 10, numberOfRooms: 1, checkIn: '2025-12-01', checkOut: '2025-12-03',
  totalAmount: 5000, status: 'Pending'
};

describe('Payment', () => {
  let component: Payment;
  let fixture: ComponentFixture<Payment>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Payment],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideToastr(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { params: { bookingId: '42' } } }
        }
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(Payment);
    component = fixture.componentInstance;
    httpMock  = TestBed.inject(HttpTestingController);

    const bookingReq = httpMock.expectOne(r => r.url.includes('booking/42'));
    bookingReq.flush(fakeBooking);

    await fixture.whenStable();
  });

  afterEach(() => httpMock.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load booking data', () => {
    expect(component.booking()?.bookingId).toBe(42);
    expect(component.booking()?.totalAmount).toBe(5000);
  });

  it('should have 2 payment methods', () => {
    expect(component.paymentMethods.length).toBe(2);
  });

  it('should contain CreditCard as payment method', () => {
    const values = component.paymentMethods.map(m => m.value);
    expect(values).toContain('CreditCard');
  });

  it('should contain DebitCard as payment method', () => {
    const values = component.paymentMethods.map(m => m.value);
    expect(values).toContain('DebitCard');
  });

  it('selectedMethod defaults to empty string', () => {
    expect(component.selectedMethod()).toBe('');
  });

  it('selectMethod should update selectedMethod signal', () => {
    component.selectMethod('CreditCard');
    expect(component.selectedMethod()).toBe('CreditCard');
  });

  it('getMethodLabel should return correct label for CreditCard', () => {
    component.selectMethod('CreditCard');
    expect(component.getMethodLabel()).toBe('Credit Card');
  });

  it('getMethodLabel should return correct label for DebitCard', () => {
    component.selectMethod('DebitCard');
    expect(component.getMethodLabel()).toBe('Debit Card');
  });

  it('getMethodLabel returns empty string when no method selected', () => {
    expect(component.getMethodLabel()).toBe('');
  });

  it('loading starts as false', () => {
    expect(component.loading()).toBeFalse();
  });

  it('should not call payment API when no method selected', () => {
    component.pay();
    httpMock.expectNone(r => r.url.includes('payment'));
  });

  it('should render payment methods', async () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('.pay-method').length).toBe(2);
  });
});
