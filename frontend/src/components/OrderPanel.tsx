import { useState } from 'react';
import type { PriceQuote } from '../types';
import { placeOrder } from '../api/client';

interface Props {
  prices: PriceQuote[];
  selectedSymbol: string;
  onSymbolChange: (s: string) => void;
  onOrderPlaced: () => void;
  onMessage: (msg: string, type: 'success' | 'error') => void;
}

export function OrderPanel({
  prices,
  selectedSymbol,
  onSymbolChange,
  onOrderPlaced,
  onMessage,
}: Props) {
  const [side, setSide] = useState<'Buy' | 'Sell'>('Buy');
  const [quantity, setQuantity] = useState('1');
  const [submitting, setSubmitting] = useState(false);

  const quote = prices.find((p) => p.symbol === selectedSymbol);
  const symbols = prices.map((p) => p.symbol);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const qty = parseFloat(quantity);
    if (!selectedSymbol) {
      onMessage('Select a symbol', 'error');
      return;
    }
    if (!Number.isFinite(qty) || qty <= 0) {
      onMessage('Enter a valid quantity', 'error');
      return;
    }
    setSubmitting(true);
    try {
      const result = await placeOrder({ symbol: selectedSymbol, side, quantity: qty });
      if (result.success) {
        onMessage(`Order filled: ${side} ${qty} ${selectedSymbol} @ ${result.trade?.price}`, 'success');
        onOrderPlaced();
      } else {
        onMessage(result.message || 'Order rejected', 'error');
      }
    } catch {
      onMessage('Failed to submit order', 'error');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="order-panel" onSubmit={handleSubmit}>
      <h2>Quick Trade</h2>
      <label>
        Symbol
        <select
          value={selectedSymbol}
          onChange={(e) => onSymbolChange(e.target.value)}
        >
          <option value="">— Select —</option>
          {symbols.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </label>
      <div className="side-toggle">
        <button
          type="button"
          className={side === 'Buy' ? 'active buy' : ''}
          onClick={() => setSide('Buy')}
        >
          Buy
        </button>
        <button
          type="button"
          className={side === 'Sell' ? 'active sell' : ''}
          onClick={() => setSide('Sell')}
        >
          Sell
        </button>
      </div>
      <label>
        Quantity
        <input
          type="number"
          min="0.01"
          step="0.01"
          value={quantity}
          onChange={(e) => setQuantity(e.target.value)}
        />
      </label>
      <div className="market-price">
        <span>Market price</span>
        <strong className="mono">
          {quote ? (quote.price >= 10 ? quote.price.toFixed(2) : quote.price.toFixed(5)) : '—'}
        </strong>
      </div>
      <button type="submit" className={`submit ${side.toLowerCase()}`} disabled={submitting}>
        {submitting ? 'Submitting…' : `${side} ${selectedSymbol || ''}`}
      </button>
    </form>
  );
}
