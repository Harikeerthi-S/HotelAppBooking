export interface WalletModel {
  walletId:  number;
  userId:    number;
  balance:   number;
  updatedAt: string;
}

export interface WalletTransactionModel {
  transactionId: number;
  type:          string;   // 'Credit' | 'Debit'
  amount:        number;
  description:   string | null;
  referenceId:   number | null;
  createdAt:     string;
}
