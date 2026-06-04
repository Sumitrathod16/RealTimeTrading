-- SQLite schema for Real-Time Trading Platform
CREATE TABLE IF NOT EXISTS Trades (
    TradeId TEXT PRIMARY KEY,
    Symbol TEXT NOT NULL,
    Side TEXT NOT NULL CHECK (Side IN ('Buy', 'Sell')),
    Quantity REAL NOT NULL CHECK (Quantity > 0),
    Price REAL NOT NULL,
    Timestamp TEXT NOT NULL,
    Status TEXT NOT NULL CHECK (Status IN ('Filled', 'Rejected'))
);

CREATE INDEX IF NOT EXISTS IX_Trades_Timestamp ON Trades (Timestamp DESC);
CREATE INDEX IF NOT EXISTS IX_Trades_Symbol ON Trades (Symbol);
