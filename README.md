# NovaWallet Ledger Service
FirstBank NovaPay is a fictional digital-first financial super-app built by FirstBank's Digital Factory for the Nigerian and West African mass market

NovaWallet is a small, wallet API demonstrating core building blocks of a digital
wallet/payments backend: account credits (simulated inbound transfers), atomic
wallet-to-wallet transfers with idempotency and daily limits, balance lookups, and paginated
transaction statements.

## Architecture

I use Clean architecture. This make the project split into four projects plus a test
project:


NovaWallet.API    : ASP.NET Core Web API (controllers, auth)
NovaWallet.Application   : Use cases / business services, DTOs
NovaWallet.Infrastructure : EF Core persistence (DbContext, repositories, Unit of Work, migrations)
NovaWallet.Domain          : Entities, enums, domain exceptions
Tests/NovaWallet.UnitTests  :  xUnit test project

 

### Key components

- **`Wallet` (Domain)** — an aggregate root that owns its balance invariants. `Credit`/`Debit`
  validate amounts and throw domain exceptions  
- **`WalletService` (Application)** — orchestrates use cases: create wallet, credit, transfer,
  balance, statement. Returns a `ResponseResult<T> 
- **`IUnitOfWork` / `UnitOfWork` (Infrastructure)** — wraps a single `DbContext` per
  request/operation and exposes all repositories plus a single `SaveChangesAsync` 

  
- **Audit log (`AuditLog`)** — every state-changing operation (wallet created/credited/debited)
  writes an audit entry with a JSON snapshot of what changed, in the same transaction as the
  business change.

## Trade-offs & design decisions

- **`EnsureCreatedAsync` instead of running migrations on startup** — `Program.cs` calls
  `dbContext.Database.EnsureCreatedAsync()` so `docker compose up` is a single command to get a
  working service + schema, without a separate `dotnet ef database update` step. EF Core
  migrations still exist (`Infrastructure/Migrations`) for production-style deployments, but
  `EnsureCreated` and migrations are **not compatible with each other** on the same database — if
  you switch to running migrations, remove the `EnsureCreatedAsync` call first.

- **`ResponseResult<T>` over exceptions for expected failures** — validation/not-found/conflict
  are modeled as data (`ErrorType`), not exceptions, so the API layer can cleanly map them to
  400/404/409 without `try/catch` per action. Unexpected errors (bugs, infra failures)  

- **One wallet per customer per currency** — `CreateWalletAsync` rejects a second wallet for the
  same `CustomerId`, keeping "which wallet do I credit" unambiguous.  

- **Mock JWT issuer instead of real IdP integration** — keeps the sample runnable without external
  dependencies (Azure AD tenant, etc.). Not suitable for production as-is: anyone can mint a token
  for any subject.
- **Swagger/OpenAPI reachable in all environments** — intentionally not restricted to
  `Development`, so the API is self-documenting immediately after `docker compose up`. In a real
  production deployment this would typically be locked down or removed.

## How to run

### Prerequisites

- .NET 9 SDK (for running locally without Docker)
- Docker Desktop (for `docker compose up`)

### Option A — Docker Compose (recommended)

From the `NovaPayWallet` folder (contains `docker-compose.yml`):

```powershell
cd NovaPayWallet
docker compose up --build  this build and start
Or 
docker compose up



This starts:
- `postgres` — PostgreSQL 16 on `localhost:5432` (db `novawallet`, user/password `postgres`/`postgres`)
- `api` — the NovaWallet API on `http://localhost:8080`

On startup the API calls `EnsureCreatedAsync`, so the schema is created automatically — no manual
migration step is required.

Once running:
- Swagger UI: `http://localhost:8080/swagger`
- OpenAPI document: `http://localhost:8080/openapi/v1.json`

Stop everything with `docker compose down` (add `-v` to also drop the Postgres data volume).

 
 Browse to the URL printed in the console (see `NovaWallet.API/Properties/launchSettings.json`)
   and append `/swagger`.

 ## Using the API

1. Get a token:

```http
POST /api/auth/token
Content-Type: application/json

{ "subject": "customer-1" }
```

2. Use the returned token as `Authorization: Bearer <token>` for all `/api/wallets/*` calls.
3. Create a wallet, credit it, then transfer between two wallets — remember to send a unique
   `Idempotency-Key` header on `POST /api/wallets/transfer`.

A ready-made set of sample requests is available in `NovaWallet.API/NovaWallet.API.http`.

## How to test

Unit tests live in `Tests/NovaWallet.UnitTests` (xUnit). Run them with:

```powershell on directory of the project
use 
 dotnet test .\NovaPayWallet.slnx

