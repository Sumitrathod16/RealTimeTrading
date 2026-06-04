import { useState } from 'react';
import { StatusBadge } from './components/StatusBadge';
import { PriceTable } from './components/PriceTable';
import { OrderPanel } from './components/OrderPanel';
import { TradeHistory } from './components/TradeHistory';
import { PositionSummary } from './components/PositionSummary';
import { useTradingSocket } from './hooks/useTradingSocket';

export default function App() {
  const {
    connectionState,
    prices,
    trades,
    positions,
    authOk,
    error,
    flashKeys,
    refreshTradesAndPositions,
    setError,
  } = useTradingSocket();

  const [selectedSymbol, setSelectedSymbol] = useState('');
  const [menuOpen, setMenuOpen] = useState(false);
  const [toast, setToast] = useState<{ text: string; type: 'success' | 'error' } | null>(null);

  function showToast(text: string, type: 'success' | 'error') {
    setToast({ text, type });
    setTimeout(() => setToast(null), 4000);
  }

  const effectiveSymbol =
    selectedSymbol || (prices.length ? prices[0].symbol : '');

  return (
    <div className="app">
      <header className="topbar">
        <button
          type="button"
          className="menu-btn"
          aria-label="Toggle menu"
          onClick={() => setMenuOpen((o) => !o)}
        >
          <span />
          <span />
          <span />
        </button>
        <div className="brand">
          <span className="brand-mark">◆</span>
          <h1>Real-Time Trading</h1>
        </div>
        <StatusBadge state={connectionState} authOk={authOk} />
      </header>

      {error && (
        <div className="banner banner-error" role="alert">
          {error}
        </div>
      )}

      {toast && (
        <div className={`toast toast-${toast.type}`} role="status">
          {toast.text}
        </div>
      )}

      <div className={`layout ${menuOpen ? 'menu-open' : ''}`}>
        <nav className="sidebar">
          <a href="#prices" onClick={() => setMenuOpen(false)}>
            Live Prices
          </a>
          <a href="#trade" onClick={() => setMenuOpen(false)}>
            Quick Trade
          </a>
          <a href="#history" onClick={() => setMenuOpen(false)}>
            Trade History
          </a>
          <a href="#positions" onClick={() => setMenuOpen(false)}>
            Positions
          </a>
        </nav>

        <main className="main">
          <section id="prices" className="card">
            <div className="card-header">
              <h2>Live Prices</h2>
              <span className="muted">{prices.length} symbols</span>
            </div>
            <PriceTable
              prices={prices}
              flashKeys={flashKeys}
              selectedSymbol={effectiveSymbol}
              onSelect={setSelectedSymbol}
            />
          </section>

          <section id="trade" className="card card-trade">
            <OrderPanel
              prices={prices}
              selectedSymbol={effectiveSymbol}
              onSymbolChange={setSelectedSymbol}
              onOrderPlaced={() => {
                refreshTradesAndPositions();
                setError(null);
              }}
              onMessage={showToast}
            />
          </section>

          <section id="history" className="card">
            <div className="card-header">
              <h2>Trade History</h2>
            </div>
            <TradeHistory trades={trades} />
          </section>

          <section id="positions" className="card">
            <div className="card-header">
              <h2>Position Summary</h2>
            </div>
            <PositionSummary positions={positions} />
          </section>
        </main>
      </div>
    </div>
  );
}
