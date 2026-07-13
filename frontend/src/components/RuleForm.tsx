import { useState } from "react";
import { CITIES, type City } from "../types/rates";
import {
  METAL_PURITY_OPTIONS,
  type DigestFrequencyType,
  type NotificationRule,
  type UpsertNotificationRuleRequest,
} from "../types/notificationRules";

interface RuleFormProps {
  initial?: NotificationRule;
  onSubmit: (request: UpsertNotificationRuleRequest) => void;
  onCancel: () => void;
  isSaving: boolean;
}

function toRequest(rule: NotificationRule | undefined): UpsertNotificationRuleRequest {
  if (!rule) {
    return {
      city: "Hyderabad",
      metal: "Gold",
      purity: "TwentyFourK",
      isActive: true,
      digestEnabled: false,
      digestFrequencyType: "DailyAtTime",
      digestTimeOfDay: "09:00:00",
      digestIntervalHours: null,
      thresholdEnabled: true,
      thresholdAbsoluteRupees: 100,
      thresholdPercent: null,
    };
  }

  return {
    city: rule.city,
    metal: rule.metal,
    purity: rule.purity,
    isActive: rule.isActive,
    digestEnabled: rule.digestEnabled,
    digestFrequencyType: rule.digestFrequencyType,
    digestTimeOfDay: rule.digestTimeOfDay,
    digestIntervalHours: rule.digestIntervalHours,
    thresholdEnabled: rule.thresholdEnabled,
    thresholdAbsoluteRupees: rule.thresholdAbsoluteRupees,
    thresholdPercent: rule.thresholdPercent,
  };
}

export function RuleForm({ initial, onSubmit, onCancel, isSaving }: RuleFormProps) {
  const [form, setForm] = useState<UpsertNotificationRuleRequest>(() => toRequest(initial));

  const selectedSeriesLabel =
    METAL_PURITY_OPTIONS.find((o) => o.metal === form.metal && o.purity === form.purity)?.label ??
    METAL_PURITY_OPTIONS[0].label;

  const handleSeriesChange = (label: string) => {
    const option = METAL_PURITY_OPTIONS.find((o) => o.label === label)!;
    setForm((f) => ({ ...f, metal: option.metal, purity: option.purity }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit(form);
  };

  return (
    <form className="rule-form" onSubmit={handleSubmit}>
      <div className="rule-form-row">
        <label>
          City
          <select value={form.city} onChange={(e) => setForm((f) => ({ ...f, city: e.target.value as City }))}>
            {CITIES.map((city) => (
              <option key={city} value={city}>
                {city}
              </option>
            ))}
          </select>
        </label>

        <label>
          Metal
          <select value={selectedSeriesLabel} onChange={(e) => handleSeriesChange(e.target.value)}>
            {METAL_PURITY_OPTIONS.map((o) => (
              <option key={o.label} value={o.label}>
                {o.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      <fieldset className="rule-form-section">
        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={form.digestEnabled}
            onChange={(e) => setForm((f) => ({ ...f, digestEnabled: e.target.checked }))}
          />
          Scheduled digest
        </label>

        {form.digestEnabled && (
          <div className="rule-form-row">
            <label>
              Frequency
              <select
                value={form.digestFrequencyType}
                onChange={(e) =>
                  setForm((f) => ({ ...f, digestFrequencyType: e.target.value as DigestFrequencyType }))
                }
              >
                <option value="DailyAtTime">Daily at a set time</option>
                <option value="EveryNHours">Every N hours</option>
              </select>
            </label>

            {form.digestFrequencyType === "DailyAtTime" ? (
              <label>
                Time (IST)
                <input
                  type="time"
                  value={form.digestTimeOfDay?.slice(0, 5) ?? "09:00"}
                  onChange={(e) => setForm((f) => ({ ...f, digestTimeOfDay: `${e.target.value}:00` }))}
                />
              </label>
            ) : (
              <label>
                Every
                <input
                  type="number"
                  min={1}
                  max={24}
                  value={form.digestIntervalHours ?? 6}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, digestIntervalHours: Number(e.target.value) }))
                  }
                />
                hours
              </label>
            )}
          </div>
        )}
      </fieldset>

      <fieldset className="rule-form-section">
        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={form.thresholdEnabled}
            onChange={(e) => setForm((f) => ({ ...f, thresholdEnabled: e.target.checked }))}
          />
          Threshold alert
        </label>

        {form.thresholdEnabled && (
          <div className="rule-form-row">
            <label>
              Move by ₹
              <input
                type="number"
                min={0}
                step="0.01"
                value={form.thresholdAbsoluteRupees ?? ""}
                onChange={(e) =>
                  setForm((f) => ({
                    ...f,
                    thresholdAbsoluteRupees: e.target.value === "" ? null : Number(e.target.value),
                  }))
                }
                placeholder="e.g. 100"
              />
            </label>
            <span className="rule-form-or">or</span>
            <label>
              Move by %
              <input
                type="number"
                min={0}
                step="0.1"
                value={form.thresholdPercent ?? ""}
                onChange={(e) =>
                  setForm((f) => ({
                    ...f,
                    thresholdPercent: e.target.value === "" ? null : Number(e.target.value),
                  }))
                }
                placeholder="e.g. 2"
              />
            </label>
          </div>
        )}
      </fieldset>

      <label className="checkbox-label">
        <input
          type="checkbox"
          checked={form.isActive}
          onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
        />
        Active
      </label>

      <div className="rule-form-actions">
        <button type="button" onClick={onCancel} disabled={isSaving}>
          Cancel
        </button>
        <button type="submit" className="primary" disabled={isSaving}>
          {isSaving ? "Saving…" : "Save alert"}
        </button>
      </div>
    </form>
  );
}
