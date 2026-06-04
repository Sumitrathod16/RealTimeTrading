import type { Position } from '../types';

export function PositionSummary({ positions }: { positions: Position[] }) {
  if (!positions.length) {
    return <p className="muted empty-inline">No open positions.</p>;
  }

  const totalPnL = positions.reduce((s, p) => s + p.unrealizedPnL, 0);

  return (
    <>
      <div className="pnl-total">
        <span>Unrealized P&amp;L</span>
        <strong className={totalPnL >= 0 ? 'buy-text' : 'sell-text'}>
          {totalPnL >= 0 ? '+' : ''}
          {totalPnL.toFixed(2)}
        </strong>
      </div>
      <div className="table-scroll">
        <table className="data-table compact">
          <thead>
            <tr>
              <th>Symbol</th>
              <th>Net Qty</th>
              <th>Avg</th>
              <th>Market</th>
              <th>P&amp;L</th>
            </tr>
          </thead>
          <tbody>
            {positions.map((p) => (
              <tr key={p.symbol}>
                <td>{p.symbol}</td>
                <td className="mono">{p.netQuantity}</td>
                <td className="mono">{p.averagePrice}</td>
                <td className="mono">{p.marketPrice}</td>
                <td className={`mono ${p.unrealizedPnL >= 0 ? 'buy-text' : 'sell-text'}`}>
                  {p.unrealizedPnL >= 0 ? '+' : ''}
                  {p.unrealizedPnL.toFixed(2)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
