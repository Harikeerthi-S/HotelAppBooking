import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { BookingModel } from '../models/booking.model';

@Component({
  selector: 'app-payment',
  imports: [CommonModule, FormsModule],
  templateUrl: './payment.html',
  styleUrl: './payment.css'
})
export class Payment {
  private apiService = inject(APIService);
  private toastr     = inject(ToastrService);
  private route      = inject(ActivatedRoute);
  private router     = inject(Router);

  booking        = signal<BookingModel | null>(null);
  selectedMethod = signal('');
  loading        = signal(false);

  // Card details (frontend only — never sent to backend)
  cardNumber  = '';
  cardHolder  = '';
  cardExpiry  = '';
  cardCvv     = '';

  // step: 'select' → 'card' (if card method) → 'confirm' → 'processing'
  step = signal<'select' | 'card' | 'confirm'>('select');

  paymentMethods = [
    { value: 'CreditCard', label: 'Credit Card', icon: '💳', sub: 'Visa / Mastercard' },
    { value: 'DebitCard',  label: 'Debit Card',  icon: '🏦', sub: 'All banks'         }
  ];

  constructor() {
    const bookingId = +this.route.snapshot.params['bookingId'];
    this.apiService.apiGetBookingById(bookingId).subscribe({
      next: b => this.booking.set(b),
      error: () => this.toastr.error('Booking not found.')
    });
  }

  selectMethod(value: string): void {
    this.selectedMethod.set(value);
    this.step.set('select');
    // reset card fields on method change
    this.cardNumber = '';
    this.cardHolder = '';
    this.cardExpiry = '';
    this.cardCvv    = '';
  }

  getMethodLabel(): string {
    return this.paymentMethods.find(m => m.value === this.selectedMethod())?.label ?? '';
  }

  /** Format card number with spaces every 4 digits */
  formatCardNumber(event: Event): void {
    const input = event.target as HTMLInputElement;
    let val = input.value.replace(/\D/g, '').slice(0, 16);
    this.cardNumber = val.replace(/(.{4})/g, '$1 ').trim();
    input.value = this.cardNumber;
  }

  /** Format expiry as MM/YY */
  formatExpiry(event: Event): void {
    const input = event.target as HTMLInputElement;
    let val = input.value.replace(/\D/g, '').slice(0, 4);
    if (val.length >= 3) val = val.slice(0, 2) + '/' + val.slice(2);
    this.cardExpiry = val;
    input.value = val;
  }

  get rawCardNumber(): string {
    return this.cardNumber.replace(/\s/g, '');
  }

  get maskedCard(): string {
    const raw = this.rawCardNumber;
    if (raw.length < 4) return '**** **** **** ****';
    return '**** **** **** ' + raw.slice(-4);
  }

  isCardValid(): boolean {
    return (
      this.rawCardNumber.length === 16 &&
      this.cardHolder.trim().length >= 3 &&
      /^\d{2}\/\d{2}$/.test(this.cardExpiry) &&
      this.cardCvv.length >= 3
    );
  }

  /** Called from "Proceed" button on method select step */
  proceedToCard(): void {
    if (!this.selectedMethod()) {
      this.toastr.warning('Please select a payment method.');
      return;
    }
    this.step.set('card');
  }

  /** Called from "Review Payment" button on card details step */
  proceedToConfirm(): void {
    if (!this.isCardValid()) {
      this.toastr.warning('Please fill in all card details correctly.');
      return;
    }
    this.step.set('confirm');
  }

  backToSelect(): void { this.step.set('select'); }
  backToCard(): void   { this.step.set('card'); }

  /** Final pay — called from confirm step */
  pay(): void {
    if (!this.booking()) return;

    this.loading.set(true);
    this.apiService.apiMakePayment(
      this.booking()!.bookingId,
      this.booking()!.totalAmount,
      this.selectedMethod()
    ).subscribe({
      next: p => {
        this.loading.set(false);
        const status = p?.paymentStatus || (p as any)?.PaymentStatus;
        if (status === 'Completed') {
          this.toastr.success('Payment successful! Booking confirmed. 🎉', 'Payment Done');
          this.router.navigateByUrl('/dashboard-user');
        } else if (status === 'Failed') {
          this.toastr.error('Payment failed. Please try again.', 'Failed');
          this.step.set('card');
        } else {
          this.toastr.warning('Payment could not be processed. Please try again.', 'Error');
          this.step.set('card');
        }
      },
      error: (e) => {
        this.loading.set(false);
        this.toastr.error(e?.error?.message || 'Payment error. Try again.', 'Error');
        this.step.set('card');
      }
    });
  }
}
