import type { Trade } from '../types';

export function TradeHistory({ trades }: { trades: Trade[] }) {
  if (!trades.length) {
    return <p className="muted empty-inline">No trades yet.</p>;
  }

  return (
    <div className="table-scroll">
      <table className="data-table compact">
        <thead>
          <tr>
            <th>ID</th>
            <th>Symbol</th>
            <th>Side</th>
            <th>Qty</th>
            <th>Price</th>
            <th>Time</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {trades.map((t) => (
            <tr key={t.tradeId}>
              <td className="mono">{t.tradeId}</td>
              <td>{t.symbol}</td>
              <td className={t.side === 'Buy' ? 'buy-text' : 'sell-text'}>{t.side}</td>
              <td className="mono">{t.quantity}</td>
              <td className="mono">{t.price}</td>
              <td>{t.timestamp}</td>
              <td>
                <span className={`pill ${t.status.toLowerCase()}`}>{t.status}</span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
