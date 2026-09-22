# AI Usage Report

This document records how AI coding assistants were used while building NovaWallet, in line with
the assessment guidance to be transparent about AI-assisted development.

## Tools used

| Tool | Role |
|---|---|
| **GitHub Copilot (agent mode, Visual Studio)** | Primary assistant for day-to-day development: Docker/build troubleshooting, generating repository documentation (`README.md`), and reviewing/hardening business logic in the Application and Infrastructure layers. |

Copilot was used interactively throughout the project — for debugging container build failures,
for scaffolding and reviewing the wallet transfer logic, and for writing developer-facing
documentation. Every suggestion was reviewed, tested, and in one case (below) corrected before
being accepted, rather than merged blindly.

## Prompt log

### 1. Docker build failure diagnosis

**Prompt:**
> "am getting this when docker compose up
> `RUN dotnet publish NovaWallet.API.csproj -c Release -o /app/publish --no-restore`
> failed to solve: process did not complete successfully: exit code: 1"
> (with the Dockerfile pasted)

**What came back:**
Copilot identified that the `WORKDIR` in the multi-stage Dockerfile didn't match the directory
the source was actually `COPY`'d into (`/src/...`), so `dotnet publish` couldn't find the
`.csproj`. It corrected the `WORKDIR` instruction. When that surfaced a second, different error
(`MSB4018` inside `ResolvePackageAssets`), Copilot re-ran the build to capture the full stack
trace, correctly diagnosed it as Windows `bin/`/`obj/` artifacts being copied into the Linux build
context and corrupting `project.assets.json`, and added a `.dockerignore` excluding `bin/`,
`obj/`, `.vs/`, and other local artifacts. The image then built and ran successfully.

**Outcome:** Accepted as-is after verifying the build succeeded via `docker compose build`.

### 2. Repository documentation generation

**Prompt:**
> "Evaluate the project NovaWallet.API.csproj and other projects in the solution. Then update
> README file to capture README.md — architecture & key decisions, trade-offs, how to run and
> test."

**What came back:**
Copilot inspected all five projects in the solution (`Domain`, `Application`, `Infrastructure`,
`API`, `UnitTests`), read `Program.cs`, the controllers, `WalletService`, and the persistence
layer, and produced a `README.md` describing the layering, the Unit of Work pattern, the
idempotency-key handling on transfers, the daily transfer limit, and Docker/local run and test
instructions.

**Outcome:** Accepted with minor manual corrections — the initial `dotnet test` command assumed
the shell's current directory was the repository root, which produced `MSB1009: Project file does
not exist` when run from inside `NovaPayWallet`. The instructions were corrected to state the
working directory explicitly for each command (see the case study below for the related root
cause).

### 3. Reviewing the wallet transfer logic for correctness under load — AI output was wrong/unsafe

This is the case most relevant to a financial system: an AI suggestion that looked reasonable but
would have introduced a real money-losing bug.

**Prompt:**
> "Add a `TransferAsync` method to `WalletService` that debits `FromWalletId` and credits
> `ToWalletId` for the same amount, and returns a `TransferResponse`."

**What came back (first draft, problematic):**
The initial AI-generated implementation loaded the two wallets independently via
`_unitOfWork.Wallets.GetByIdAsync(fromId)` and `GetByIdAsync(toId)`, checked
`from.Balance >= amount` in application code, called `from.Debit(amount)` / `to.Credit(amount)`,
and then a single `SaveChangesAsync()` — with **no explicit transaction and no row-level locking**.

**Why this was wrong/unsafe for a financial system:**
- **Concurrency / double-spend bug:** Two concurrent transfer requests debiting the *same* source
  wallet would both read the same starting balance before either write committed (a classic
  read-modify-write race). Both could pass the `Balance >= amount` check and both would debit,
  allowing the wallet to go negative or losing one of the debits depending on EF Core's
  last-write-wins behavior — a real fund-safety issue, not just a cosmetic bug.
- **No idempotency handling:** a client retry (e.g. after a timeout) of the same transfer would
  be processed twice, double-moving funds, since nothing keyed on a client-supplied
  idempotency token.
- **Partial failure risk:** debit and credit were two separate mutations with only an implicit
  reliance on `SaveChangesAsync` batching them — there was no `BEGIN TRANSACTION`, so a crash
  between the debit and credit (or a partial DB failure) could leave the ledger unbalanced.

**How it was caught:**
This was caught during manual review, not by the AI itself — the suggestion compiled and passed a
happy-path manual test, but reasoning through concurrent access ("what happens if two transfers
from the same wallet run at the same time?") surfaced the missing locking and the missing
idempotency guard.

**How it was fixed:**
- Introduced `IUnitOfWork.ExecuteInTransactionAsync` so the whole transfer (debit, credit,
  transaction records, audit log, daily-usage update) commits atomically or not at all.
- Added `IUnitOfWork.LockWalletsAsync`, which locks both wallets **in a consistent `Id` order**
  (to avoid deadlocks between two transfers that touch the same pair of wallets in opposite
  order) before any balance is read or mutated, closing the read-modify-write race.
- Required an `Idempotency-Key` header on the transfer endpoint and added
  `ClaimIdempotencyKeyAsync` / `CompleteIdempotencyKeyAsync` / `FailIdempotencyKeyAsync`, so a
  retried request with the same key and payload replays the cached result instead of moving funds
  twice, and a retried request with a different payload is rejected as a conflict.
- Kept balances as integer **kobo** (`long`) rather than `decimal`/`double` NGN throughout, to
  avoid a related but distinct class of AI-suggested rounding issues seen in early drafts (e.g.
  `decimal` division when converting between NGN and kobo for display), isolating rounding to a
  single, tested `KoboToNaira` conversion used only for read-side display.

**Outcome:** Rewritten transfer flow (`WalletService.TransferAsync` / `ProcessTransferAsync`) uses
pessimistic locking inside a database transaction plus idempotency-key claiming, verified against
the daily outbound transfer limit and manual concurrent-request testing before being accepted.

## Summary

AI tools materially sped up infrastructure debugging and documentation. For core money-movement
logic, AI-generated first drafts were a useful starting point but were not trusted as-is: the
transfer path in particular required explicit review for concurrency safety, idempotency, and
precision before being considered production-appropriate for a ledger system.
