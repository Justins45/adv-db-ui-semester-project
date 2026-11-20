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
| T1 | Role, status and enum values are **upper-case** (`PATRON`, `ACTIVE`, `CHECKED_OUT`, `ABANDONED`, `PENDING`, `SCHEDULED`, ...) (spec 4.1, decision 18) | `Patron`, `Active`, `Expired`, `Live`, `Paid` | | |
| T2 | Expired holds set the cart to `ABANDONED` (spec 5, 6.2) | `Expired` | | |
| T3 | `p_off` is a **fraction 0-1**; discount = subtotal x `p_off` with **no `/ 100`** (decision 17, spec 6.4) | percent 0-100, `/ 100` | | |
| T4 | `price_tier` keeps **four scopes**, with the 6.3 resolution rule (decision 19) | only (section, performance); removes stage_id and event_id | | |
| T5 | `person.password` is **NOT NULL**; performers get a random unusable hash (A9) | nullable | | |
| T6 | `Person` has **optional one-to-one** `Patron`/`Staff`/`Performer`; **no table-per-type inheritance** (decision 13, spec 5) | disjoint subtypes, TPT | | |
| T7 | Log timestamps are **ISO-8601 UTC strings** `yyyy-MM-ddTHH:mm:ssZ`, not Firestore `Timestamp` (spec 8.1) | Firestore `Timestamp` | | |
| T8 | Audit logs use `changes {old?, new?}` and `context {ip_address, source}` and `tags[]` (spec 8.1) | `old_value`/`new_value` JSON strings, top-level `ip_address` | | |
| T9 | A **Logs API exists** under `api/logs/...` (decision 12) | "no log viewer endpoints" | | |
| T10 | `payment.method` is one of the 7 allowed values (spec 4.1), not `Mock` | `Mock` | | |
| T11 | Event "live" status is `ACTIVE` (spec 4.1) | `Live` | | |
| T12 | `login_history` is **not** a Postgres table (decision 11); it exists only in Firestore | (agrees) | | |
| T13 | Phase 2 portfolio triggers/procedure/view are **not installed** (spec 5, 6.2) | n/a | | |
| T14 | `ticket_scan` is **not mapped or exposed** (decision 5) | (agrees) | | |
| T15 | Seeding never runs on app start (decision 6) | (agrees) | | |
| T16 | Money is `decimal` / `numeric(10,2)` everywhere; no `double`/`float` in any money or discount calculation (decision 9) | (agrees) | | |
| T17 | Target framework was **not upgraded** from what `TheaterApi.csproj` originally used (A14). Brief says .NET 8, but the existing project wins | .NET 8 | | |

---

## Phase 0: Baseline (brief M1)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 0.1 | Target framework recorded in the audit (state it here): `________` and noted as unchanged from the original project (A14) | | |
| 0.2 | Neon connection and Firestore setup kept from the original project | | |
| 0.3 | Connection-test GET/POST endpoints removed; no test endpoints remain | | |
| 0.4 | Global exception handler returns `ProblemDetails` (RFC 7807) | | |
| 0.5 | `TimeProvider` registered in DI and used instead of `DateTime.UtcNow` / `DateTimeOffset.UtcNow` (grep for direct calls and list any) | | |
| 0.6 | Swagger loads at `https://localhost:5045/swagger`, with endpoint summaries | | |
| 0.7 | `TheaterApi.Tests` exists (xUnit + `WebApplicationFactory`) | | |
| 0.8 | No secrets in git: no connection string, JWT key, or Firestore key in tracked files; `.gitignore` covers key files; user-secrets or env vars used | | |
| 0.9 | `dotnet build` passes. `dotnet test` passes. Record: total / passed / failed / skipped | | |
| 0.10 | `docs/assumptions.md` exists and logs the defaults the agent picked | | |
| 0.11 | Git history shows roughly one commit per phase (list the commit subjects for phases 0-9) | | |

## Phase 1: Schema

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 1.1 | EF entities and `AppDbContext` mapping exist for all 25 base tables (no `login_history`), plus `review` and `order_item` | | |
| 1.2 | `Person` -> optional one-to-one `Patron`, `Staff`, `Performer` sharing the PK; no TPT (T6) | | |
| 1.3 | `"ORDER"` mapped exactly with `ToTable("ORDER")`; all other tables lower-case | | |
| 1.4 | `ticket_scan` is not mapped (T14) | | |
| 1.5 | `staff.employee_id` mapped as store-generated; never written | | |
| 1.6 | `Baseline` migration `Up` runs the raw SQL of `AdvDataPhase2Schema.sql`, then `new_table_updates.sql`, then sections 2-3 of `jsonb_implementation.sql`; a matching `Down` exists (spec 3.1) | | |
| 1.7 | Baseline uses a plain `CREATE TABLE review`, not `DROP TABLE IF EXISTS review` (spec 3.2) | | |
| 1.8 | Model snapshot kept from the generated migration | | |
| 1.9 | Normal migration drops `login_history` | | |
| 1.10 | 4.2 item 1: `"ORDER"` gets `status` (default `PENDING`, CHECK in PENDING/PAID/FAILED/CANCELLED), `total_amount numeric(10,2) NOT NULL` (CHECK >= 0), `payment_reference varchar(100)`, `idempotency_key varchar(100)` with unique index (nullable), and `cart_id` unique | | |
| 1.11 | 4.2 item 2: `order_item` table (`id`, `order_id` FK, `performance_seat_id` FK, `price_at_purchase numeric(10,2) NOT NULL`) | | |
| 1.12 | 4.2 item 3 indexes: `performance_seat (performance_id, status)`, `performance_seat (held_until)`, `cart (patron_id, status)`, `event (status, live_date)` | | |
| 1.13 | 4.2 item 4: backfill of the 1,519 seed orders (status `PAID`, `total_amount` from `payment.amount`, `order_item` from `cart_item` -> `ticket` -> `performance_seat`, `price_at_purchase = ticket.price`) is implemented (it may be part of seed step 5; say where) | | |
| 1.14 | `vector` extension enabled in a migration | | |
| 1.15 | `review.venue_id` NOT NULL, `body` and `rating` nullable, `embedding` nullable `vector(384)` | | |
| 1.16 | `price_tier` uniqueness (`UNIQUE NULLS NOT DISTINCT`) and `patron.preferences` CHECK + GIN index are present in the migrated schema | | |
| 1.17 | Test: a fresh database migrates from zero | | |
| 1.18 | Test: 27 tables exist (excluding the EF history table) and `login_history` is not one | | |
| 1.19 | Test: `SELECT count(*)` through EF works for every mapped entity (drift guard) | | |
| 1.20 | Test: a bad preferences document is rejected by the CHECK | | |
| 1.21 | Test: a second `performance_seat` for the same seat and performance fails | | |
| 1.22 | No `price_tier`, `ticket_scan`, or other table changed beyond 4.2 | | |

## Phase 2: Logs API (Firestore)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 2.1 | `LogsController` routes all live under `api/logs/<endpoint>` | | |
| 2.2 | Full CRUD for each of the 5 collections: `login_history`, `audit_log`, `permission_change_log`, `performance_status_log`, `error_log` (list one row per collection below) | | |
| 2.2a | `login_history` CRUD | | |
| 2.2b | `audit_log` CRUD | | |
| 2.2c | `permission_change_log` CRUD | | |
| 2.2d | `performance_status_log` CRUD | | |
| 2.2e | `error_log` CRUD | | |
| 2.3 | Models match spec 8.1 field shapes, including `changes {old?, new?}`, `context {ip_address nullable, source}`, `tags[]` (T8) | | |
| 2.4 | Optional fields (`?`) are optional in the models; timestamps are strings (T7) | | |
| 2.5 | `error_log` has an optional `timestamp` string (A13) | | |
| 2.6 | `audit_log` count per action (`INSERT`/`UPDATE`/`DELETE`) using Firestore aggregate counts | | |
| 2.7 | `GET api/logs/audit-recent?daysAgo=N` compares `changed_at` to an ISO-8601 cutoff string | | |
| 2.8 | Staff-only authorization on the Logs API (spec 7 table) | | |
| 2.9 | `tools/split_seed.py` exists; writes `postgres.sql` (without the `LOGIN_HISTORY` insert and its `setval`) and `login_history.json` (`id`, `person_id`, `ip_address`, `success`, `timestamp` as `yyyy-MM-ddTHH:mm:ssZ`); original seed file not edited | | |
| 2.10 | `SeedFirestore` flag loads 94 JSON documents + 1,200 `login_history` documents, keyed by `id`; safe to re-run | | |
| 2.11 | Test: create/read/update/delete round-trip | | |
| 2.12 | Test: action counts are 9 / 45 / 6 on the seed | | |
| 2.13 | Test: `login_history` holds 1,200 documents after seeding | | |
| 2.14 | Test: `audit-recent` count agrees with a count computed from the seed relative to today (not hardcoded) | | |
| 2.15 | Integration tests use the Firestore **emulator**, not the real database | | |

## Phase 3: Seed and catalogue (brief M2)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 3.1 | `dotnet run -- seed` exists and does **not** run on normal app start | | |
| 3.2 | Seed step 1: split (`tools/split_seed.py`) | | |
| 3.3 | Seed step 2: `postgres.sql` runs only if `person` is empty | | |
| 3.4 | Seed step 3: `theater_reviews_with_vectors.sql` only if `review` is empty; then `ALTER TABLE review ALTER COLUMN venue_id DROP DEFAULT` | | |
| 3.5 | Seed step 4: preferences `UPDATE` from `jsonb_implementation.sql` section 1, guarded by `preferences = '{}'` | | |
| 3.6 | Seed step 5: order backfill (see 1.13) | | |
| 3.7 | Seed step 6: dev accounts. One ADMIN, one STAFF, one patron-only person get passwords from user-secrets via `PasswordHasher`; emails are printed; no credentials committed (A16) | | |
| 3.8 | Seed step 7: Firestore load (see 2.10) | | |
| 3.9 | Each step inserts only into empty tables and never updates or deletes existing rows | | |
| 3.10 | `GET /events`: paged, text search, genre filter, `ACTIVE` events only, no image bytes | | |
| 3.11 | `GET /events/{id}`: details, genres, upcoming performances, cast | | |
| 3.12 | `GET /events/{id}/image` | | |
| 3.13 | `GET /genres`: active genres only | | |
| 3.14 | `GET /performances/{id}` | | |
| 3.15 | `GET /performances/{id}/seats`: section, row, seat number, status, resolved price for every seat, as a projection (not entities) | | |
| 3.16 | Seat list applies **lazy expiry**: `HELD` with `held_until < now` shows as available | | |
| 3.17 | Price resolution follows spec 6.3 (most non-NULL scope columns wins; ties go to the higher price; all-NULL default tier works) | | |
| 3.18 | Test: seeded row counts match spec 9 (400 people, 100 performers, 6 venues, 12 stages, 36 sections, 864 seats, 15 genres, 100 events, 194 performances, 13,968 performance seats, 159 price tiers, 40 promo codes, 1,561 carts, 4,687 tickets and cart items, 1,519 orders and payments, 3,782 ticket scans, 180 email-list rows, 150 password resets, 2,150 reviews) | | |
| 3.19 | Test: running the seed twice changes nothing | | |
| 3.20 | Test: seed after editing a seeded row does not overwrite the edit | | |
| 3.21 | Test: `/performances/{id}/seats` returns status and price for every seat of a seeded performance | | |
| 3.22 | Unit tests for price resolution cover each scope (section, performance, stage, event) **and** the tie-break | | |

## Phase 4: Auth (brief M3)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 4.1 | All routes are prefixed `/api` (brief 5), except Logs API, which is `api/logs` | | |
| 4.2 | `POST /auth/register` creates `person` + `patron` (role `PATRON`) in one transaction | | |
| 4.3 | `POST /auth/login` returns a JWT with short expiry; uses `PasswordHasher<Person>` (not ASP.NET Identity) | | |
| 4.4 | JWT role claim comes from `staff.role_id` if a staff row exists, otherwise the patron role (spec 6.1) | | |
| 4.5 | `GET /auth/me` | | |
| 4.6 | `POST /auth/password-reset/request` creates a `password_reset_request` and sends via a mock email sender behind an interface | | |
| 4.7 | `POST /auth/password-reset/confirm` sets the new password; request status becomes `COMPLETED`; expired/used tokens rejected | | |
| 4.8 | `POST` / `DELETE /account/email-list` join/leave | | |
| 4.9 | `ILogSink` interface with an in-memory implementation; every login attempt (success **and** failure) goes through it | | |
| 4.10 | Roles `PATRON`/`STAFF`/`ADMIN` enforced via `[Authorize]`; service-layer validation that `patron.role_id` = `PATRON` and `staff.role_id` in (`STAFF`,`ADMIN`) | | |
| 4.11 | Every staff member gets a patron row, created in the same transaction if missing | | |
| 4.12 | Rate limiting (`AddRateLimiter`) on login and password reset | | |
| 4.13 | CORS configured explicitly; HTTPS enforced | | |
| 4.14 | Passwords and hashes are never returned or logged | | |
| 4.15 | Test: register | | |
| 4.16 | Test: login success and failure; both reach `ILogSink` | | |
| 4.17 | Test: a seeded person with an unusable hash gets **401, not 500** | | |
| 4.18 | Test: the dev accounts can log in | | |
| 4.19 | Test: wrong-type role is rejected | | |
| 4.20 | Test: patron A cannot read patron B's data | | |
| 4.21 | Test: admin routes return 403 for patrons | | |

## Phase 5: Holds (brief M4, the core)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 5.1 | `POST /performances/{id}/holds` (body: seat IDs; Patron) | | |
| 5.2 | `GET /holds/current` returns the active hold and seconds remaining | | |
| 5.3 | `DELETE /holds/{id}` releases the patron's own hold (`{id}` = cart id); another patron's hold is refused | | |
| 5.4 | Hold uses **approach B**: a single atomic conditional `UPDATE` (via raw SQL) in a transaction; affected rows compared to requested count; fewer means rollback and 409 | | |
| 5.5 | Taking over an expired hold deletes the stale `ticket` and `cart_item` in the same transaction before creating new ones | | |
| 5.6 | Hold creates: `cart` (`ACTIVE`), one `ticket` per seat (price copied from the resolved price tier at hold time), one `cart_item` per seat, `held_until` = now + 10 minutes using DB/server UTC time | | |
| 5.7 | A patron has at most one `ACTIVE` cart; all seats are for one performance | | |
| 5.8 | Holding different seats releases the old hold first | | |
| 5.9 | Max seats per hold is configurable, default 8 | | |
| 5.10 | Hold rejected for: non-`SCHEDULED` performance, past `date_time`, `UNAVAILABLE` seats, seat IDs that do not belong to the performance, empty seat list, oversized request | | |
| 5.11 | **Lazy expiry**: every hold attempt treats `HELD` + `held_until < now` as available | | |
| 5.12 | Background cleanup (`BackgroundService`, every 30-60 s, uses `IServiceScopeFactory`): resets expired seats to `AVAILABLE`, clears `held_until`, deletes their `ticket` and `cart_item`, marks the cart `ABANDONED` (A4) | | |
| 5.13 | Correctness does not depend on the cleanup job running | | |
| 5.14 | Structured `ILogger` logging for every hold, expiry, and conflict | | |
| 5.15 | 409 Conflict returned when a seat is already taken | | |
| 5.16 | Test: 20 simultaneous hold requests for one seat give exactly one 200 and nineteen 409s (real Postgres, not in-memory EF) | | |
| 5.17 | Test: an expired hold makes the seat available again; its ticket and cart item are gone; cart is `ABANDONED` (uses fake `TimeProvider`) | | |
| 5.18 | Test: holding different seats releases the old hold | | |
| 5.19 | Test: holds for a non-`SCHEDULED` performance are rejected | | |

## Phase 6: Checkout and promo codes (brief M5)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 6.1 | `POST /orders` with optional `promoCode`, `Idempotency-Key` header, `method`, and `cardNumber` | | |
| 6.2 | Checkout runs in one transaction: loads the patron's `ACTIVE` cart and re-verifies all seats are `HELD` with `held_until > now` inside the transaction | | |
| 6.3 | `xmin` optimistic concurrency used on the checkout step (brief 6) | | |
| 6.4 | Mock payment service behind an interface (`IPaymentService`); spaces and dashes in the card number ignored; `4545454545454545` succeeds; any other number is declined | | |
| 6.5 | Declined card returns **402**, creates **no order**, seats stay held | | |
| 6.6 | Card number is never stored, logged, or returned (grep the code, logs, DTOs, and exception messages) | | |
| 6.7 | `payment.method` validated against the 7 allowed values (T10); successful `payment_reference` is `MOCK-<guid>` | | |
| 6.8 | Promo code lookup is case-insensitive; rejected if not found, inactive, expired, or both `d_off` and `p_off` are null | | |
| 6.9 | Totals use ticket price **snapshots**; `d_off` flat or subtotal x `p_off` (T3); floor at zero; rounded to 2 decimals; all `decimal` (T16) | | |
| 6.10 | On success: seats `SOLD`, cart `CHECKED_OUT`, `"ORDER"` row (`PAID`, total, `payment_reference`, `promo_code_id`, `cart_id`), `order_item` rows (`price_at_purchase` = ticket price), `payment` row (`amount` = total) | | |
| 6.11 | `performance.filled` flips to true in the same transaction when no `AVAILABLE`/`HELD` seats remain, and a `performance_status_log` entry is written (system change: no `changed_by`, `new` = `SOLD_OUT`) | | |
| 6.12 | Idempotency: retrying with the same key returns the original order, not a second one | | |
| 6.13 | `GET /orders` (own orders) and `GET /orders/{id}` (owner only, resolved via `order -> cart -> patron`) | | |
| 6.14 | Edge cases handled: hold expired mid-checkout; patron checks out seats held by someone else; no active hold | | |
| 6.15 | Promo management for Staff: `POST`/`PUT /admin/promo-codes` | | |
| 6.16 | Unit tests: `d_off`, `p_off` (fraction), floor at zero, 2-decimal rounding | | |
| 6.17 | Test: inactive, expired, and empty promo codes are rejected | | |
| 6.18 | Test: declined card creates no order and leaves seats held | | |
| 6.19 | Test: retrying with the same `Idempotency-Key` returns the original order | | |
| 6.20 | Test: `filled` flips when the last seat sells | | |
| 6.21 | Test: a patron cannot read another patron's order | | |
| 6.22 | Test or grep evidence: the card number appears in no log or response | | |

## Phase 7: Admin endpoints

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 7.1 | `POST`/`PUT`/`DELETE /admin/events` (status values per spec 4.1; image handled without being returned in lists) | | |
| 7.2 | `POST`/`PUT /admin/genres` | | |
| 7.3 | `POST /admin/performers` creates `person` + `patron` + `performer` with a random unusable password hash (A9) | | |
| 7.4 | `POST /admin/venues` | | |
| 7.5 | `POST /admin/venues/{id}/stages` | | |
| 7.6 | `POST /admin/stages/{id}/sections` | | |
| 7.7 | `POST /admin/sections/{id}/seats` (bulk: rows and seat numbers) | | |
| 7.8 | `POST /admin/performances`: one transaction creates the performance, one `performance_seat` per seat of its stage, and the (performance, section) price tiers from a price per section | | |
| 7.9 | A section from another stage is rejected | | |
| 7.10 | `PUT /admin/performances/{id}/cast` | | |
| 7.11 | `PUT /admin/people/{id}/role` is **Admin only**: to `STAFF`/`ADMIN` creates the staff row if missing (requires `assigned_venue`, no default; A11), to `PATRON` deletes the staff row (A8); writes a `permission_change_log` entry | | |
| 7.12 | All admin routes are Staff-or-Admin protected; creates return 201 with `Location` | | |
| 7.13 | Test: creating a performance generates one `performance_seat` per stage seat and the (performance, section) tiers | | |
| 7.14 | Test: only Admin can change roles (Staff gets 403) | | |

## Phase 8: Log sink wiring (brief M6)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 8.1 | In-memory `ILogSink` is **replaced** by a `Channel<T>` sink + Firestore `BackgroundService` writer | | |
| 8.2 | Writer retries a few times, then falls back to `ILogger`; a Firestore failure never fails or delays a request | | |
| 8.3 | `login_history`: every login attempt, success or failure (`timestamp` string, not `login_timestamp`) | | |
| 8.4 | `error_log`: unhandled exceptions from the global handler (with optional `timestamp`, A13) | | |
| 8.5 | `audit_log`: **every** Staff/Admin change (list which admin endpoints from Phase 7 do and do not write one) | | |
| 8.6 | `audit_log` `changes` excludes `event.image`; INSERT has only `new`, DELETE only `old` | | |
| 8.7 | `permission_change_log`: every role change | | |
| 8.8 | `performance_status_log`: status changes by staff **and** the `filled` flip | | |
| 8.9 | App-written entries use real table names and upper-case status values (spec 8.1 last bullet) | | |
| 8.10 | No password, hash, JWT, or card number is ever written to a log document | | |
| 8.11 | New log documents get an integer `id` (state how ids are assigned) | | |
| 8.12 | Test: each of the five log types produces the expected Firestore document (emulator) | | |
| 8.13 | Test: a Firestore outage does not fail or delay a request | | |

## Phase 9: Preferences, points, reviews

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 9.1 | `GET`/`PATCH /patrons/me/preferences` (Patron) | | |
| 9.2 | Preferences DTO enforces the same rules as the DB CHECK (spec 6.5 shape; `reminder_hours_before` 1-168; `content_advisories` array of strings); bad input returns **400, not a DB error** | | |
| 9.3 | `GET /admin/patrons?wheelchair=true` uses a JSONB containment query (Staff) | | |
| 9.4 | `POST /admin/patrons/{id}/points` (Staff) | | |
| 9.5 | One points service method using an atomic `UPDATE ... SET points_balance = points_balance + @d WHERE id = @id AND points_balance + @d >= 0`; zero rows means missing patron or would go negative (A6) | | |
| 9.6 | `person_status` implemented as a C# service method (A6) | | |
| 9.7 | Review CRUD: `POST`/`GET`/`PUT`/`DELETE /reviews`; create/edit/delete require a person; reads are Public | | |
| 9.8 | Review rating validated 1-5 in the DTO; `venue_id` always supplied by the API | | |
| 9.9 | Only the owner can edit or delete their review | | |
| 9.10 | `embedding` stored as NULL (Phase 10 not yet confirmed) | | |
| 9.11 | Test: containment filter returns the **3** wheelchair patrons on the seed | | |
| 9.12 | Test: invalid preferences return 400 | | |
| 9.13 | Test: a points change below zero is rejected | | |
| 9.14 | Test: review create/edit/delete works; a non-owner cannot edit | | |

## Phase 10 (optional, needs A7): Semantic search

Expected not started. Record the actual state so we know the starting point.

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 10.1 | `POST /reviews/search` | | |
| 10.2 | Query embedding with `all-MiniLM-L6-v2` (384 dims, unit length) | | |
| 10.3 | Embedded text is title + body, title first | | |
| 10.4 | Seed review vectors regenerated with title + body | | |
| 10.5 | `ORDER BY embedding <=> $1 LIMIT k`; filter-first CTE for filtered searches | | |
| 10.6 | Embeddings regenerated on review edit | | |
| 10.7 | Query plan uses `idx_review_embedding` for an unfiltered query | | |

## Phase 11: Delivery (brief M7)

Expected not started. Record the actual state.

| # | Item | Status | Evidence |
|---|------|--------|----------|
| 11.1 | Dockerfile exists and builds | | |
| 11.2 | All configuration comes from environment variables (Neon string, JWT key, Firestore project and credentials) | | |
| 11.3 | Migrations and seeding are manual steps, not automatic on start | | |
| 11.4 | README: setup, secrets, migrate, seed, Swagger, demo script | | |
| 11.5 | Neon retry policy (`EnableRetryOnFailure`) for cold starts | | |

---

## Cross-cutting checks (apply to all phases)

| # | Item | Status | Evidence |
|---|------|--------|----------|
| X1 | Controllers contain no business logic; services sit behind interfaces registered in DI | | |
| X2 | Controllers return DTOs, never EF entities | | |
| X3 | No `password` field and no `image` bytes appear in any list response | | |
| X4 | Async end to end; `CancellationToken` passed through where practical | | |
| X5 | Status codes: 201 + `Location` on create, 400 validation, 401/403 auth, 404 missing, 409 seat taken | | |
| X6 | `AsNoTracking` on read queries; projections used for the seat list | | |
| X7 | Event list is paginated | | |
| X8 | Every order/hold endpoint checks ownership; every admin route checks role | | |
| X9 | All times are UTC; no client-supplied time is trusted | | |
| X10 | Integration tests run against real Postgres (not EF in-memory) for any seat logic | | |
| X11 | No unrequested features were added (list anything in the code that is not in the plan or spec) | | |

---

## Final report (write at the end of `docs/audit.md`)

### Summary
| Phase | DONE | PARTIAL | MISSING | DIVERGES | N/A |
|-------|------|---------|---------|----------|-----|
| Brief traps | | | | | |
| 0 | | | | | |
| 1 | | | | | |
| 2 | | | | | |
| 3 | | | | | |
| 4 | | | | | |
| 5 | | | | | |
| 6 | | | | | |
| 7 | | | | | |
| 8 | | | | | |
| 9 | | | | | |
| 10 | | | | | |
| 11 | | | | | |
| Cross-cutting | | | | | |

### Test run
`dotnet build` / `dotnet test` result, and the names of any failing or skipped tests.

### Gaps, in the order they should be fixed
List every `MISSING`, `PARTIAL` and `DIVERGES` row, earliest phase first. For each give: row number, one line on what is wrong, and the spec/plan reference that makes it required. Put anything that breaks correctness first (double-booking, money, auth, data loss), then missing tests, then polish.

### Things I could not verify
Rows marked `PARTIAL` only because they need a live database or emulator, and what would be needed.

### Assumptions the code made that are not in `docs/assumptions.md`
