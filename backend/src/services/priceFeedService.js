import WebSocket from 'ws';
import { config } from '../config.js';
import { authenticate, getToken } from './authService.js';

const prices = new Map();
let connectionState = 'Disconnected';
let lastError = null;
let ws = null;
let reconnectTimer = null;
let reconnectDelay = config.wsReconnectInitialMs;
let io = null;
let messageCount = 0;

export function setSocketIo(socketIo) {
  io = socketIo;
}

export function getConnectionState() {
  return { state: connectionState, lastError, messageCount };
}

export function getLatestPrices() {
  return Array.from(prices.values()).sort((a, b) => a.symbol.localeCompare(b.symbol));
}

export function getPrice(symbol) {
  return prices.get(symbol?.toUpperCase()) || null;
}

export async function startPriceFeed() {
  if (ws && (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING)) {
    return;
  }
  if (connectionState === 'Connecting') return;
  await connect();
}

export async function stopPriceFeed() {
  if (reconnectTimer) clearTimeout(reconnectTimer);
  if (ws) {
    ws.removeAllListeners();
    ws.close();
    ws = null;
  }
  connectionState = 'Disconnected';
  broadcastStatus();
}

async function connect() {
  if (reconnectTimer) {
    clearTimeout(reconnectTimer);
    reconnectTimer = null;
  }

  connectionState = 'Connecting';
  lastError = null;
  broadcastStatus();

  try {
    let token = getToken();
    if (!token) token = await authenticate();
    const url = `${config.wsBaseUrl}?token=${encodeURIComponent(token)}`;
    ws = new WebSocket(url);

    ws.on('open', () => {
      connectionState = 'Connected';
      reconnectDelay = config.wsReconnectInitialMs;
      lastError = null;
      console.log('[WS] Connected to price feed');
      broadcastStatus();
    });

    ws.on('message', (data) => {
      try {
        const raw = data.toString();
        const updates = parsePriceMessage(raw);
        for (const update of updates) {
          if (!update?.symbol || update.price == null) continue;
          const key = update.symbol.toUpperCase();
          const prev = prices.get(key);
          prices.set(key, {
            symbol: key,
            price: update.price,
            bid: update.bid ?? update.price,
            ask: update.ask ?? update.price,
            timestamp: update.timestamp || new Date().toISOString(),
            direction: prev ? (update.price > prev.price ? 'up' : update.price < prev.price ? 'down' : 'flat') : 'flat',
          });
        }
        messageCount += 1;
        if (io) io.emit('prices', getLatestPrices());
      } catch (err) {
        console.warn('[WS] Parse error:', err.message);
      }
    });

    ws.on('error', (err) => {
      lastError = err.message;
      console.error('[WS] Error:', err.message);
    });

    ws.on('close', () => {
      connectionState = 'Disconnected';
      ws = null;
      console.log('[WS] Disconnected, scheduling reconnect');
      broadcastStatus();
      scheduleReconnect();
    });
  } catch (err) {
    connectionState = 'Error';
    lastError = err.message;
    console.error('[WS] Connect failed:', err.message);
    broadcastStatus();
    scheduleReconnect();
  }
}

function scheduleReconnect() {
  if (reconnectTimer) return;
  reconnectTimer = setTimeout(async () => {
    reconnectTimer = null;
    reconnectDelay = Math.min(reconnectDelay * 2, config.wsReconnectMaxMs);
    try {
      await authenticate(true);
    } catch (e) {
      console.warn('[WS] Re-auth before reconnect failed:', e.message);
    }
    await connect();
  }, reconnectDelay);
}

function broadcastStatus() {
  if (io) {
    io.emit('status', { ...getConnectionState(), prices: getLatestPrices() });
  }
}

function parsePriceMessage(raw) {
  const results = [];
  let data;
  try {
    data = JSON.parse(raw);
  } catch {
    return results;
  }

  const items = Array.isArray(data) ? data : data.prices || data.data || data.ticks || [data];

  for (const item of items) {
    if (!item || typeof item !== 'object') continue;
    const symbol =
      item.symbol ||
      item.Symbol ||
      item.instrument ||
      item.Instrument ||
      item.s ||
      item.pair;
    const bid = num(item.bid ?? item.Bid ?? item.b);
    const ask = num(item.ask ?? item.Ask ?? item.a);
    const mid = num(item.price ?? item.Price ?? item.last ?? item.Last ?? item.mid ?? item.Mid);
    const price = mid ?? (bid != null && ask != null ? (bid + ask) / 2 : bid ?? ask);
    if (symbol && price != null) {
      results.push({
        symbol: String(symbol).toUpperCase(),
        price,
        bid,
        ask,
        timestamp: item.timestamp || item.time || item.Time,
      });
    }
  }
  return results;
}

function num(v) {
  if (v == null || v === '') return null;
  const n = Number(v);
  return Number.isFinite(n) ? n : null;
}
