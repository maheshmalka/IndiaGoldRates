import type { NotificationRule } from "../types/notificationRules";
import { METAL_PURITY_OPTIONS } from "../types/notificationRules";

interface RuleListProps {
  rules: NotificationRule[];
  onEdit: (rule: NotificationRule) => void;
  onDelete: (rule: NotificationRule) => void;
}

function seriesLabel(rule: NotificationRule): string {
  return (
    METAL_PURITY_OPTIONS.find((o) => o.metal === rule.metal && o.purity === rule.purity)?.label ??
    `${rule.metal} ${rule.purity}`
  );
}

function describeDigest(rule: NotificationRule): string | null {
  if (!rule.digestEnabled) return null;
  if (rule.digestFrequencyType === "DailyAtTime") {
    return `Daily digest at ${rule.digestTimeOfDay?.slice(0, 5) ?? "—"} IST`;
  }
  return `Digest every ${rule.digestIntervalHours}h`;
}

function describeThreshold(rule: NotificationRule): string | null {
  if (!rule.thresholdEnabled) return null;
  const parts: string[] = [];
  if (rule.thresholdAbsoluteRupees) parts.push(`₹${rule.thresholdAbsoluteRupees}`);
  if (rule.thresholdPercent) parts.push(`${rule.thresholdPercent}%`);
  return `Alert on move of ${parts.join(" or ")}`;
}

export function RuleList({ rules, onEdit, onDelete }: RuleListProps) {
  if (rules.length === 0) {
    return <p className="history-chart-status">No alerts configured yet.</p>;
  }

  return (
    <ul className="rule-list">
      {rules.map((rule) => (
        <li key={rule.id} className={`rule-list-item ${rule.isActive ? "" : "rule-list-item--inactive"}`}>
          <div>
            <div className="rule-list-title">
              {rule.city} — {seriesLabel(rule)}
              {!rule.isActive && <span className="rule-list-badge">Paused</span>}
            </div>
            <div className="rule-list-detail">
              {[describeDigest(rule), describeThreshold(rule)].filter(Boolean).join(" · ")}
            </div>
          </div>
          <div className="rule-list-actions">
            <button onClick={() => onEdit(rule)}>Edit</button>
            <button onClick={() => onDelete(rule)}>Delete</button>
          </div>
        </li>
      ))}
    </ul>
  );
}
