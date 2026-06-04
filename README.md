# Real-Time Mini Trading Platform

Full-stack prototype: SysFX REST auth, WebSocket live prices, REST + SignalR to the UI, SQLite trade storage, no page reload.

## Architecture

```
┌─────────────┐   REST + SignalR    ┌──────────────────────────┐
│  React UI   │ ◄──────────────────►│  Trading.Api (ASP.NET 8) │
│  (Vite)     │   /api/*  /hubs/*   │  Web API + SignalR       │
└─────────────┘                     └────────────┬─────────────┘
                                               │
              ┌────────────────────────────────┼────────────────────┐
              ▼                                ▼                    ▼
      POST /auth/token                   WebSocket feed        SQLite
      (external REST)                  ws://...?token=...    data/trades.db
```

### Backend (§6.1) — `Trading.Api/`

| Requirement | Implementation |
|-------------|----------------|
| ASP.NET Core Web API, C# | .NET 8 project |
| REST authentication | `AuthService` |
| WebSocket price feed | `PriceFeedWorker` (`ClientWebSocket`) |
| Latest price per symbol | `PriceCache` (in-memory) |
| REST endpoints | API controllers |
| Trade storage | EF Core + SQLite |
| Reconnect / failures | Exponential backoff + structured logging |

Legacy Node.js API remains in `backend/` for reference; **use `Trading.Api` for the assignment**.

### Frontend

React + TypeScript — SignalR live updates (200ms throttle), responsive dashboard.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Node.js 18+ (frontend only)
- Network access to `s138.sysfx.com` (ports 10001, 10006)

## Setup

1. Copy credentials:

   ```powershell
   copy .env.example .env
   ```

2. Set environment variables (or `Trading.Api/appsettings.json` → `Auth` section):

   ```powershell
   $env:USER_ID="csfx2568033"
   $env:ACCOUNT_ID="21733"
   $env:PASSWORD="your_password"
   ```

3. **API** (terminal 1):

   ```powershell
   dotnet run --project Trading.Api
   ```

   - http://localhost:5000/swagger  
   - http://localhost:5000/api/health  

4. **UI** (terminal 2):

   ```powershell
   cd frontend
   npm install
   npm run dev
   ```

   Open http://localhost:5173

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Auth + WebSocket status |
| GET | `/api/prices` | Latest prices |
| POST | `/api/orders` | Place Buy/Sell |
| GET | `/api/trades` | Trade history |
| GET | `/api/positions` | Positions + unrealized PnL |
| POST | `/api/auth/refresh` | Refresh token |

SignalR hub: `/hubs/trading` — events `PricesUpdated`, `StatusUpdated`

### Order body

```json
{ "symbol": "EURUSD", "side": "Buy", "quantity": 1 }
```

## Database

- File: `data/trades.db`
- Schema: `database/schema.sql`

## Assumptions

- Auth JSON body: `userId`, `accountId`, `password` (PascalCase fallback).
- WebSocket messages: flexible JSON (`symbol`, `bid`, `ask`, `price`, etc.).
- Orders execute at latest in-memory price (simulated, not sent to broker).

## Project layout

```
RealTimeTrading/
├── Trading.Api/          ← ASP.NET Core backend (primary)
├── frontend/             ← React UI
├── backend/              ← Legacy Node.js API
├── database/schema.sql
└── README.md
```
