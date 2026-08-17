import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ClipboardCheck, Play } from "lucide-react";
import { FormEvent, useState } from "react";
import { api } from "../../api/client";
import type { BusinessValidationPayload } from "../../api/types";
import { StatusBadge } from "../../components/StatusBadge";
import { formatCurrency, formatDateTime } from "../formatters";

type BusinessValidationPanelProps = {
  defaultDate: string;
  onActivity: () => void;
};

type FormState = {
  entriesCount: number;
  creditPercentage: number;
  creditAmount: number;
  debitAmount: number;
  businessDate: string;
  timeoutSeconds: number;
  includeInvalidCases: boolean;
};

export function BusinessValidationPanel({ defaultDate, onActivity }: BusinessValidationPanelProps) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState<FormState>({
    entriesCount: 8,
    creditPercentage: 50,
    creditAmount: 100,
    debitAmount: 40,
    businessDate: defaultDate,
    timeoutSeconds: 30,
    includeInvalidCases: true
  });

  const validationMutation = useMutation({
    mutationFn: api.runBusinessValidation,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["outbox"] });
      queryClient.invalidateQueries({ queryKey: ["cash-entries"] });
      queryClient.invalidateQueries({ queryKey: ["daily-balances"] });
      onActivity();
    }
  });

  function updateNumber(name: keyof FormState, value: string) {
    setForm((current) => ({
      ...current,
      [name]: Number(value)
    }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const payload: BusinessValidationPayload = {
      entriesCount: form.entriesCount,
      creditPercentage: form.creditPercentage,
      creditAmount: form.creditAmount,
      debitAmount: form.debitAmount,
      businessDate: form.businessDate || null,
      timeoutSeconds: form.timeoutSeconds,
      includeInvalidCases: form.includeInvalidCases
    };

    validationMutation.mutate(payload);
  }

  const result = validationMutation.data;
  const statusTone = validationMutation.error ? "danger" : result?.passed ? "good" : result ? "danger" : "neutral";
  const statusLabel = validationMutation.isPending
    ? "Executando"
    : validationMutation.error
      ? "Falha"
      : result?.passed
        ? "Aprovado"
        : result
          ? "Reprovado"
          : "Não executado";

  return (
    <section className="panel validation-panel">
      <div className="panel-heading panel-heading--between">
        <div className="panel-title">
          <ClipboardCheck size={20} />
          <h2>Validação funcional</h2>
        </div>
        <StatusBadge tone={statusTone}>{statusLabel}</StatusBadge>
      </div>

      <div className="load-grid">
        <form className="load-form validation-form" onSubmit={submit}>
          <label>
            <span>Lançamentos do cenário</span>
            <input
              min={1}
              max={100}
              type="number"
              value={form.entriesCount}
              onChange={(event) => updateNumber("entriesCount", event.target.value)}
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
            <span>Valor do crédito</span>
            <input
              min={1}
              step="0.01"
              type="number"
              value={form.creditAmount}
              onChange={(event) => updateNumber("creditAmount", event.target.value)}
            />
          </label>

          <label>
            <span>Valor do débito</span>
            <input
              min={1}
              step="0.01"
              type="number"
              value={form.debitAmount}
              onChange={(event) => updateNumber("debitAmount", event.target.value)}
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

          <label>
            <span>Timeout em segundos</span>
            <input
              min={5}
              max={120}
              type="number"
              value={form.timeoutSeconds}
              onChange={(event) => updateNumber("timeoutSeconds", event.target.value)}
            />
          </label>

          <label className="checkbox-field">
            <input
              checked={form.includeInvalidCases}
              type="checkbox"
              onChange={(event) =>
                setForm((current) => ({ ...current, includeInvalidCases: event.target.checked }))
              }
            />
            <span>Incluir cenários inválidos</span>
          </label>

          <div className="load-actions validation-actions">
            <button className="button button--primary" disabled={validationMutation.isPending} type="submit">
              <Play size={18} />
              {validationMutation.isPending ? "Executando" : "Executar validação"}
            </button>
          </div>

          {validationMutation.error ? <p className="form-error">{validationMutation.error.message}</p> : null}
        </form>

        <div className="load-status">
          <article className="load-stat">
            <span>Lançamentos</span>
            <strong>{result?.totals.createdEntries ?? 0}</strong>
          </article>
          <article className="load-stat">
            <span>Passos</span>
            <strong>{result?.steps.filter((step) => step.passed).length ?? 0}/{result?.steps.length ?? 0}</strong>
          </article>
          <article className="load-stat">
            <span>Saldo esperado</span>
            <strong>{formatCurrency(result?.totals.expectedBalance ?? 0)}</strong>
          </article>
          <article className="load-stat">
            <span>Saldo observado</span>
            <strong>{formatCurrency(result?.totals.observedBalance ?? 0)}</strong>
          </article>

          <div className="load-timeline">
            <span>Última execução</span>
            <strong>{result?.finishedAt ? formatDateTime(result.finishedAt) : "-"}</strong>
            <span>Identificador</span>
            <strong>{result?.runId ? result.runId.slice(0, 8) : "-"}</strong>
          </div>

          {result ? (
            <div className="validation-steps">
              {result.steps.map((step) => (
                <article className="validation-step" key={step.name}>
                  <div>
                    <strong>{step.name}</strong>
                    {step.details ? <small>{step.details}</small> : null}
                  </div>
                  <StatusBadge tone={step.passed ? "good" : "danger"}>
                    {step.passed ? "Aprovado" : "Falhou"}
                  </StatusBadge>
                  <small>Esperado: {step.expected}</small>
                  <small>Obtido: {step.actual}</small>
                </article>
              ))}
            </div>
          ) : (
            <div className="empty-state validation-empty">
              <strong>Nenhuma validação executada</strong>
              <span>Configure os parâmetros e execute a rotina automatizada.</span>
            </div>
          )}
        </div>
      </div>
    </section>
  );
}
