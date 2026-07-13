import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { API_BASE_URL } from "../api/client";
import { getCurrentRates } from "../api/rates";
import type { CurrentRatesView } from "../types/rates";

export type ConnectionStatus = "connecting" | "connected" | "reconnecting" | "disconnected";

/**
 * Fetches the current rates once for first paint, then subscribes to the RatesHub for live
 * updates. Falls back to showing the initial REST snapshot if the hub connection is still
 * establishing or drops (auto-reconnect handles transient network blips).
 */
export function useRatesConnection() {
  const [rates, setRates] = useState<CurrentRatesView | null>(null);
  const [status, setStatus] = useState<ConnectionStatus>("connecting");
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    let cancelled = false;

    getCurrentRates()
      .then((initial) => {
        if (!cancelled) setRates(initial);
      })
      .catch(() => {
        // The hub connection below will populate rates once it connects; ignore first-paint failure.
      });

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/rates`)
      .withAutomaticReconnect()
      .build();

    connection.on("RatesUpdated", (payload: CurrentRatesView) => {
      setRates(payload);
    });

    connection.onreconnecting(() => setStatus("reconnecting"));
    connection.onreconnected(() => setStatus("connected"));
    connection.onclose(() => setStatus("disconnected"));

    connection
      .start()
      .then(() => setStatus("connected"))
      .catch(() => setStatus("disconnected"));

    connectionRef.current = connection;

    return () => {
      cancelled = true;
      connection.stop();
    };
  }, []);

  return { rates, status };
}
