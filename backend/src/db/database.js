import Database from 'better-sqlite3';
import fs from 'fs';
import path from 'path';
import { config } from '../config.js';

let db;

export function getDb() {
  if (!db) {
    const dir = path.dirname(config.dbPath);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    db = new Database(config.dbPath);
    db.pragma('journal_mode = WAL');
    initSchema();
  }
  return db;
}

function initSchema() {
  db.exec(`
    CREATE TABLE IF NOT EXISTS Trades (
      TradeId TEXT PRIMARY KEY,
      Symbol TEXT NOT NULL,
      Side TEXT NOT NULL,
      Quantity REAL NOT NULL,
      Price REAL NOT NULL,
      Timestamp TEXT NOT NULL,
      Status TEXT NOT NULL
    );
    CREATE INDEX IF NOT EXISTS IX_Trades_Timestamp ON Trades (Timestamp DESC);
  `);
}

let tradeCounter = 10000;

export function nextTradeId() {
  tradeCounter += 1;
  return `TRD${tradeCounter}`;
}

export function insertTrade(trade) {
  const stmt = getDb().prepare(`
    INSERT INTO Trades (TradeId, Symbol, Side, Quantity, Price, Timestamp, Status)
    VALUES (@TradeId, @Symbol, @Side, @Quantity, @Price, @Timestamp, @Status)
  `);
  stmt.run(trade);
  return trade;
}

export function getTrades(limit = 100) {
  return getDb()
    .prepare('SELECT * FROM Trades ORDER BY Timestamp DESC LIMIT ?')
    .all(limit);
}
