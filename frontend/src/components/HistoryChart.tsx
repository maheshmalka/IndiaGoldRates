import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { getRateHistory } from "../api/rates";
import type { Metal, Purity } from "../types/rates";

const SERIES_OPTIONS: { label: string; metal: Metal; purity: Purity }[] = [
  { label: "Gold 24K", metal: "Gold", purity: "TwentyFourK" },
  { label: "Gold 22K", metal: "Gold", purity: "TwentyTwoK" },
  { label: "Silver", metal: "Silver", purity: "Pure" },
];

const RANGE_OPTIONS: { label: string; rangeHours: number }[] = [
  { label: "24h", rangeHours: 24 },
  { label: "7d", rangeHours: 24 * 7 },
  { label: "30d", rangeHours: 24 * 30 },
];

export function HistoryChart() {
  const [seriesIndex, setSeriesIndex] = useState(0);
  const [rangeHours, setRangeHours] = useState(24);
  const series = SERIES_OPTIONS[seriesIndex];

  const { data, isLoading, isError } = useQuery({
    queryKey: ["rate-history", series.metal, series.purity, rangeHours],
    queryFn: () => getRateHistory(series.metal, series.purity, rangeHours),
    refetchInterval: 60_000,
  });

  return (
    <div className="history-chart">
      <div className="history-chart-controls">
        <div className="button-group">
          {SERIES_OPTIONS.map((option, index) => (
            <button
              key={option.label}
              className={index === seriesIndex ? "active" : ""}
              onClick={() => setSeriesIndex(index)}
            >
              {option.label}
            </button>
          ))}
        </div>
        <div className="button-group">
          {RANGE_OPTIONS.map((option) => (
            <button
              key={option.label}
              className={option.rangeHours === rangeHours ? "active" : ""}
              onClick={() => setRangeHours(option.rangeHours)}
            >
              {option.label}
            </button>
          ))}
        </div>
      </div>

      {isLoading && <p className="history-chart-status">Loading history…</p>}
      {isError && <p className="history-chart-status">Couldn't load history.</p>}
      {data && data.length === 0 && (
        <p className="history-chart-status">No history yet for this range.</p>
      )}

      {data && data.length > 0 && (
        <ResponsiveContainer width="100%" height={280}>
          <LineChart data={data}>
            <CartesianGrid strokeDasharray="3 3" opacity={0.2} />
            <XAxis
              dataKey="bucketStartUtc"
              tickFormatter={(value: string) =>
                new Date(value).toLocaleString("en-IN", {
                  hour: "2-digit",
                  minute: "2-digit",
                  ...(rangeHours > 24 ? { day: "2-digit", month: "short" } : {}),
                })
              }
              minTickGap={40}
            />
            <YAxis domain={["auto", "auto"]} width={70} />
            <Tooltip
              labelFormatter={(value) => (value ? new Date(value as string).toLocaleString("en-IN") : "")}
              formatter={(value) => [`₹${Number(value).toFixed(2)}`, "Price/gram"]}
            />
            <Line type="monotone" dataKey="priceInrPerGram" stroke="#c9a227" dot={false} strokeWidth={2} />
          </LineChart>
        </ResponsiveContainer>
      )}
    </div>
  );
}
