export class WalletTransactionModel {
  transactionId: number = 0;
  type: string = '';          // Credit | Debit
  amount: number = 0;
  description: string | null = null;
  referenceId: number | null = null;
  createdAt: string = '';
}

export class WalletModel {
  walletId: number = 0;
  userId: number = 0;
  balance: number = 0;
  updatedAt: string = '';
  transactions: WalletTransactionModel[] = [];
}
