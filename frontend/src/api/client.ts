import type { HealthResponse, Position, PriceQuote, Trade } from '../types';

const API_BASE = (import.meta.env.VITE_API_BASE_URL as string) || '';
const API = `${API_BASE}/api`;

export async function fetchHealth(): Promise<HealthResponse> {
  const res = await fetch(`${API}/health`);
  if (!res.ok) throw new Error('Health check failed');
  return res.json();
}

export async function fetchPrices(): Promise<PriceQuote[]> {
  const res = await fetch(`${API}/prices`);
  if (!res.ok) throw new Error('Failed to load prices');
  return res.json();
}

export async function fetchTrades(): Promise<Trade[]> {
  const res = await fetch(`${API}/trades`);
  if (!res.ok) throw new Error('Failed to load trades');
  return res.json();
}

export async function fetchPositions(): Promise<Position[]> {
  const res = await fetch(`${API}/positions`);
  if (!res.ok) throw new Error('Failed to load positions');
  return res.json();
}

export async function placeOrder(body: {
  symbol: string;
  side: 'Buy' | 'Sell';
  quantity: number;
}): Promise<{ success: boolean; trade?: Trade; message?: string }> {
  const res = await fetch(`${API}/orders`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  const data = await res.json();
  if (!res.ok) return { success: false, message: data.message || 'Order failed' };
  return data;
}
