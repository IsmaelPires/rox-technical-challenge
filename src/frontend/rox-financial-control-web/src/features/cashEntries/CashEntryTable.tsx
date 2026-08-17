import { useQuery } from "@tanstack/react-query";
import { ArrowDownCircle, ArrowUpCircle } from "lucide-react";
import { api } from "../../api/client";
import type { CashEntryOrigin } from "../../api/types";
import { EmptyState } from "../../components/EmptyState";
import { StatusBadge } from "../../components/StatusBadge";
import { formatCurrency, formatDate, formatDateTime } from "../formatters";
import { originLabels } from "../origins";

type CashEntryTableProps = {
  from: string;
  to: string;
  origin: CashEntryOrigin;
  refreshKey: number;
};

const originBadgeTone: Record<CashEntryOrigin, "neutral" | "good" | "danger" | "warning"> = {
  Business: "neutral",
  Validation: "warning",
  LoadSimulation: "good"
};

export function CashEntryTable({ from, to, origin, refreshKey }: CashEntryTableProps) {
  const { data, isLoading, error } = useQuery({
    queryKey: ["cash-entries", from, to, origin, refreshKey],
    queryFn: () => api.listCashEntries({ from, to, origin, page: 1, pageSize: 25 })
  });

  if (isLoading) {
    return <EmptyState title="Carregando" description="Buscando lançamentos do período." />;
  }

  if (error) {
    return <EmptyState title="Falha na consulta" description={error.message} />;
  }

  if (!data || data.items.length === 0) {
    return <EmptyState title="Sem lançamentos" description="Nenhum registro encontrado para a origem selecionada." />;
  }

  return (
    <div className="table-shell">
      <table>
        <thead>
          <tr>
            <th>Data</th>
            <th>Tipo</th>
            <th>Origem</th>
            <th>Descrição</th>
            <th className="align-right">Valor</th>
            <th>Registro</th>
          </tr>
        </thead>
        <tbody>
          {data.items.map((entry) => (
            <tr key={entry.id}>
              <td>{formatDate(entry.businessDate)}</td>
              <td>
                <StatusBadge tone={entry.type === "Credit" ? "good" : "danger"}>
                  {entry.type === "Credit" ? <ArrowUpCircle size={14} /> : <ArrowDownCircle size={14} />}
                  {entry.type === "Credit" ? "Crédito" : "Débito"}
                </StatusBadge>
              </td>
              <td>
                <StatusBadge tone={originBadgeTone[entry.origin]}>{originLabels[entry.origin]}</StatusBadge>
              </td>
              <td>{entry.description}</td>
              <td className="align-right">{formatCurrency(entry.amount)}</td>
              <td>{formatDateTime(entry.registeredAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
