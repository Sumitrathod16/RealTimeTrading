# Trading.Api — ASP.NET Core Web API

Implements assignment **§6.1 Backend Requirements**:

| Requirement | Implementation |
|-------------|----------------|
| ASP.NET Core Web API (C#) | This project (.NET 8) |
| REST auth | `AuthService` → POST token endpoint |
| WebSocket price feed | `PriceFeedWorker` + `ClientWebSocket` + token query |
| Latest price per symbol | `PriceCache` (in-memory `ConcurrentDictionary`) |
| REST for frontend | Controllers under `/api/*` |
| Trade storage | SQLite via EF Core (`data/trades.db`) |
| WebSocket reconnect | Exponential backoff in `PriceFeedWorker` |
| Logging | `ILogger` on auth, feed, orders, errors |

## Run

1. Install [.NET 8 SDK](https://dotnet.microsoft.com/download).
2. Set credentials (environment variables or `appsettings.json`):

   ```
   Auth__UserId=csfx2568033
   Auth__AccountId=21733
   Auth__Password=your_password
   ```

   Or copy from repo root `.env` and export `USER_ID`, `ACCOUNT_ID`, `PASSWORD` (mapped in `Program.cs`).

3. From repo root:

   ```bash
   dotnet run --project Trading.Api
   ```

4. Swagger: http://localhost:5000/swagger  
   Health: http://localhost:5000/api/health

## Endpoints

- `GET /api/health`
- `GET /api/prices`
- `POST /api/orders`
- `GET /api/trades`
- `GET /api/positions`
- `POST /api/auth/refresh`
- SignalR hub: `/hubs/trading` (`PricesUpdated`, `StatusUpdated`)
