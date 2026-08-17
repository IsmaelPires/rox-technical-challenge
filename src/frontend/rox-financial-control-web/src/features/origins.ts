import type { CashEntryOrigin } from "../api/types";

export const originLabels: Record<CashEntryOrigin, string> = {
  Business: "Reais",
  Validation: "Validação",
  LoadSimulation: "Teste de carga"
};

export const originDescriptions: Record<CashEntryOrigin, string> = {
  Business: "Operação",
  Validation: "Rotina funcional",
  LoadSimulation: "Simulação"
};
