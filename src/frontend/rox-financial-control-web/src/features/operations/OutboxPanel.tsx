import { useQuery } from "@tanstack/react-query";
import { RadioTower } from "lucide-react";
import { api } from "../../api/client";
import { StatusBadge } from "../../components/StatusBadge";

type OutboxPanelProps = {
  refreshKey: number;
};

export function OutboxPanel({ refreshKey }: OutboxPanelProps) {
  const { data, isLoading, error } = useQuery({
    queryKey: ["outbox", refreshKey],
    queryFn: api.getOutboxStatus,
    refetchInterval: 3000
  });

  const tone = error ? "danger" : data?.totalPending ? "warning" : "good";

  return (
    <article className="metric-card metric-card--outbox">
      <RadioTower size={20} />
      <span>Outbox</span>
      <strong>{isLoading ? "..." : data?.totalPending ?? "-"}</strong>
      <StatusBadge tone={tone}>{error ? "Falha" : data?.totalPending ? "Pendente" : "Publicado"}</StatusBadge>
    </article>
  );
}
