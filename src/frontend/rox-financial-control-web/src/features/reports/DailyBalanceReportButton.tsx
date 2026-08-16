import { Download } from "lucide-react";
import { useState } from "react";
import { api } from "../../api/client";
import type { DailyBalance } from "../../api/types";
import { formatDate, formatDateTime } from "../formatters";

type DailyBalanceReportButtonProps = {
  from: string;
  to: string;
};

const numberFormatter = new Intl.NumberFormat("pt-BR", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2
});

function csvCell(value: string | number) {
  const serialized = String(value).replace(/"/g, '""');
  return `"${serialized}"`;
}

function csvRow(values: Array<string | number>) {
  return values.map(csvCell).join(";");
}

function buildReportRows(from: string, to: string, balances: DailyBalance[]) {
  const orderedBalances = [...balances].sort((a, b) => a.businessDate.localeCompare(b.businessDate));
  const totals = orderedBalances.reduce(
    (acc, item) => ({
      credits: acc.credits + item.totalCredits,
      debits: acc.debits + item.totalDebits,
      balance: acc.balance + item.balance,
      entries: acc.entries + item.entriesCount
    }),
    { credits: 0, debits: 0, balance: 0, entries: 0 }
  );

  return [
    csvRow(["Relatório de saldo consolidado diário"]),
    csvRow(["Período", `${formatDate(from)} a ${formatDate(to)}`]),
    csvRow(["Gerado em", formatDateTime(new Date().toISOString())]),
    "",
    csvRow([
      "Data",
      "Créditos (R$)",
      "Débitos (R$)",
      "Saldo consolidado (R$)",
      "Lançamentos",
      "Última atualização"
    ]),
    ...orderedBalances.map((item) =>
      csvRow([
        formatDate(item.businessDate),
        numberFormatter.format(item.totalCredits),
        numberFormatter.format(item.totalDebits),
        numberFormatter.format(item.balance),
        item.entriesCount,
        formatDateTime(item.lastUpdatedAt)
      ])
    ),
    csvRow([
      "Total",
      numberFormatter.format(totals.credits),
      numberFormatter.format(totals.debits),
      numberFormatter.format(totals.balance),
      totals.entries,
      ""
    ])
  ];
}

function downloadCsv(filename: string, rows: string[]) {
  const csv = `\uFEFF${rows.join("\r\n")}`;
  const blob = new Blob([csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

export function DailyBalanceReportButton({ from, to }: DailyBalanceReportButtonProps) {
  const [isDownloading, setIsDownloading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleDownload() {
    setError(null);
    setIsDownloading(true);

    try {
      const balances = await api.listDailyBalances({ from, to });
      const rows = buildReportRows(from, to, balances);
      downloadCsv(`saldo-consolidado_${from}_${to}.csv`, rows);
    } catch (downloadError) {
      setError(downloadError instanceof Error ? downloadError.message : "Não foi possível gerar o relatório.");
    } finally {
      setIsDownloading(false);
    }
  }

  return (
    <div className="toolbar-action">
      <button className="button button--primary button--inline" onClick={handleDownload} disabled={isDownloading}>
        <Download size={18} />
        {isDownloading ? "Gerando..." : "Baixar consolidado"}
      </button>
      {error ? <small className="toolbar-error">{error}</small> : null}
    </div>
  );
}
