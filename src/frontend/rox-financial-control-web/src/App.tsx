import { Activity, Banknote, ClipboardList, Gauge, RefreshCw, ShieldCheck, WalletCards } from "lucide-react";
import { useState } from "react";
import type { CashEntryOrigin } from "./api/types";
import { BalancePanel } from "./features/balances/BalancePanel";
import { BusinessValidationPanel } from "./features/businessValidation/BusinessValidationPanel";
import { CashEntryForm } from "./features/cashEntries/CashEntryForm";
import { CashEntryTable } from "./features/cashEntries/CashEntryTable";
import { LoadSimulationPanel } from "./features/loadSimulation/LoadSimulationPanel";
import { OutboxPanel } from "./features/operations/OutboxPanel";
import { originDescriptions, originLabels } from "./features/origins";
import { DailyBalanceReportButton } from "./features/reports/DailyBalanceReportButton";

const today = new Date().toISOString().slice(0, 10);
type AppTab = "entries" | "business-validation" | "load-test";

const originOptions = [
  { value: "Business", Icon: WalletCards },
  { value: "Validation", Icon: ShieldCheck },
  { value: "LoadSimulation", Icon: Gauge }
] satisfies Array<{ value: CashEntryOrigin; Icon: typeof WalletCards }>;

export function App() {
  const [from, setFrom] = useState(today);
  const [to, setTo] = useState(today);
  const [origin, setOrigin] = useState<CashEntryOrigin>("Business");
  const [refreshKey, setRefreshKey] = useState(0);
  const [activeTab, setActiveTab] = useState<AppTab>("entries");
  const selectedOriginLabel = originLabels[origin];
  const selectedOriginDescription = originDescriptions[origin];

  function refresh() {
    setRefreshKey((current) => current + 1);
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <div>
          <span className="eyebrow">ROX Technical Challenge</span>
          <h1>Controle de caixa</h1>
        </div>
        <button className="icon-button icon-button--primary" onClick={refresh} title="Atualizar">
          <RefreshCw size={18} />
        </button>
      </header>

      <nav className="tabs" aria-label="Navegação principal">
        <button
          className={activeTab === "entries" ? "tab tab--active" : "tab"}
          onClick={() => setActiveTab("entries")}
          type="button"
        >
          Lançamentos
        </button>
        <button
          className={activeTab === "load-test" ? "tab tab--active" : "tab"}
          onClick={() => setActiveTab("load-test")}
          type="button"
        >
          Teste de carga
        </button>
        <button
          className={activeTab === "business-validation" ? "tab tab--active" : "tab"}
          onClick={() => setActiveTab("business-validation")}
          type="button"
        >
          Validação funcional
        </button>
      </nav>

      {activeTab === "entries" ? (
        <>
          <section className="toolbar" aria-label="Filtros">
            <label>
              <span>Início</span>
              <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
            </label>
            <label>
              <span>Fim</span>
              <input type="date" value={to} onChange={(event) => setTo(event.target.value)} />
            </label>
            <fieldset className="origin-filter">
              <legend>Origem</legend>
              <div className="origin-filter__options">
                {originOptions.map(({ value, Icon }) => (
                  <button
                    aria-pressed={origin === value}
                    className={origin === value ? "origin-choice origin-choice--active" : "origin-choice"}
                    key={value}
                    onClick={() => setOrigin(value)}
                    title={originDescriptions[value]}
                    type="button"
                  >
                    <Icon size={16} />
                    <span>{originLabels[value]}</span>
                  </button>
                ))}
              </div>
            </fieldset>
            <DailyBalanceReportButton from={from} to={to} origin={origin} />
          </section>

          <section className="kpi-grid" aria-label="Resumo operacional">
            <BalancePanel from={from} to={to} origin={origin} refreshKey={refreshKey} />
            <OutboxPanel refreshKey={refreshKey} />
          </section>

          <section className="workspace-grid">
            <div className="panel panel--form">
              <div className="panel-heading">
                <Banknote size={20} />
                <h2>{origin === "Business" ? "Novo lançamento" : "Origem selecionada"}</h2>
              </div>
              {origin === "Business" ? (
                <CashEntryForm onCreated={refresh} defaultDate={today} />
              ) : (
                <div className="origin-summary">
                  <span>{selectedOriginDescription}</span>
                  <strong>{selectedOriginLabel}</strong>
                  <small>Consulta separada dos lançamentos reais.</small>
                </div>
              )}
            </div>

            <div className="panel panel--table">
              <div className="panel-heading">
                <ClipboardList size={20} />
                <h2>Lançamentos - {selectedOriginLabel}</h2>
              </div>
              <CashEntryTable from={from} to={to} origin={origin} refreshKey={refreshKey} />
            </div>
          </section>
        </>
      ) : activeTab === "load-test" ? (
        <LoadSimulationPanel defaultDate={today} onActivity={refresh} />
      ) : (
        <BusinessValidationPanel defaultDate={today} onActivity={refresh} />
      )}

      <footer className="footer-note">
        <Activity size={16} />
        <span>API, outbox, RabbitMQ worker e SQL Server preparados para execução local ou Docker.</span>
      </footer>
    </main>
  );
}
