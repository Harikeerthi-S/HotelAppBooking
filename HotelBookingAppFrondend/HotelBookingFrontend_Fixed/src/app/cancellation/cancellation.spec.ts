import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Cancellation } from './cancellation';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';

describe('Cancellation', () => {
  let component: Cancellation;
  let fixture:   ComponentFixture<Cancellation>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Cancellation],
      providers: [provideHttpClient(), provideRouter([]), provideAnimations(), provideToastr()]
    }).compileComponents();
    fixture   = TestBed.createComponent(Cancellation);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('initial signals are empty / false', () => {
    expect(component.cancellations()).toEqual([]);
    expect(component.showForm()).toBeFalse();
    expect(component.showStatusModal()).toBeFalse();
    expect(component.filterStatus()).toBe('');
  });

  it('openForm / closeForm toggles showForm', () => {
    component.openForm();
    expect(component.showForm()).toBeTrue();
    component.closeForm();
    expect(component.showForm()).toBeFalse();
  });

  it('statusClass returns correct class', () => {
    expect(component.statusClass('Pending')).toBe('cs-pending');
    expect(component.statusClass('Approved')).toBe('cs-approved');
    expect(component.statusClass('Rejected')).toBe('cs-rejected');
  });

  it('statusIcon returns correct icon', () => {
    expect(component.statusIcon('Pending')).toBe('⏳');
    expect(component.statusIcon('Approved')).toBe('✅');
    expect(component.statusIcon('Rejected')).toBe('❌');
  });

  it('filtered() respects filterStatus signal', () => {
    (component as any).cancellations.set([
      { cancellationId: 1, bookingId: 1, reason: 'test', refundAmount: 0, status: 'Pending',  cancellationDate: '', hotelName: 'H1' },
      { cancellationId: 2, bookingId: 2, reason: 'test', refundAmount: 0, status: 'Approved', cancellationDate: '', hotelName: 'H2' },
    ]);
    component.filterStatus.set('Pending');
    expect(component.filtered().length).toBe(1);
    expect(component.filtered()[0].cancellationId).toBe(1);
  });

  it('totalPending and totalApproved computed correctly', () => {
    (component as any).cancellations.set([
      { cancellationId: 1, bookingId: 1, reason: '', refundAmount: 0, status: 'Pending',  cancellationDate: '', hotelName: '' },
      { cancellationId: 2, bookingId: 2, reason: '', refundAmount: 500, status: 'Approved', cancellationDate: '', hotelName: '' },
      { cancellationId: 3, bookingId: 3, reason: '', refundAmount: 0, status: 'Pending',  cancellationDate: '', hotelName: '' },
    ]);
    expect(component.totalPending()).toBe(2);
    expect(component.totalApproved()).toBe(1);
    expect(component.totalRefund()).toBe(500);
  });

  it('goPage ignores out-of-range values', () => {
    (component as any).totalPages.set(3);
    (component as any).page.set(2);
    component.goPage(0);
    expect(component.page()).toBe(2); // unchanged
    component.goPage(4);
    expect(component.page()).toBe(2); // unchanged
  });
});
