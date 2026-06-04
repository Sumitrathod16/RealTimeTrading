export interface PriceQuote {
  symbol: string;
  price: number;
  bid?: number;
  ask?: number;
  timestamp?: string;
  direction?: 'up' | 'down' | 'flat';
}

export interface Trade {
  tradeId: string;
  symbol: string;
  side: 'Buy' | 'Sell';
  quantity: number;
  price: number;
  timestamp: string;
  status: 'Filled' | 'Rejected';
}

export interface Position {
  symbol: string;
  netQuantity: number;
  averagePrice: number;
  marketPrice: number;
  unrealizedPnL: number;
}

export interface HealthResponse {
  status: string;
  timestamp: string;
  auth: { ok: boolean; message: string };
  websocket: { state: string; lastError: string | null; messageCount: number };
}

export type ConnectionState = 'Connected' | 'Connecting' | 'Disconnected' | 'Error';
