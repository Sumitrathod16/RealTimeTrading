import type { PriceQuote } from '../types';

interface Props {
  prices: PriceQuote[];
  flashKeys: Record<string, 'up' | 'down'>;
  selectedSymbol: string;
  onSelect: (symbol: string) => void;
}

export function PriceTable({ prices, flashKeys, selectedSymbol, onSelect }: Props) {
  if (!prices.length) {
    return (
      <div className="empty-state">
        <p>Waiting for live prices…</p>
        <p className="muted">Ensure backend is connected to the WebSocket feed.</p>
      </div>
    );
  }

  return (
    <div className="table-scroll">
      <table className="data-table">
        <thead>
          <tr>
            <th>Symbol</th>
            <th>Bid</th>
            <th>Ask</th>
            <th>Mid</th>
          </tr>
        </thead>
        <tbody>
          {prices.map((q) => (
            <tr
              key={q.symbol}
              className={[
                selectedSymbol === q.symbol ? 'selected' : '',
                flashKeys[q.symbol] ? `flash-${flashKeys[q.symbol]}` : '',
              ]
                .filter(Boolean)
                .join(' ')}
              onClick={() => onSelect(q.symbol)}
            >
              <td className="symbol-cell">{q.symbol}</td>
              <td className="mono">{format(q.bid ?? q.price)}</td>
              <td className="mono">{format(q.ask ?? q.price)}</td>
              <td className="mono price-mid">{format(q.price)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function format(n: number) {
  return n >= 10 ? n.toFixed(2) : n.toFixed(5);
}
