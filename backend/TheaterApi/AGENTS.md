# AGENTS.md - TheaterApi (Tradition LTD. theater ticketing API)

## What this is
ASP.NET Core Web API (C#, EF Core) for a theater ticketing system: browse events, hold seats for 10 minutes, check out.
PostgreSQL on Neon (cloud) is the source of truth. Cloud Firestore stores system logs. Demoed mainly through Swagger.

Read these before writing code:
- `docs/spec.md` - what to build, closed decisions, business rules, where sources disagree
- `docs/plan.md` - build order, with a "Done when" check per phase
- `docs/sources/` - original class deliverables, SQL schema files, and the API brief
- `seed/` - seed data (thousands of lines of inserts). Never open these in full; use `head`, `grep`, `wc`

Precedence when sources disagree: `docs/spec.md`, then the SQL files, then the API brief, then the Phase reports.
The API brief is an older plan: follow it for stack, routes, and the hold/checkout algorithm; its data model is superseded.

## Working rules
1. Work through `docs/plan.md` in order. Do not start a phase until the previous phase's "Done when" checks pass. Commit once per phase.
2. No silent decisions. If the spec does not answer something, choose the simplest default, record it in `docs/assumptions.md` (what you chose, why, the alternative), and keep going.
3. Closed decisions (spec section 2) are not up for change. If one looks wrong, note it in `docs/assumptions.md`; do not work around it.
4. Build only what the spec lists. No extra endpoints, features, or abstraction layers.
5. Run `dotnet build` and `dotnet test` before calling a phase done. The repo must build at the end of every phase.
6. Never commit secrets (connection strings, service-account JSON, JWT keys, seed account passwords).
7. Never run the SQL files against a database that holds data you were not told to reset. `new_table_updates.sql` is run once inside the baseline migration: re-running it fails on its `ADD COLUMN` lines and wipes `review`.

## Git workflow
- **Branching:** Always create and check out a new branch before making changes (e.g., `phase-1-setup`, `phase-2-auth`). Never work or commit directly on `main` or `master`.
- **Committing:** Stage and commit once per completed phase after `dotnet build` and `dotnet test` pass.
- **No merging:** Never merge branches. Do not run `git merge`, `git rebase`, `git pull`, or `gh pr merge`.
- **Handoff:** Once a phase is committed on its branch, stop and report the branch name so the user can review and merge it on GitHub.

## Out of scope (do not build)
- Frontend and seat-map rendering (seats live in Postgres; maps belong to the frontend)
- Ticket scanning (the `ticket_scan` table exists; do not map or expose it)
- USA support (Canada only: CAD, Canadian provinces, postal codes, phone formats)
- Multi-tenancy (one venue per deployment, no tenant column)
- Real payments and real email (mock services behind interfaces)
- Refunds, QR/PDF tickets, real-time seat updates

## Stack
- ASP.NET Core Web API with controllers, C#, EF Core, Npgsql. Keep the target framework `TheaterApi.csproj` already uses
- Neon PostgreSQL (the `vector` extension is required for reviews)
- Cloud Firestore via `Google.Cloud.Firestore`; the Firestore emulator for tests
- Auth: JWT + `PasswordHasher<Person>` (not ASP.NET Identity)
- xUnit + `WebApplicationFactory`, Swagger/Swashbuckle, Docker
- Dev OS: Linux

## Project layout
`TheaterApi/` with `Controllers/`, `Services/`, `Data/` (context, seeding), `Migrations/`, `Models/`, `Dtos/`, `Logging/`, `BackgroundJobs/`, `Middleware/`; tests in `TheaterApi.Tests/`.
Controllers only handle HTTP and return DTOs, never EF entities. Business logic lives in services behind interfaces.

## Commands
```bash
dotnet build
dotnet run                      # Swagger: https://localhost:5045/swagger
dotnet run -- seed              # deliberate seed step (see spec 9); never automatic
dotnet test
dotnet ef migrations add <Name>
dotnet ef database update
```

## Configuration and secrets
- Postgres: user-secrets key `ConnectionStrings:Neon` (Neon free tier suspends when idle: enable connection retry)
- JWT signing key and dev account credentials: user secrets locally, environment variables in production
- Firebase: `GOOGLE_APPLICATION_CREDENTIALS` pointing to a service-account JSON kept outside the repo or under a `.gitignore`d path; the project name is in `TheaterApi/appsettings.json`
- Firestore seeding: `SeedFirestore` flag in `TheaterApi/appsettings.Development.json` (safe to re-run; set back to false after)

## Conventions
- Integer primary keys. Money is `numeric(10,2)` / `decimal`, never float
- Timestamps are `timestamptz` / UTC `DateTimeOffset`. Use the database or server clock via `TimeProvider`, never client time
- Table and column names are the SQL's. `"ORDER"` is quoted upper-case: map it exactly. Other tables are lower-case
- Enum-like values are upper-case strings (spec 4.1), including event status `ACTIVE`. Do not invent new ones
- `promo_code.p_off` is a fraction from 0 to 1 (0.15 = 15%), in the database, the API, and calculations. The brief says percent; it is wrong
- `login_history` is a Firestore collection, not a Postgres table
- Mock payment: card `4545 4545 4545 4545` succeeds, anything else is declined (402). Never store, log, or return a card number
- Passwords: hash only. Never log, return, or seed plaintext
- Errors: RFC 7807 `ProblemDetails`. A seat already taken returns 409
- Anything that changes several tables (hold, checkout, performance creation, staff creation) runs in one transaction
- Seat concurrency is app-level (brief section 6, approach B). Do not add database triggers for holds or the `filled` flag
- A Firestore failure must never fail or delay a request
- Log strings in Firestore are free text; timestamps there are ISO-8601 UTC strings `yyyy-MM-ddTHH:mm:ssZ`, never Firestore `Timestamp`s
