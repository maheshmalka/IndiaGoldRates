export interface MetalRateView {
  priceInrPerGram: number;
}

export interface CurrentRatesView {
  goldTwentyTwoK: MetalRateView;
  goldTwentyFourK: MetalRateView;
  silver: MetalRateView;
  isStale: boolean;
  sourceUpdatedAtUtc: string;
}

export type Metal = "Gold" | "Silver";
export type Purity = "TwentyTwoK" | "TwentyFourK" | "Pure";

export interface HistoryPoint {
  bucketStartUtc: string;
  priceInrPerGram: number;
}

export type City = "Hyderabad" | "Bangalore" | "Mumbai" | "Delhi";

export const CITIES: City[] = ["Hyderabad", "Bangalore", "Mumbai", "Delhi"];
