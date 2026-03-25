import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { APIService } from '../services/api.service';
import { ToastrService } from 'ngx-toastr';
import { BookingModel } from '../models/booking.model';

@Component({
  selector: 'app-payment',
  imports: [CommonModule, RouterLink],
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

  paymentMethods = [
    { value: 'CreditCard', label: 'Credit Card',  icon: '💳', sub: 'Visa / Mastercard' },
    { value: 'DebitCard',  label: 'Debit Card',   icon: '🏦', sub: 'All banks'         },
    { value: 'UPI',        label: 'UPI',           icon: '📱', sub: 'GPay / PhonePe'   },
    { value: 'Wallet',     label: 'Wallet',        icon: '👛', sub: 'Paytm / Amazon'   },
    { value: 'PayPal',     label: 'PayPal',        icon: '🅿️', sub: 'International'    }
  ];

  constructor() {
    const bookingId = +this.route.snapshot.params['bookingId'];
    this.apiService.apiGetBookingById(bookingId).subscribe({
      next: b => this.booking.set(b),
      error: () => this.toastr.error('Booking not found.')
    });
  }

  selectMethod(value: string): void { this.selectedMethod.set(value); }

  getMethodLabel(): string {
    return this.paymentMethods.find(m => m.value === this.selectedMethod())?.label ?? '';
  }

  pay(): void {
    if (!this.selectedMethod()) { this.toastr.warning('Please select a payment method.'); return; }
    if (!this.booking()) return;

    this.loading.set(true);
    this.apiService.apiMakePayment(
      this.booking()!.bookingId,
      this.booking()!.totalAmount,
      this.selectedMethod()
    ).subscribe({
      next: p => {
        this.loading.set(false);
        if (p.paymentStatus === 'Completed') {
          this.toastr.success('Payment successful! Booking confirmed. 🎉', 'Payment Done');
          this.router.navigateByUrl('/dashboard-user');
        } else if (p.paymentStatus === 'Pending') {
          this.toastr.info('Payment is pending confirmation.', 'Pending');
          this.router.navigateByUrl('/dashboard-user');
        } else {
          this.toastr.warning('Payment failed. Please try again.', 'Failed');
        }
      },
      error: (e) => {
        this.loading.set(false);
        this.toastr.error(e?.error?.message || 'Payment error. Try again.', 'Error');
      }
    });
  }
}
