import type { ConnectionStatus } from "../hooks/useRatesConnection";

const LABELS: Record<ConnectionStatus, string> = {
  connecting: "Connecting…",
  connected: "Live",
  reconnecting: "Reconnecting…",
  disconnected: "Disconnected",
};

export function ConnectionStatusBadge({ status }: { status: ConnectionStatus }) {
  return <span className={`connection-badge connection-badge--${status}`}>{LABELS[status]}</span>;
}
