import type { City, Metal, Purity } from "./rates";

export type DigestFrequencyType = "DailyAtTime" | "EveryNHours";

export interface NotificationRule {
  id: string;
  city: City;
  metal: Metal;
  purity: Purity;
  isActive: boolean;
  digestEnabled: boolean;
  digestFrequencyType: DigestFrequencyType;
  digestTimeOfDay: string | null;
  digestIntervalHours: number | null;
  digestLastSentAtUtc: string | null;
  thresholdEnabled: boolean;
  thresholdAbsoluteRupees: number | null;
  thresholdPercent: number | null;
  thresholdReferencePriceInrPerGram: number | null;
  thresholdReferenceSetAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface UpsertNotificationRuleRequest {
  city: City;
  metal: Metal;
  purity: Purity;
  isActive: boolean;
  digestEnabled: boolean;
  digestFrequencyType: DigestFrequencyType;
  digestTimeOfDay: string | null;
  digestIntervalHours: number | null;
  thresholdEnabled: boolean;
  thresholdAbsoluteRupees: number | null;
  thresholdPercent: number | null;
}

export const METAL_PURITY_OPTIONS: { label: string; metal: Metal; purity: Purity }[] = [
  { label: "Gold 24K", metal: "Gold", purity: "TwentyFourK" },
  { label: "Gold 22K", metal: "Gold", purity: "TwentyTwoK" },
  { label: "Silver", metal: "Silver", purity: "Pure" },
];
