export type CashEntryType = "Credit" | "Debit";

export type CashEntry = {
  id: string;
  businessDate: string;
  type: CashEntryType;
  amount: number;
  description: string;
  occurredAt: string;
  registeredAt: string;
};

export type DailyBalance = {
  businessDate: string;
  totalCredits: number;
  totalDebits: number;
  balance: number;
  entriesCount: number;
  lastUpdatedAt: string;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type CreateCashEntryPayload = {
  businessDate: string;
  type: CashEntryType;
  amount: number;
  description: string;
  occurredAt?: string | null;
};

export type OutboxStatus = {
  totalPending: number;
  lastErrors: Array<{
    id: string;
    type: string;
    attempts: number;
    error: string | null;
    occurredAt: string;
  }>;
};
