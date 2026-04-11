export class CancellationModel {
  cancellationId: number = 0;
  bookingId: number = 0;
  reason: string = '';
  refundAmount: number = 0;
  refundPolicy: string = '';   // e.g. "≥ 5 days — 100% refund"
  refundPercent: number = 0;
  walletCredited: boolean = false;
  status: string = '';
  cancellationDate: string = '';
}
