import { getPrice } from './priceFeedService.js';
import { insertTrade, getTrades, nextTradeId } from '../db/database.js';

export function placeOrder({ symbol, side, quantity }) {
  const sym = String(symbol || '').trim().toUpperCase();
  const normalizedSide = normalizeSide(side);
  const qty = Number(quantity);

  if (!sym) return reject('Symbol is required');
  if (!normalizedSide) return reject('Side must be Buy or Sell');
  if (!Number.isFinite(qty) || qty <= 0) return reject('Quantity must be a positive number');

  const quote = getPrice(sym);
  if (!quote?.price) {
    return reject(`No live price available for ${sym}. Wait for feed or check symbol.`);
  }

  const trade = {
    TradeId: nextTradeId(),
    Symbol: sym,
    Side: normalizedSide,
    Quantity: qty,
    Price: quote.price,
    Timestamp: new Date().toISOString().replace('T', ' ').slice(0, 19),
    Status: 'Filled',
  };

  insertTrade(trade);
  return { success: true, trade };
}

function reject(message) {
  const trade = {
    TradeId: nextTradeId(),
    Symbol: '',
    Side: 'Buy',
    Quantity: 0,
    Price: 0,
    Timestamp: new Date().toISOString().replace('T', ' ').slice(0, 19),
    Status: 'Rejected',
  };
  return { success: false, message, trade: null };
}

function normalizeSide(side) {
  const s = String(side || '').trim().toLowerCase();
  if (s === 'buy') return 'Buy';
  if (s === 'sell') return 'Sell';
  return null;
}

export function listTrades(limit) {
  return getTrades(limit);
}

export function computePositions(trades, latestPrices) {
  const bySymbol = new Map();

  for (const t of [...trades].reverse()) {
    if (t.Status !== 'Filled') continue;
    const sym = t.Symbol;
    if (!bySymbol.has(sym)) {
      bySymbol.set(sym, { symbol: sym, netQty: 0, costBasis: 0 });
    }
    const p = bySymbol.get(sym);
    const sign = t.Side === 'Buy' ? 1 : -1;
    const delta = sign * t.Quantity;
    const newQty = p.netQty + delta;
    if (newQty === 0) {
      p.costBasis = 0;
    } else if (Math.sign(newQty) === Math.sign(p.netQty) || p.netQty === 0) {
      p.costBasis =
        (p.costBasis * Math.abs(p.netQty) + t.Price * t.Quantity) / Math.abs(newQty);
    }
    p.netQty = newQty;
  }

  const priceMap = new Map(latestPrices.map((x) => [x.symbol, x.price]));

  return Array.from(bySymbol.values())
    .filter((p) => Math.abs(p.netQty) > 1e-9)
    .map((p) => {
      const market = priceMap.get(p.symbol) ?? p.costBasis;
      const unrealizedPnL = (market - p.costBasis) * p.netQty;
      return {
        symbol: p.symbol,
        netQuantity: round(p.netQty, 4),
        averagePrice: round(p.costBasis, 5),
        marketPrice: round(market, 5),
        unrealizedPnL: round(unrealizedPnL, 2),
      };
    });
}

function round(n, d) {
  const f = 10 ** d;
  return Math.round(n * f) / f;
}
