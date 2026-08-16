import type { ReactNode } from "react";

type StatusBadgeProps = {
  tone: "neutral" | "good" | "danger" | "warning";
  children: ReactNode;
};

export function StatusBadge({ tone, children }: StatusBadgeProps) {
  return <span className={`status-badge status-badge--${tone}`}>{children}</span>;
}
