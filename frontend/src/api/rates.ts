import { apiGet } from "./client";
import type { CurrentRatesView, HistoryPoint, Metal, Purity } from "../types/rates";

export function getCurrentRates(): Promise<CurrentRatesView> {
  return apiGet<CurrentRatesView>("/api/rates/current");
}

export function getRateHistory(
  metal: Metal,
  purity: Purity,
  rangeHours: number,
): Promise<HistoryPoint[]> {
  const params = new URLSearchParams({
    metal,
    purity,
    rangeHours: String(rangeHours),
  });
  return apiGet<HistoryPoint[]>(`/api/rates/history?${params.toString()}`);
}
