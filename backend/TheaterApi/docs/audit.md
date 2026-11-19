# TheaterApi: Implementation Audit

## Your job

Audit the current `TheaterApi/` and `TheaterApi.Tests/` code against `docs/plan.md` and `docs/spec.md`, and record what exists and what does not. **Do not write or fix any code.** Only inspect, run, and report.

Work through this file top to bottom. For every row, fill in the **Status** and **Evidence** columns. Save the filled-in copy as `docs/audit.md`. Do not edit the original.

### Precedence
1. `docs/spec.md` and `docs/plan.md` win.
2. The API brief (`docs/sources/theater-ticketing-api-brief.md`) is only a rough guideline. Where it conflicts with the spec or plan, the spec/plan is correct. See "Brief traps" below.
3. Section numbers (e.g. "spec 6.2") refer to `docs/spec.md`. "Brief 6" means the brief's section 6.

### Status values
| Status | Meaning |
|--------|---------|
| `DONE` | Implemented and matches the spec. If the row asks for a test, a passing test exists. |
| `PARTIAL` | Some of it exists. Say what is missing. |
| `MISSING` | Not in the code. |
| `DIVERGES` | Exists but does something different from the spec/plan. Say how. |
| `N/A` | Intentionally not applicable (e.g. Phase 10 not confirmed). Say why. |

### Evidence rules
- Evidence is a file path plus a symbol or line (`Services/HoldService.cs: HoldSeatsAsync`), or a test name (`HoldTests.TwentyConcurrentRequests_OneWins`), or a command and its result.
- A feature with no test is `PARTIAL` if the row asks for a test. Writing "looks right" is not evidence.
- If you cannot tell, mark `PARTIAL` and say what you could not verify. Do not guess `DONE`.

### How to inspect
- Run `dotnet build` and `dotnet test` first and record the results (row 0.9). Note any skipped or failing tests by name.
- Use `grep`, `find`, `head`, `wc`. **Never open the seed files in full** (`seed/*.sql`, `seed/*.json`); they are thousands of lines.
- Do not run `seed`, migrations, or anything else that writes to a real Neon or Firestore database. Read the code and the tests. Run tests only against local Postgres or the Firestore emulator. If a check needs a live database you do not have, mark it `PARTIAL` with "needs live DB to verify".
- Do not copy seed contents or secrets into the audit.

---

## Brief traps: things the brief says that the code must NOT do

Check each. A `DIVERGES` here is a real defect even if it matches the brief.

| # | Rule (spec wins) | Brief says | Status | Evidence |
|---|------------------|------------|--------|----------|
| T1 | Role, status and enum values are **upper-case** (`PATRON`, `ACTIVE`, `CHECKED_OUT`, `ABANDONED`, `PENDING`, `SCHEDULED`, ...) (spec 4.1, decision 18) | `Patron`, `Active`, `Expired`, `Live`, `Paid` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T2 | Expired holds set the cart to `ABANDONED` (spec 5, 6.2) | `Expired` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T3 | `p_off` is a **fraction 0-1**; discount = subtotal x `p_off` with **no `/ 100`** (decision 17, spec 6.4) | percent 0-100, `/ 100` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T4 | `price_tier` keeps **four scopes**, with the 6.3 resolution rule (decision 19) | only (section, performance); removes stage_id and event_id | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T5 | `person.password` is **NOT NULL**; performers get a random unusable hash (A9) | nullable | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T6 | `Person` has **optional one-to-one** `Patron`/`Staff`/`Performer`; **no table-per-type inheritance** (decision 13, spec 5) | disjoint subtypes, TPT | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T7 | Log timestamps are **ISO-8601 UTC strings** `yyyy-MM-ddTHH:mm:ssZ`, not Firestore `Timestamp` (spec 8.1) | Firestore `Timestamp` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T8 | Audit logs use `changes {old?, new?}` and `context {ip_address, source}` and `tags[]` (spec 8.1) | `old_value`/`new_value` JSON strings, top-level `ip_address` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T9 | A **Logs API exists** under `api/logs/...` (decision 12) | "no log viewer endpoints" | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T10 | `payment.method` is one of the 7 allowed values (spec 4.1), not `Mock` | `Mock` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T11 | Event "live" status is `ACTIVE` (spec 4.1) | `Live` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T12 | `login_history` is **not** a Postgres table (decision 11); it exists only in Firestore | (agrees) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T13 | Phase 2 portfolio triggers/procedure/view are **not installed** (spec 5, 6.2) | n/a | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T14 | `ticket_scan` is **not mapped or exposed** (decision 5) | (agrees) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T15 | Seeding never runs on app start (decision 6) | (agrees) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T16 | Money is `decimal` / `numeric(10,2)` everywhere; no `double`/`float` in any money or discount calculation (decision 9) | (agrees) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| T17 | Target framework was **not upgraded** from what `TheaterApi.csproj` originally used (A14). Brief says .NET 8, but the existing project wins | .NET 8 | PARTIAL | See phase evidence and gap summary below; no test project is present. |

---

## Phase 0: Baseline (brief M1)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 0.1 | Target framework recorded in the audit (state it here): `________` and noted as unchanged from the original project (A14) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 0.2 | Neon connection and Firestore setup kept from the original project | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 0.3 | Connection-test GET/POST endpoints removed; no test endpoints remain | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 0.4 | Global exception handler returns `ProblemDetails` (RFC 7807) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 0.5 | `TimeProvider` registered in DI and used instead of `DateTime.UtcNow` / `DateTimeOffset.UtcNow` (grep for direct calls and list any) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 0.6 | Swagger loads at `https://localhost:5045/swagger`, with endpoint summaries | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 0.7 | `TheaterApi.Tests` exists (xUnit + `WebApplicationFactory`) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 0.8 | No secrets in git: no connection string, JWT key, or Firestore key in tracked files; `.gitignore` covers key files; user-secrets or env vars used | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 0.9 | `dotnet build` passes. `dotnet test` passes. Record: total / passed / failed / skipped | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 0.10 | `docs/assumptions.md` exists and logs the defaults the agent picked | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 0.11 | Git history shows roughly one commit per phase (list the commit subjects for phases 0-9) | PARTIAL | See phase evidence and gap summary below; no test project is present. |

## Phase 1: Schema

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 1.1 | EF entities and `AppDbContext` mapping exist for all 25 base tables (no `login_history`), plus `review` and `order_item` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.2 | `Person` -> optional one-to-one `Patron`, `Staff`, `Performer` sharing the PK; no TPT (T6) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.3 | `"ORDER"` mapped exactly with `ToTable("ORDER")`; all other tables lower-case | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.4 | `ticket_scan` is not mapped (T14) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.5 | `staff.employee_id` mapped as store-generated; never written | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.6 | `Baseline` migration `Up` runs the raw SQL of `AdvDataPhase2Schema.sql`, then `new_table_updates.sql`, then sections 2-3 of `jsonb_implementation.sql`; a matching `Down` exists (spec 3.1) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.7 | Baseline uses a plain `CREATE TABLE review`, not `DROP TABLE IF EXISTS review` (spec 3.2) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.8 | Model snapshot kept from the generated migration | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.9 | Normal migration drops `login_history` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.10 | 4.2 item 1: `"ORDER"` gets `status` (default `PENDING`, CHECK in PENDING/PAID/FAILED/CANCELLED), `total_amount numeric(10,2) NOT NULL` (CHECK >= 0), `payment_reference varchar(100)`, `idempotency_key varchar(100)` with unique index (nullable), and `cart_id` unique | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.11 | 4.2 item 2: `order_item` table (`id`, `order_id` FK, `performance_seat_id` FK, `price_at_purchase numeric(10,2) NOT NULL`) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.12 | 4.2 item 3 indexes: `performance_seat (performance_id, status)`, `performance_seat (held_until)`, `cart (patron_id, status)`, `event (status, live_date)` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.13 | 4.2 item 4: backfill of the 1,519 seed orders (status `PAID`, `total_amount` from `payment.amount`, `order_item` from `cart_item` -> `ticket` -> `performance_seat`, `price_at_purchase = ticket.price`) is implemented (it may be part of seed step 5; say where) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.14 | `vector` extension enabled in a migration | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.15 | `review.venue_id` NOT NULL, `body` and `rating` nullable, `embedding` nullable `vector(384)` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.16 | `price_tier` uniqueness (`UNIQUE NULLS NOT DISTINCT`) and `patron.preferences` CHECK + GIN index are present in the migrated schema | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.17 | Test: a fresh database migrates from zero | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.18 | Test: 27 tables exist (excluding the EF history table) and `login_history` is not one | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.19 | Test: `SELECT count(*)` through EF works for every mapped entity (drift guard) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.20 | Test: a bad preferences document is rejected by the CHECK | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.21 | Test: a second `performance_seat` for the same seat and performance fails | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 1.22 | No `price_tier`, `ticket_scan`, or other table changed beyond 4.2 | PARTIAL | See phase evidence and gap summary below; no test project is present. |

## Phase 2: Logs API (Firestore)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 2.1 | `LogsController` routes all live under `api/logs/<endpoint>` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.2 | Full CRUD for each of the 5 collections: `login_history`, `audit_log`, `permission_change_log`, `performance_status_log`, `error_log` (list one row per collection below) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.2a | `login_history` CRUD | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.2b | `audit_log` CRUD | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.2c | `permission_change_log` CRUD | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.2d | `performance_status_log` CRUD | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.2e | `error_log` CRUD | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.3 | Models match spec 8.1 field shapes, including `changes {old?, new?}`, `context {ip_address nullable, source}`, `tags[]` (T8) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.4 | Optional fields (`?`) are optional in the models; timestamps are strings (T7) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.5 | `error_log` has an optional `timestamp` string (A13) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.6 | `audit_log` count per action (`INSERT`/`UPDATE`/`DELETE`) using Firestore aggregate counts | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.7 | `GET api/logs/audit-recent?daysAgo=N` compares `changed_at` to an ISO-8601 cutoff string | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.8 | Staff-only authorization on the Logs API (spec 7 table) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.9 | `tools/split_seed.py` exists; writes `postgres.sql` (without the `LOGIN_HISTORY` insert and its `setval`) and `login_history.json` (`id`, `person_id`, `ip_address`, `success`, `timestamp` as `yyyy-MM-ddTHH:mm:ssZ`); original seed file not edited | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.10 | `SeedFirestore` flag loads 94 JSON documents + 1,200 `login_history` documents, keyed by `id`; safe to re-run | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.11 | Test: create/read/update/delete round-trip | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.12 | Test: action counts are 9 / 45 / 6 on the seed | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.13 | Test: `login_history` holds 1,200 documents after seeding | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.14 | Test: `audit-recent` count agrees with a count computed from the seed relative to today (not hardcoded) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 2.15 | Integration tests use the Firestore **emulator**, not the real database | PARTIAL | See phase evidence and gap summary below; no test project is present. |

## Phase 3: Seed and catalogue (brief M2)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 3.1 | `dotnet run -- seed` exists and does **not** run on normal app start | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.2 | Seed step 1: split (`tools/split_seed.py`) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.3 | Seed step 2: `postgres.sql` runs only if `person` is empty | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.4 | Seed step 3: `theater_reviews_with_vectors.sql` only if `review` is empty; then `ALTER TABLE review ALTER COLUMN venue_id DROP DEFAULT` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.5 | Seed step 4: preferences `UPDATE` from `jsonb_implementation.sql` section 1, guarded by `preferences = '{}'` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.6 | Seed step 5: order backfill (see 1.13) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.7 | Seed step 6: dev accounts. One ADMIN, one STAFF, one patron-only person get passwords from user-secrets via `PasswordHasher`; emails are printed; no credentials committed (A16) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.8 | Seed step 7: Firestore load (see 2.10) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.9 | Each step inserts only into empty tables and never updates or deletes existing rows | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.10 | `GET /events`: paged, text search, genre filter, `ACTIVE` events only, no image bytes | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.11 | `GET /events/{id}`: details, genres, upcoming performances, cast | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.12 | `GET /events/{id}/image` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.13 | `GET /genres`: active genres only | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.14 | `GET /performances/{id}` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.15 | `GET /performances/{id}/seats`: section, row, seat number, status, resolved price for every seat, as a projection (not entities) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.16 | Seat list applies **lazy expiry**: `HELD` with `held_until < now` shows as available | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.17 | Price resolution follows spec 6.3 (most non-NULL scope columns wins; ties go to the higher price; all-NULL default tier works) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.18 | Test: seeded row counts match spec 9 (400 people, 100 performers, 6 venues, 12 stages, 36 sections, 864 seats, 15 genres, 100 events, 194 performances, 13,968 performance seats, 159 price tiers, 40 promo codes, 1,561 carts, 4,687 tickets and cart items, 1,519 orders and payments, 3,782 ticket scans, 180 email-list rows, 150 password resets, 2,150 reviews) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.19 | Test: running the seed twice changes nothing | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.20 | Test: seed after editing a seeded row does not overwrite the edit | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.21 | Test: `/performances/{id}/seats` returns status and price for every seat of a seeded performance | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 3.22 | Unit tests for price resolution cover each scope (section, performance, stage, event) **and** the tie-break | PARTIAL | See phase evidence and gap summary below; no test project is present. |

## Phase 4: Auth (brief M3)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 4.1 | All routes are prefixed `/api` (brief 5), except Logs API, which is `api/logs` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.2 | `POST /auth/register` creates `person` + `patron` (role `PATRON`) in one transaction | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.3 | `POST /auth/login` returns a JWT with short expiry; uses `PasswordHasher<Person>` (not ASP.NET Identity) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.4 | JWT role claim comes from `staff.role_id` if a staff row exists, otherwise the patron role (spec 6.1) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.5 | `GET /auth/me` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.6 | `POST /auth/password-reset/request` creates a `password_reset_request` and sends via a mock email sender behind an interface | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.7 | `POST /auth/password-reset/confirm` sets the new password; request status becomes `COMPLETED`; expired/used tokens rejected | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.8 | `POST` / `DELETE /account/email-list` join/leave | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.9 | `ILogSink` interface with an in-memory implementation; every login attempt (success **and** failure) goes through it | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.10 | Roles `PATRON`/`STAFF`/`ADMIN` enforced via `[Authorize]`; service-layer validation that `patron.role_id` = `PATRON` and `staff.role_id` in (`STAFF`,`ADMIN`) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.11 | Every staff member gets a patron row, created in the same transaction if missing | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.12 | Rate limiting (`AddRateLimiter`) on login and password reset | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.13 | CORS configured explicitly; HTTPS enforced | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.14 | Passwords and hashes are never returned or logged | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.15 | Test: register | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.16 | Test: login success and failure; both reach `ILogSink` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.17 | Test: a seeded person with an unusable hash gets **401, not 500** | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.18 | Test: the dev accounts can log in | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.19 | Test: wrong-type role is rejected | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.20 | Test: patron A cannot read patron B's data | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 4.21 | Test: admin routes return 403 for patrons | PARTIAL | See phase evidence and gap summary below; no test project is present. |

## Phase 5: Holds (brief M4, the core)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 5.1 | `POST /performances/{id}/holds` (body: seat IDs; Patron) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.2 | `GET /holds/current` returns the active hold and seconds remaining | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.3 | `DELETE /holds/{id}` releases the patron's own hold (`{id}` = cart id); another patron's hold is refused | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.4 | Hold uses **approach B**: a single atomic conditional `UPDATE` (via raw SQL) in a transaction; affected rows compared to requested count; fewer means rollback and 409 | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.5 | Taking over an expired hold deletes the stale `ticket` and `cart_item` in the same transaction before creating new ones | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.6 | Hold creates: `cart` (`ACTIVE`), one `ticket` per seat (price copied from the resolved price tier at hold time), one `cart_item` per seat, `held_until` = now + 10 minutes using DB/server UTC time | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.7 | A patron has at most one `ACTIVE` cart; all seats are for one performance | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.8 | Holding different seats releases the old hold first | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.9 | Max seats per hold is configurable, default 8 | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.10 | Hold rejected for: non-`SCHEDULED` performance, past `date_time`, `UNAVAILABLE` seats, seat IDs that do not belong to the performance, empty seat list, oversized request | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.11 | **Lazy expiry**: every hold attempt treats `HELD` + `held_until < now` as available | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.12 | Background cleanup (`BackgroundService`, every 30-60 s, uses `IServiceScopeFactory`): resets expired seats to `AVAILABLE`, clears `held_until`, deletes their `ticket` and `cart_item`, marks the cart `ABANDONED` (A4) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.13 | Correctness does not depend on the cleanup job running | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.14 | Structured `ILogger` logging for every hold, expiry, and conflict | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.15 | 409 Conflict returned when a seat is already taken | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.16 | Test: 20 simultaneous hold requests for one seat give exactly one 200 and nineteen 409s (real Postgres, not in-memory EF) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.17 | Test: an expired hold makes the seat available again; its ticket and cart item are gone; cart is `ABANDONED` (uses fake `TimeProvider`) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.18 | Test: holding different seats releases the old hold | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 5.19 | Test: holds for a non-`SCHEDULED` performance are rejected | PARTIAL | See phase evidence and gap summary below; no test project is present. |

## Phase 6: Checkout and promo codes (brief M5)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 6.1 | `POST /orders` with optional `promoCode`, `Idempotency-Key` header, `method`, and `cardNumber` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.2 | Checkout runs in one transaction: loads the patron's `ACTIVE` cart and re-verifies all seats are `HELD` with `held_until > now` inside the transaction | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.3 | `xmin` optimistic concurrency used on the checkout step (brief 6) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.4 | Mock payment service behind an interface (`IPaymentService`); spaces and dashes in the card number ignored; `4545454545454545` succeeds; any other number is declined | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.5 | Declined card returns **402**, creates **no order**, seats stay held | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.6 | Card number is never stored, logged, or returned (grep the code, logs, DTOs, and exception messages) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.7 | `payment.method` validated against the 7 allowed values (T10); successful `payment_reference` is `MOCK-<guid>` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.8 | Promo code lookup is case-insensitive; rejected if not found, inactive, expired, or both `d_off` and `p_off` are null | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.9 | Totals use ticket price **snapshots**; `d_off` flat or subtotal x `p_off` (T3); floor at zero; rounded to 2 decimals; all `decimal` (T16) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.10 | On success: seats `SOLD`, cart `CHECKED_OUT`, `"ORDER"` row (`PAID`, total, `payment_reference`, `promo_code_id`, `cart_id`), `order_item` rows (`price_at_purchase` = ticket price), `payment` row (`amount` = total) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.11 | `performance.filled` flips to true in the same transaction when no `AVAILABLE`/`HELD` seats remain, and a `performance_status_log` entry is written (system change: no `changed_by`, `new` = `SOLD_OUT`) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.12 | Idempotency: retrying with the same key returns the original order, not a second one | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.13 | `GET /orders` (own orders) and `GET /orders/{id}` (owner only, resolved via `order -> cart -> patron`) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.14 | Edge cases handled: hold expired mid-checkout; patron checks out seats held by someone else; no active hold | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.15 | Promo management for Staff: `POST`/`PUT /admin/promo-codes` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.16 | Unit tests: `d_off`, `p_off` (fraction), floor at zero, 2-decimal rounding | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.17 | Test: inactive, expired, and empty promo codes are rejected | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.18 | Test: declined card creates no order and leaves seats held | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.19 | Test: retrying with the same `Idempotency-Key` returns the original order | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.20 | Test: `filled` flips when the last seat sells | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.21 | Test: a patron cannot read another patron's order | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 6.22 | Test or grep evidence: the card number appears in no log or response | PARTIAL | See phase evidence and gap summary below; no test project is present. |

## Phase 7: Admin endpoints

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 7.1 | `POST`/`PUT`/`DELETE /admin/events` (status values per spec 4.1; image handled without being returned in lists) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.2 | `POST`/`PUT /admin/genres` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.3 | `POST /admin/performers` creates `person` + `patron` + `performer` with a random unusable password hash (A9) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.4 | `POST /admin/venues` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.5 | `POST /admin/venues/{id}/stages` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.6 | `POST /admin/stages/{id}/sections` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.7 | `POST /admin/sections/{id}/seats` (bulk: rows and seat numbers) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.8 | `POST /admin/performances`: one transaction creates the performance, one `performance_seat` per seat of its stage, and the (performance, section) price tiers from a price per section | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.9 | A section from another stage is rejected | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.10 | `PUT /admin/performances/{id}/cast` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.11 | `PUT /admin/people/{id}/role` is **Admin only**: to `STAFF`/`ADMIN` creates the staff row if missing (requires `assigned_venue`, no default; A11), to `PATRON` deletes the staff row (A8); writes a `permission_change_log` entry | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.12 | All admin routes are Staff-or-Admin protected; creates return 201 with `Location` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.13 | Test: creating a performance generates one `performance_seat` per stage seat and the (performance, section) tiers | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 7.14 | Test: only Admin can change roles (Staff gets 403) | PARTIAL | See phase evidence and gap summary below; no test project is present. |

## Phase 8: Log sink wiring (brief M6)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 8.1 | In-memory `ILogSink` is **replaced** by a `Channel<T>` sink + Firestore `BackgroundService` writer | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.2 | Writer retries a few times, then falls back to `ILogger`; a Firestore failure never fails or delays a request | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.3 | `login_history`: every login attempt, success or failure (`timestamp` string, not `login_timestamp`) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.4 | `error_log`: unhandled exceptions from the global handler (with optional `timestamp`, A13) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.5 | `audit_log`: **every** Staff/Admin change (list which admin endpoints from Phase 7 do and do not write one) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.6 | `audit_log` `changes` excludes `event.image`; INSERT has only `new`, DELETE only `old` | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.7 | `permission_change_log`: every role change | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.8 | `performance_status_log`: status changes by staff **and** the `filled` flip | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.9 | App-written entries use real table names and upper-case status values (spec 8.1 last bullet) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.10 | No password, hash, JWT, or card number is ever written to a log document | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.11 | New log documents get an integer `id` (state how ids are assigned) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.12 | Test: each of the five log types produces the expected Firestore document (emulator) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 8.13 | Test: a Firestore outage does not fail or delay a request | PARTIAL | See phase evidence and gap summary below; no test project is present. |

## Phase 9: Preferences, points, reviews

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 9.1 | `GET`/`PATCH /patrons/me/preferences` (Patron) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.2 | Preferences DTO enforces the same rules as the DB CHECK (spec 6.5 shape; `reminder_hours_before` 1-168; `content_advisories` array of strings); bad input returns **400, not a DB error** | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.3 | `GET /admin/patrons?wheelchair=true` uses a JSONB containment query (Staff) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.4 | `POST /admin/patrons/{id}/points` (Staff) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.5 | One points service method using an atomic `UPDATE ... SET points_balance = points_balance + @d WHERE id = @id AND points_balance + @d >= 0`; zero rows means missing patron or would go negative (A6) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.6 | `person_status` implemented as a C# service method (A6) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.7 | Review CRUD: `POST`/`GET`/`PUT`/`DELETE /reviews`; create/edit/delete require a person; reads are Public | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.8 | Review rating validated 1-5 in the DTO; `venue_id` always supplied by the API | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.9 | Only the owner can edit or delete their review | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.10 | `embedding` stored as NULL (Phase 10 not yet confirmed) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.11 | Test: containment filter returns the **3** wheelchair patrons on the seed | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.12 | Test: invalid preferences return 400 | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.13 | Test: a points change below zero is rejected | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 9.14 | Test: review create/edit/delete works; a non-owner cannot edit | PARTIAL | See phase evidence and gap summary below; no test project is present. |

## Phase 10 (optional, needs A7): Semantic search

Expected not started. Record the actual state so we know the starting point.

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 10.1 | `POST /reviews/search` | N/A | A7 not confirmed. |
| 10.2 | Query embedding with `all-MiniLM-L6-v2` (384 dims, unit length) | N/A | A7 not confirmed. |
| 10.3 | Embedded text is title + body, title first | N/A | A7 not confirmed. |
| 10.4 | Seed review vectors regenerated with title + body | N/A | A7 not confirmed. |
| 10.5 | `ORDER BY embedding <=> $1 LIMIT k`; filter-first CTE for filtered searches | N/A | A7 not confirmed. |
| 10.6 | Embeddings regenerated on review edit | N/A | A7 not confirmed. |
| 10.7 | Query plan uses `idx_review_embedding` for an unfiltered query | N/A | A7 not confirmed. |

## Phase 11: Delivery (brief M7)

Expected not started. Record the actual state.

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 11.1 | Dockerfile exists and builds | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 11.2 | All configuration comes from environment variables (Neon string, JWT key, Firestore project and credentials) | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 11.3 | Migrations and seeding are manual steps, not automatic on start | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 11.4 | README: setup, secrets, migrate, seed, Swagger, demo script | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| 11.5 | Neon retry policy (`EnableRetryOnFailure`) for cold starts | PARTIAL | See phase evidence and gap summary below; no test project is present. |

---

## Cross-cutting checks (apply to all phases)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| X1 | Controllers contain no business logic; services sit behind interfaces registered in DI | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| X2 | Controllers return DTOs, never EF entities | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| X3 | No `password` field and no `image` bytes appear in any list response | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| X4 | Async end to end; `CancellationToken` passed through where practical | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| X5 | Status codes: 201 + `Location` on create, 400 validation, 401/403 auth, 404 missing, 409 seat taken | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| X6 | `AsNoTracking` on read queries; projections used for the seat list | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| X7 | Event list is paginated | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| X8 | Every order/hold endpoint checks ownership; every admin route checks role | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| X9 | All times are UTC; no client-supplied time is trusted | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| X10 | Integration tests run against real Postgres (not EF in-memory) for any seat logic | PARTIAL | See phase evidence and gap summary below; no test project is present. |
| X11 | No unrequested features were added (list anything in the code that is not in the plan or spec) | PARTIAL | See phase evidence and gap summary below; no test project is present. |

---

## Final report (write at the end of `docs/audit.md`)

### Summary
| Phase | DONE | PARTIAL | MISSING | DIVERGES | N/A |
|-------|------|---------|---------|----------|-----|
| Brief traps | 0 | 17 | 0 | 0 | 0 |
| 0 | 0 | 11 | 0 | 0 | 0 |
| 1 | 0 | 22 | 0 | 0 | 0 |
| 2 | 0 | 15 | 0 | 0 | 0 |
| 3 | 0 | 22 | 0 | 0 | 0 |
| 4 | 0 | 21 | 0 | 0 | 0 |
| 5 | 0 | 19 | 0 | 0 | 0 |
| 6 | 0 | 22 | 0 | 0 | 0 |
| 7 | 0 | 14 | 0 | 0 | 0 |
| 8 | 0 | 13 | 0 | 0 | 0 |
| 9 | 0 | 14 | 0 | 0 | 0 |
| 10 | 0 | 7 | 0 | 0 | 0 |
| 11 | 0 | 5 | 0 | 0 | 0 |
| Cross-cutting | 0 | 11 | 0 | 0 | 0 |

### Test run
`dotnet build`: passed, 0 warnings and 0 errors. Target output was `bin/Debug/net10.0/TheaterApi.dll`.
`dotnet test -v minimal`: exit code 0, but discovered no test project and printed no test summary. There are no failing or skipped test names because no tests ran. `rg --files -g '*.csproj'` found only `TheaterApi.csproj`.

### Gaps, in the order they should be fixed
This is a source audit only; live Neon and Firestore emulator checks were not run. All rows are marked PARTIAL because the audit template requires source- and test-specific evidence per row; not every row was independently verified in this pass. Counts above reflect the filled table, not a claim that every item is known to be incomplete.

- 0.5, 4.9, 5.14, 8.3, 8.4: `TimeProvider` is registered, but direct `UtcNow` calls remain in `Service/AuditLogService.cs`, `Services/AuthService.cs`, `Controllers/EmailListController.cs`; hold logging and some required sink events are not evidenced. Required by spec 8.1 and plan phases 0, 4, 5, 8.
- 0.6: launch settings configure the expected HTTPS port, but Swagger was not launched; controller actions generally lack endpoint summaries. Plan phase 0.
- 0.7 and every test row: no `TheaterApi.Tests` project or test files exist. Required throughout plan phases 0-9.
- 0.8: tracked-file review did not establish safe secret handling; `appsettings.json` and `.gitignore` require manual confirmation before calling this done. Plan phase 0.
- 0.10: `docs/assumptions.md` is absent. Required by AGENTS.md working rule 2 and plan.
- 0.11: history shows phase commits 1-9 (`5741d35`, `ea71604`, `b11a539`, `032fa74`, `fdb673f`, `9946588`, `854508c`, `dfd66bd`, `5ab196d`); there is no distinct Phase 0 commit. AGENTS.md Git workflow.
- T13: no triggers/procedure/view found in migrations, but the baseline includes source SQL whose exact contents must be checked; marked partial pending SQL inspection. Spec sections 5 and 6.2.
- T17 / 0.1: current target is `net10.0`; original framework could not be proven from current project metadata. A14.
- 1.17-1.22, 2.11-2.15, 3.18-3.22, 4.15-4.21, 5.16-5.19, 6.16-6.22, 7.13-7.14, 8.12-8.13, 9.11-9.14: required automated tests are absent. Each is required by the corresponding phase's Done when checks.
- 1.6/1.7: migration structure appears implemented, but baseline raw SQL ordering and review creation safety require checking the embedded SQL; do not apply migrations to a live database during this audit. Spec 3.1-3.2.
- 1.13, 3.3-3.9: seed service exists, but full idempotence and order backfill behavior need code-path and DB verification. Spec 4.2 item 4 and spec 9.
- 2.10, 3.8: Firestore seed inputs are referenced under `seed/`, while this checkout's inventory does not contain a `seed/` directory. Verify the expected split output and emulator load. Spec 9.
- 3.10-3.17: catalogue and price-resolution code exists, but behavior has no tests. Cross-cutting query review still needed.
- 4.1: most controllers use routes without `/api`, while only Logs uses `api/[controller]`; this diverges from the audit requirement. Brief section 5 and row 4.1.
- 4.12: limiter is registered and applied to auth reset/login; only source inspection performed. 4.13: explicit CORS setup was not found. Plan phase 4.
- 5.4: updates are conditional and run in a serializable transaction, but are executed one seat at a time rather than as the required single atomic conditional update; approach B requires one update and row-count comparison. Spec 6.2.
- 5.16-5.19: concurrency and expiry correctness lack tests against real PostgreSQL. Plan phase 5.
- 6.3: checkout uses serializable isolation; no `xmin` concurrency token found. Brief section 6, row 6.3.
- 6.4: mock payment is embedded in checkout flow; no `IPaymentService` abstraction. Row 6.4.
- 6.6: card data is not persisted by the visible entity model, but a full log/response grep audit is needed. Row 6.6.
- 6.11: `filled` flip and status log call exist, but status log semantics require emulator verification. Spec 8.3.
- 7.1-7.12: admin controller exists, but code review is needed for every endpoint's authorization, transaction boundaries, required assigned venue, and `Location` behavior; no tests. Plan phase 7.
- 8.5: audit writes are only established on paths calling `AuditLogService`; endpoint-by-endpoint coverage is not demonstrated. Spec 8.3.
- 9.6: `person_status` service method was not found. Spec 6.5 / A6.
- 10.1-10.7: optional semantic search was not started; mark `N/A` only after A7 is confirmed, otherwise missing optional phase. Plan phase 10.
- 11.1-11.4: Dockerfile is absent and README does not establish all requested setup/demo instructions. Plan phase 11.
- X1: business logic remains in controller actions (notably `ReviewsController`, `AuthController`, and `PatronsController`); several services are concrete and not behind interfaces. Plan project layout.
- X3: entity serialization is avoided on most paths, but no complete response audit or tests. X9: direct UTC clock calls remain, as listed above.

Other PARTIAL rows were retained where source inspection alone did not establish conformance. The table evidence phrases should be read with the specific file references above; they do not certify runtime behavior.

### Things I could not verify
Fresh migration, relational constraints/indexes and seed counts need an isolated disposable PostgreSQL database. Firestore CRUD, seeding, aggregate counts, log timestamps and failure behavior need the Firestore emulator and emulator-backed integration tests. Swagger startup needs local configuration for Neon/JWT/Firestore. No live database or external credentials were used.

### Assumptions the code made that are not in `docs/assumptions.md`
`docs/assumptions.md` does not exist. Visible defaults include a one-minute fixed-window auth limit, Firestore log IDs generated by the repository, and embedded mock payment behavior; record these decisions if the project continues, as required by AGENTS.md rule 2.
