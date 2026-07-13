import { useEffect, useState } from "react";
import { useRatesConnection } from "../hooks/useRatesConnection";
import { RateCard } from "../components/RateCard";
import { ConnectionStatusBadge } from "../components/ConnectionStatusBadge";
import { CitySelector } from "../components/CitySelector";
import { HistoryChart } from "../components/HistoryChart";
import { type City } from "../types/rates";

const CITY_STORAGE_KEY = "indiaGoldRates.selectedCity";

export function DashboardPage() {
  const { rates, status } = useRatesConnection();
  const [selectedCity, setSelectedCity] = useState<City>(
    () => (localStorage.getItem(CITY_STORAGE_KEY) as City) || "Hyderabad",
  );

  useEffect(() => {
    localStorage.setItem(CITY_STORAGE_KEY, selectedCity);
  }, [selectedCity]);

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <h1>Gold &amp; Silver Rates — {selectedCity}</h1>
        <div className="dashboard-header-controls">
          <CitySelector selectedCity={selectedCity} onChange={setSelectedCity} />
          <ConnectionStatusBadge status={status} />
        </div>
      </header>

      <p className="disclaimer">
        Rates reflect the global spot price converted to INR and are currently{" "}
        <strong>identical across all cities</strong> — a real per-city retail feed (with local
        jeweller premiums) may be added in a future update.
        {rates?.isStale && " The feed is temporarily stale; showing the last known value."}
      </p>

      <div className="rate-cards">
        <RateCard label="Gold 24K" priceInrPerGram={rates?.goldTwentyFourK.priceInrPerGram} />
        <RateCard label="Gold 22K" priceInrPerGram={rates?.goldTwentyTwoK.priceInrPerGram} />
        <RateCard label="Silver" priceInrPerGram={rates?.silver.priceInrPerGram} />
      </div>

      {rates && (
        <p className="last-updated">
          Last updated: {new Date(rates.sourceUpdatedAtUtc).toLocaleString("en-IN")}
        </p>
      )}

      <HistoryChart />
    </div>
  );
}
