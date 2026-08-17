import type {
  BusinessValidationPayload,
  BusinessValidationResult,
  CashEntry,
  CashEntryOrigin,
  CashEntryType,
  CreateCashEntryPayload,
  DailyBalance,
  LoadSimulationStatus,
  OutboxStatus,
  PagedResult,
  StartLoadSimulationPayload
} from "./types";

const configuredApiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim();
const API_BASE_URL = configuredApiBaseUrl ? configuredApiBaseUrl.replace(/\/$/, "") : "";

type CashEntryFilters = {
  from?: string;
  to?: string;
  type?: CashEntryType | "";
  origin?: CashEntryOrigin;
  page?: number;
  pageSize?: number;
};

type BalanceFilters = {
  from?: string;
  to?: string;
  origin?: CashEntryOrigin;
};

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...init?.headers
    },
    ...init
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.detail ?? "Não foi possível concluir a requisição.");
  }

  return response.json() as Promise<T>;
}

function toQuery(params: Record<string, string | number | undefined>) {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") {
      query.set(key, String(value));
    }
  }

  const serialized = query.toString();
  return serialized ? `?${serialized}` : "";
}

export const api = {
  createCashEntry(payload: CreateCashEntryPayload) {
    return request<CashEntry>("/api/cash-entries", {
      method: "POST",
      body: JSON.stringify(payload)
    });
  },

  listCashEntries(filters: CashEntryFilters) {
    return request<PagedResult<CashEntry>>(
      `/api/cash-entries${toQuery({
        from: filters.from,
        to: filters.to,
        type: filters.type,
        origin: filters.origin ?? "Business",
        page: filters.page ?? 1,
        pageSize: filters.pageSize ?? 20
      })}`
    );
  },

  listDailyBalances(filters: BalanceFilters) {
    return request<DailyBalance[]>(
      `/api/daily-balances${toQuery({
        from: filters.from,
        to: filters.to,
        origin: filters.origin ?? "Business"
      })}`
    );
  },

  getOutboxStatus() {
    return request<OutboxStatus>("/api/operations/outbox");
  },

  getLoadSimulationStatus() {
    return request<LoadSimulationStatus>("/api/operations/load-simulation");
  },

  startLoadSimulation(payload: StartLoadSimulationPayload) {
    return request<LoadSimulationStatus>("/api/operations/load-simulation/start", {
      method: "POST",
      body: JSON.stringify(payload)
    });
  },

  stopLoadSimulation() {
    return request<LoadSimulationStatus>("/api/operations/load-simulation/stop", {
      method: "POST"
    });
  },

  runBusinessValidation(payload: BusinessValidationPayload) {
    return request<BusinessValidationResult>("/api/operations/business-validation/run", {
      method: "POST",
      body: JSON.stringify(payload)
    });
  }
};
