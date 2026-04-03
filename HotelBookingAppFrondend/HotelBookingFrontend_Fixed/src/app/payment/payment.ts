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

  step           = signal<'select' | 'confirm' | 'processing'>('select');

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
  }

  getMethodLabel(): string {
    return this.paymentMethods.find(m => m.value === this.selectedMethod())?.label ?? '';
  }

  proceedToConfirm(): void {
    if (!this.selectedMethod()) { this.toastr.warning('Please select a payment method.'); return; }
    this.pay();
  }

  isConfirmDetailsValid(): boolean {
    return true;
  }

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
        } else {
          this.toastr.warning('Payment could not be processed. Please try again.', 'Error');
        }
      },
      error: (e) => {
        this.loading.set(false);
        this.toastr.error(e?.error?.message || 'Payment error. Try again.', 'Error');
      }
    });
  }
}
