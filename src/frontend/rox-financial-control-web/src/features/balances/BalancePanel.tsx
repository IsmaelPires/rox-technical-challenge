import { useQuery } from "@tanstack/react-query";
import { CircleDollarSign, TrendingDown, TrendingUp } from "lucide-react";
import { api } from "../../api/client";
import type { CashEntryOrigin } from "../../api/types";
import { EmptyState } from "../../components/EmptyState";
import { formatCurrency } from "../formatters";

type BalancePanelProps = {
  from: string;
  to: string;
  origin: CashEntryOrigin;
  refreshKey: number;
};

export function BalancePanel({ from, to, origin, refreshKey }: BalancePanelProps) {
  const { data, isLoading, error } = useQuery({
    queryKey: ["daily-balances", from, to, origin, refreshKey],
    queryFn: () => api.listDailyBalances({ from, to, origin }),
    refetchInterval: 5000
  });

  if (isLoading) {
    return <EmptyState title="Saldos" description="Atualizando consolidado." />;
  }

  if (error) {
    return <EmptyState title="Saldos indisponíveis" description={error.message} />;
  }

  const totals = (data ?? []).reduce(
    (acc, item) => ({
      credits: acc.credits + item.totalCredits,
      debits: acc.debits + item.totalDebits,
      balance: acc.balance + item.balance,
      entries: acc.entries + item.entriesCount
    }),
    { credits: 0, debits: 0, balance: 0, entries: 0 }
  );

  return (
    <>
      <article className="metric-card">
        <CircleDollarSign size={20} />
        <span>Saldo</span>
        <strong>{formatCurrency(totals.balance)}</strong>
        <small>{totals.entries} lançamentos consolidados</small>
      </article>
      <article className="metric-card">
        <TrendingUp size={20} />
        <span>Créditos</span>
        <strong>{formatCurrency(totals.credits)}</strong>
        <small>{data?.length ?? 0} dias no período</small>
      </article>
      <article className="metric-card">
        <TrendingDown size={20} />
        <span>Débitos</span>
        <strong>{formatCurrency(totals.debits)}</strong>
        <small>Atualizado via worker</small>
      </article>
    </>
  );
}
