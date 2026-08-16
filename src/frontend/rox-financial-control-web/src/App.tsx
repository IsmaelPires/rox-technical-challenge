import { Activity, Banknote, ClipboardList, RefreshCw } from "lucide-react";
import { useState } from "react";
import { BalancePanel } from "./features/balances/BalancePanel";
import { CashEntryForm } from "./features/cashEntries/CashEntryForm";
import { CashEntryTable } from "./features/cashEntries/CashEntryTable";
import { OutboxPanel } from "./features/operations/OutboxPanel";
import { DailyBalanceReportButton } from "./features/reports/DailyBalanceReportButton";

const today = new Date().toISOString().slice(0, 10);

export function App() {
  const [from, setFrom] = useState(today);
  const [to, setTo] = useState(today);
  const [refreshKey, setRefreshKey] = useState(0);

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

      <section className="toolbar" aria-label="Filtros">
        <label>
          <span>Início</span>
          <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
        </label>
        <label>
          <span>Fim</span>
          <input type="date" value={to} onChange={(event) => setTo(event.target.value)} />
        </label>
        <DailyBalanceReportButton from={from} to={to} />
      </section>

      <section className="kpi-grid" aria-label="Resumo operacional">
        <BalancePanel from={from} to={to} refreshKey={refreshKey} />
        <OutboxPanel refreshKey={refreshKey} />
      </section>

      <section className="workspace-grid">
        <div className="panel panel--form">
          <div className="panel-heading">
            <Banknote size={20} />
            <h2>Novo lançamento</h2>
          </div>
          <CashEntryForm onCreated={refresh} defaultDate={today} />
        </div>

        <div className="panel panel--table">
          <div className="panel-heading">
            <ClipboardList size={20} />
            <h2>Lançamentos</h2>
          </div>
          <CashEntryTable from={from} to={to} refreshKey={refreshKey} />
        </div>
      </section>

      <footer className="footer-note">
        <Activity size={16} />
        <span>API, outbox, RabbitMQ worker e SQL Server preparados para execução local ou Docker.</span>
      </footer>
    </main>
  );
}
