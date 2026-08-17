export type CashEntryType = "Credit" | "Debit";
export type CashEntryOrigin = "Business" | "Validation" | "LoadSimulation";

export type CashEntry = {
  id: string;
  businessDate: string;
  type: CashEntryType;
  origin: CashEntryOrigin;
  amount: number;
  description: string;
  occurredAt: string;
  registeredAt: string;
};

export type DailyBalance = {
  businessDate: string;
  origin: CashEntryOrigin;
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
  origin?: CashEntryOrigin | null;
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

export type LoadSimulationConfiguration = {
  requestsPerBatch: number;
  intervalSeconds: number;
  maxBatches: number | null;
  creditPercentage: number;
  minAmount: number;
  maxAmount: number;
  businessDate: string | null;
};

export type StartLoadSimulationPayload = LoadSimulationConfiguration;

export type LoadSimulationStatus = {
  isRunning: boolean;
  isBatchRunning: boolean;
  configuration: LoadSimulationConfiguration | null;
  startedAt: string | null;
  stoppedAt: string | null;
  lastRunAt: string | null;
  nextRunAt: string | null;
  batchesExecuted: number;
  totalRequested: number;
  totalSucceeded: number;
  totalFailed: number;
  lastErrors: Array<{
    occurredAt: string;
    statusCode: number;
    message: string;
  }>;
};

export type BusinessValidationPayload = {
  entriesCount: number;
  creditPercentage: number;
  creditAmount: number;
  debitAmount: number;
  businessDate: string | null;
  timeoutSeconds: number;
  includeInvalidCases: boolean;
};

export type BusinessValidationStep = {
  name: string;
  passed: boolean;
  expected: string;
  actual: string;
  details: string | null;
};

export type BusinessValidationResult = {
  runId: string;
  passed: boolean;
  startedAt: string;
  finishedAt: string;
  configuration: BusinessValidationPayload & {
    businessDate: string;
  };
  totals: {
    createdEntries: number;
    expectedCredits: number;
    expectedDebits: number;
    expectedBalance: number;
    observedCredits: number;
    observedDebits: number;
    observedBalance: number;
    observedEntriesCount: number;
  };
  steps: BusinessValidationStep[];
};
