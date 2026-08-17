import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Gauge, Play, Square } from "lucide-react";
import { FormEvent, useState } from "react";
import { api } from "../../api/client";
import type { StartLoadSimulationPayload } from "../../api/types";
import { StatusBadge } from "../../components/StatusBadge";
import { formatDateTime } from "../formatters";

type LoadSimulationPanelProps = {
  defaultDate: string;
  onActivity: () => void;
};

type FormState = {
  requestsPerBatch: number;
  intervalSeconds: number;
  maxBatches: number;
  creditPercentage: number;
  minAmount: number;
  maxAmount: number;
  businessDate: string;
};

export function LoadSimulationPanel({ defaultDate, onActivity }: LoadSimulationPanelProps) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState<FormState>({
    requestsPerBatch: 20,
    intervalSeconds: 60,
    maxBatches: 10,
    creditPercentage: 70,
    minAmount: 20,
    maxAmount: 600,
    businessDate: defaultDate
  });

  const { data, error, isLoading } = useQuery({
    queryKey: ["load-simulation"],
    queryFn: api.getLoadSimulationStatus,
    refetchInterval: 2000
  });

  function refreshOperationalQueries() {
    queryClient.invalidateQueries({ queryKey: ["load-simulation"] });
    queryClient.invalidateQueries({ queryKey: ["outbox"] });
    queryClient.invalidateQueries({ queryKey: ["cash-entries"] });
    queryClient.invalidateQueries({ queryKey: ["daily-balances"] });
    onActivity();
  }

  const startMutation = useMutation({
    mutationFn: api.startLoadSimulation,
    onSuccess: refreshOperationalQueries
  });

  const stopMutation = useMutation({
    mutationFn: api.stopLoadSimulation,
    onSuccess: refreshOperationalQueries
  });

  function updateNumber(name: keyof FormState, value: string) {
    setForm((current) => ({
      ...current,
      [name]: Number(value)
    }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const payload: StartLoadSimulationPayload = {
      requestsPerBatch: form.requestsPerBatch,
      intervalSeconds: form.intervalSeconds,
      maxBatches: form.maxBatches || null,
      creditPercentage: form.creditPercentage,
      minAmount: form.minAmount,
      maxAmount: form.maxAmount,
      businessDate: form.businessDate || null
    };

    startMutation.mutate(payload);
  }

  const statusTone = error ? "danger" : data?.isRunning ? "warning" : "good";
  const statusLabel = error ? "Falha" : data?.isRunning ? "Em execução" : "Parado";
  const mutationError = startMutation.error ?? stopMutation.error;

  return (
    <section className="panel load-panel">
      <div className="panel-heading panel-heading--between">
        <div className="panel-title">
          <Gauge size={20} />
          <h2>Teste de carga</h2>
        </div>
        <StatusBadge tone={statusTone}>{isLoading ? "Carregando" : statusLabel}</StatusBadge>
      </div>

      <div className="load-grid">
        <form className="load-form" onSubmit={submit}>
          <label>
            <span>Requisições por rodada</span>
            <input
              min={1}
              max={250}
              type="number"
              value={form.requestsPerBatch}
              onChange={(event) => updateNumber("requestsPerBatch", event.target.value)}
            />
          </label>

          <label>
            <span>Intervalo em segundos</span>
            <input
              min={5}
              max={3600}
              type="number"
              value={form.intervalSeconds}
              onChange={(event) => updateNumber("intervalSeconds", event.target.value)}
            />
          </label>

          <label>
            <span>Máximo de rodadas</span>
            <input
              min={1}
              max={1000}
              type="number"
              value={form.maxBatches}
              onChange={(event) => updateNumber("maxBatches", event.target.value)}
            />
          </label>

          <label>
            <span>Créditos (%)</span>
            <input
              min={0}
              max={100}
              type="number"
              value={form.creditPercentage}
              onChange={(event) => updateNumber("creditPercentage", event.target.value)}
            />
          </label>

          <label>
            <span>Valor mínimo</span>
            <input
              min={1}
              step="0.01"
              type="number"
              value={form.minAmount}
              onChange={(event) => updateNumber("minAmount", event.target.value)}
            />
          </label>

          <label>
            <span>Valor máximo</span>
            <input
              min={1}
              step="0.01"
              type="number"
              value={form.maxAmount}
              onChange={(event) => updateNumber("maxAmount", event.target.value)}
            />
          </label>

          <label>
            <span>Data de negócio</span>
            <input
              type="date"
              value={form.businessDate}
              onChange={(event) => setForm((current) => ({ ...current, businessDate: event.target.value }))}
            />
          </label>

          <div className="load-actions">
            <button className="button button--primary" disabled={startMutation.isPending} type="submit">
              <Play size={18} />
              {startMutation.isPending ? "Iniciando" : "Iniciar"}
            </button>
            <button
              className="button button--secondary"
              disabled={stopMutation.isPending || !data?.isRunning}
              onClick={() => stopMutation.mutate()}
              type="button"
            >
              <Square size={18} />
              {stopMutation.isPending ? "Parando" : "Parar"}
            </button>
          </div>

          {mutationError ? <p className="form-error">{mutationError.message}</p> : null}
          {error ? <p className="form-error">{error.message}</p> : null}
        </form>

        <div className="load-status">
          <article className="load-stat">
            <span>Rodadas</span>
            <strong>{data?.batchesExecuted ?? 0}</strong>
          </article>
          <article className="load-stat">
            <span>Requisições</span>
            <strong>{data?.totalRequested ?? 0}</strong>
          </article>
          <article className="load-stat">
            <span>Sucesso</span>
            <strong>{data?.totalSucceeded ?? 0}</strong>
          </article>
          <article className="load-stat">
            <span>Falhas</span>
            <strong>{data?.totalFailed ?? 0}</strong>
          </article>

          <div className="load-timeline">
            <span>Última rodada</span>
            <strong>{data?.lastRunAt ? formatDateTime(data.lastRunAt) : "-"}</strong>
            <span>Próxima rodada</span>
            <strong>{data?.nextRunAt ? formatDateTime(data.nextRunAt) : "-"}</strong>
          </div>

          {data?.lastErrors.length ? (
            <div className="load-errors">
              <strong>Últimos erros</strong>
              {data.lastErrors.map((item) => (
                <small key={`${item.occurredAt}-${item.message}`}>
                  {formatDateTime(item.occurredAt)} - {item.statusCode || "rede"} - {item.message}
                </small>
              ))}
            </div>
          ) : null}
        </div>
      </div>
    </section>
  );
}
