import { useCallback, useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import type { ConnectionState, PriceQuote } from '../types';
import { fetchHealth, fetchPrices, fetchTrades, fetchPositions } from '../api/client';
import type { Position, Trade } from '../types';

const THROTTLE_MS = 200;
const API_BASE = (import.meta.env.VITE_API_BASE_URL as string) || '';
const HUB_URL = `${API_BASE}/hubs/trading`;

export function useTradingSocket() {
  const [connectionState, setConnectionState] = useState<ConnectionState>('Connecting');
  const [prices, setPrices] = useState<PriceQuote[]>([]);
  const [trades, setTrades] = useState<Trade[]>([]);
  const [positions, setPositions] = useState<Position[]>([]);
  const [authOk, setAuthOk] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [flashKeys, setFlashKeys] = useState<Record<string, 'up' | 'down'>>({});
  const pendingPrices = useRef<PriceQuote[] | null>(null);
  const throttleTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const prevPrices = useRef<Map<string, number>>(new Map());

  const applyPrices = useCallback((incoming: PriceQuote[]) => {
    const flashes: Record<string, 'up' | 'down'> = {};
    for (const q of incoming) {
      const prev = prevPrices.current.get(q.symbol);
      if (prev != null && q.price !== prev) {
        flashes[q.symbol] = q.price > prev ? 'up' : 'down';
      }
      prevPrices.current.set(q.symbol, q.price);
    }
    setPrices(incoming);
    if (Object.keys(flashes).length) {
      setFlashKeys(flashes);
      setTimeout(() => setFlashKeys({}), 400);
    }
  }, []);

  const schedulePriceUpdate = useCallback(
    (incoming: PriceQuote[]) => {
      pendingPrices.current = incoming;
      if (throttleTimer.current) return;
      throttleTimer.current = setTimeout(() => {
        throttleTimer.current = null;
        if (pendingPrices.current) applyPrices(pendingPrices.current);
      }, THROTTLE_MS);
    },
    [applyPrices]
  );

  const refreshTradesAndPositions = useCallback(async () => {
    try {
      const [t, p] = await Promise.all([fetchTrades(), fetchPositions()]);
      setTrades(t);
      setPositions(p);
    } catch (e) {
      console.error(e);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function init() {
      try {
        const health = await fetchHealth();
        if (cancelled) return;
        setAuthOk(health.auth?.ok ?? false);
        const wsState = (health.websocket?.state || 'Disconnected') as ConnectionState;
        setConnectionState(wsState);
        if (!health.auth?.ok) setError(health.auth?.message || 'Auth not ready');
      } catch (e) {
        setConnectionState('Error');
        setError(e instanceof Error ? e.message : 'Backend unreachable');
      }

      try {
        const [p, t, pos] = await Promise.all([
          fetchPrices(),
          fetchTrades(),
          fetchPositions(),
        ]);
        if (!cancelled) {
          applyPrices(p);
          setTrades(t);
          setPositions(pos);
        }
      } catch {
        /* prices may be empty until feed connects */
      }
    }

    init();

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build();

    connection.on('PricesUpdated', (payload: PriceQuote[]) => {
      schedulePriceUpdate(payload);
    });

    connection.on(
      'StatusUpdated',
      (payload: { state: string; lastError?: string; prices?: PriceQuote[] }) => {
        setConnectionState((payload.state as ConnectionState) || 'Disconnected');
        if (payload.prices?.length) schedulePriceUpdate(payload.prices);
      }
    );

    connection
      .start()
      .catch((err) => console.warn('SignalR connect:', err.message));

    return () => {
      cancelled = true;
      connection.stop();
      if (throttleTimer.current) clearTimeout(throttleTimer.current);
    };
  }, [applyPrices, schedulePriceUpdate]);

  return {
    connectionState,
    prices,
    trades,
    positions,
    authOk,
    error,
    flashKeys,
    refreshTradesAndPositions,
    setError,
  };
}
