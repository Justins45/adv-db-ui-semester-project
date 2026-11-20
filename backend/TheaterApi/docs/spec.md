# Spec - TheaterApi

The single source of truth for what to build. It merges the Phase 1-3 deliverables, the SQL files, the decision log, and the older API brief.
Tags: **[D]** decision log (`updates.md`), **[SQL]** the SQL files, **[B]** the API brief, **[P3]** Phase 3 deliverables, **[A]** assumed default awaiting confirmation (section 10).

## 0. How to read the sources

If sources disagree, the higher one wins:

1. This spec
2. The SQL files: `AdvDataPhase2Schema.sql`, `new_table_updates.sql`, `jsonb_implementation.sql` (the implemented schema), and the seed files in section 9
3. The API brief `theater-ticketing-api-brief.md` (older plan)
4. Phase 3 reports and design plan, Phase 2 reports
5. Phase 1 relational schema and EERD

The API brief was written before most of the other files. Use it for the parts it is strongest on:
tech stack and decisions (its section 2), architecture rules (3), routes (5), the seat-hold and checkout algorithm (6), non-functional requirements (8), tests (9), deployment (10).
Its **data model (section 4) is superseded** by the SQL files and section 4-5 below.

## 1. Product
Ticketing API for one theater venue per deployment. Patrons browse events, hold seats for 10 minutes, and check out with an optional promo code. Staff and admins manage the catalogue. Each deployment has its own Neon database, server, and Firestore database; no tenant column anywhere. Canada only (CAD, Canadian addresses and phones). **[D][B]**

## 2. Closed decisions
Do not change these.

| # | Decision | Source |
|---|----------|--------|
| 1 | EF Core stays; ASP.NET Core controllers (not Minimal APIs) | [D][B] |
| 2 | Auth: JWT + `PasswordHasher<Person>` on the existing `person` table; not ASP.NET Identity | [B] |
| 3 | Login/auth is in scope, with the EERD's auth tables | [D] |
| 4 | Seat maps are out; seats live in Postgres | [D] |
| 5 | Ticket scanning is out of the API (the `ticket_scan` table exists but is not mapped or exposed) | [D][B] |
| 6 | Seed data is the base; anything users alter stays; seeding is a deliberate step, never on app start | [D][B] |
| 7 | Promo codes are in | [D] |
| 8 | Hold model follows the EERD: `performance_seat` (status, held_until) -> `ticket` -> `cart_item` -> `cart` | [D][B] |
| 9 | Money is `numeric(10,2)` / `decimal`; never float | [D] |
| 10 | Order model and status follow the API brief (adds columns and `order_item`; see 4.2) | [D] |
| 11 | Logs live in Firestore: five collections (`login_history`, `audit_log`, `permission_change_log`, `performance_status_log`, `error_log`). The Postgres `login_history` table is dropped | [D][B] |
| 12 | A Logs API (CRUD over the Firestore logs) is in scope. This overrides the brief's "no log viewer endpoints" | [D] |
| 13 | Person hierarchy is overlapping: every person is a patron; staff also have a patron row. This overrides the brief's "disjoint subtypes" | [P3] |
| 14 | Only `patron.preferences` is JSONB. No venue JSONB, no favourite venues | [P3] |
| 15 | Only reviews are vectorized (title + body); reviews belong to venues | [P3] |
| 16 | The schema is 3NF. Canada only | [D] |
| 17 | `promo_code.p_off` is a fraction from 0 to 1 (0.15 = 15%). Any percent wording in the older docs is wrong | [D][SQL] |
| 18 | Every role and status value is upper-case | [D] |
| 19 | `price_tier` keeps all four scopes (section, performance, stage, event) | [D][SQL] |
| 20 | Payment is mocked by card number: `4545 4545 4545 4545` succeeds, any other number is declined | [D] |

## 3. Data stores and the schema baseline

**PostgreSQL (Neon), source of truth.** Tables come from `AdvDataPhase2Schema.sql` (26 tables, minus `login_history`, which moves to Firestore), then `new_table_updates.sql` (Phase 3 columns and `review`), then the changes in 4.2. Result: 27 tables.
**Firestore, logs only.** Collections: `login_history`, `audit_log`, `permission_change_log`, `performance_status_log`, `error_log`. Firestore never overrides Postgres.

### 3.1 How to build the schema (migrations)
Start from a fresh Neon database. Create the EF models by hand to match the SQL, then:
1. `dotnet ef migrations add Baseline`. Keep the generated model snapshot, but replace the migration's `Up` with the raw SQL of `AdvDataPhase2Schema.sql`, then `new_table_updates.sql`, then sections 2-3 of `jsonb_implementation.sql` (the CHECK constraint and GIN index; sections 1, 4, 5 are sample data and demo queries). See the gotchas below. Write a matching `Down`.
2. A normal migration drops `login_history` (it lives in Firestore, section 8).
3. Every later change is a normal EF migration.
Why: the SQL already has the exact CHECKs, defaults, uniques, and indexes, and it is what the class tested. Rebuilding it in Fluent config risks drift. The cost is that models and SQL can drift; the Phase 1 integration test (plan) guards against it.

### 3.2 Gotchas in the SQL files
- `"ORDER"` is created quoted and upper-case. Map it exactly: `ToTable("ORDER")`. Every other table is created unquoted, so it is lower-case.
- `new_table_updates.sql` is not safe to re-run: its `ALTER TABLE ... ADD` lines have no `IF NOT EXISTS`, so a second run fails at the first one, and its review section starts with `DROP TABLE IF EXISTS review`, which wipes reviews and embeddings. It runs once, inside the baseline migration; use a plain `CREATE TABLE review` there. (`theater_reviews_with_vectors.sql` and `jsonb_implementation.sql` sections 2-3 are re-runnable.)
- `staff.employee_id` is a generated stored column (`'SFF_' || id`). Map it as store-generated; never write to it.
- `staff.assigned_venue` is NOT NULL. Creating a staff row needs a venue (see A11).
- `price_tier` uses `UNIQUE NULLS NOT DISTINCT` (Postgres 15+; fine on Neon).
- `patron.preferences` defaults to `'{}'` and is guarded by `validate_patron_preferences_jsonb` (see 6.5).
- `review.body` and `review.rating` are nullable, `review.embedding` is nullable `vector(384)`.
- The `new_table_updates.sql` comments show an older `person` shape. Trust `AdvDataPhase2Schema.sql` (`phone` is `NOT NULL UNIQUE`, `password` is `NOT NULL`).
- `CREATE EXTENSION vector` must be enabled on Neon.

## 4. Schema reference and required changes

### 4.1 Values fixed by the SQL (use exactly these)
| Column | Allowed values |
|--------|----------------|
| `role.role` | `ADMIN`, `STAFF`, `PATRON` |
| `cart.status` | `ACTIVE` (default), `CHECKED_OUT`, `ABANDONED` |
| `performance.status` | `DRAFT`, `SCHEDULED` (default), `CANCELLED`, `COMPLETED` |
| `performance_seat.status` | `AVAILABLE` (default), `HELD`, `SOLD`, `UNAVAILABLE` |
| `password_reset_request.status` | `PENDING` (default), `COMPLETED`, `EXPIRED`, `FAILED` |
| `payment.method` | `CREDIT_CARD`, `DEBIT_CARD`, `PAYPAL`, `APPLE_PAY`, `GOOGLE_PAY`, `MASTERCARD`, `VISA` |
| `promo_code.p_off` | 0 to 1 (a fraction: 0.15 = 15%) |
| `event.status` | `DRAFT` (default), `ACTIVE` (the live value the seed uses), `CANCELLED` (allowed, not seeded). The brief's `Live` is `ACTIVE` |

### 4.2 Migrations the API needs on top of the baseline **[D]**
1. `"ORDER"`: add `status varchar(50) NOT NULL DEFAULT 'PENDING'` with CHECK in (`PENDING`, `PAID`, `FAILED`, `CANCELLED`); `total_amount numeric(10,2) NOT NULL` (CHECK >= 0); `payment_reference varchar(100)`; `idempotency_key varchar(100)` with a unique index (nullable); make `cart_id` unique.
2. New table `order_item`: `id`, `order_id` (FK to `"ORDER"`), `performance_seat_id` (FK), `price_at_purchase numeric(10,2) NOT NULL`.
3. Indexes from the brief's section 8 not already in the SQL: `performance_seat (performance_id, status)`, `performance_seat (held_until)`, `cart (patron_id, status)`, `event (status, live_date)`.
4. Backfill existing seed orders (1,519): set `status = 'PAID'`, set `total_amount` from the order's `payment.amount`, and create `order_item` rows from `cart_item` -> `ticket` -> `performance_seat` with `price_at_purchase = ticket.price`.
Do not change `price_tier`, `ticket_scan`, or any other table.

## 5. Where the brief and the SQL disagree (resolved)
| Topic | Brief (old) | SQL / newer source | Resolution |
|-------|-------------|--------------------|------------|
| Person subtypes | disjoint, exactly one | overlapping; every person has a patron row | SQL/P3. Do not use EF table-per-type inheritance (a person can be a patron and staff at once); map `Person` with optional one-to-one `Patron`, `Staff`, `Performer` sharing the PK |
| Role values | `Patron`, `Staff`, `Admin` | `PATRON`, `STAFF`, `ADMIN` | SQL |
| Cart status | `Active`, `CheckedOut`, `Expired` | `ACTIVE`, `CHECKED_OUT`, `ABANDONED` | SQL. Expired holds mark the cart `ABANDONED` |
| Reset status | `Requested`... | `PENDING`... | SQL |
| `promo_code.p_off` | percent 0-100, discount = subtotal x p_off / 100 | fraction 0-1 (CHECK) | SQL. Discount = subtotal x p_off |
| `price_tier` | only (section, performance) | four scopes: section, performance, stage, event | SQL. Resolution rule in 6.3 |
| `payment.method` | `Mock` | fixed list in 4.1 | Request supplies one of the allowed values plus a card number; the payment service is a mock (6.4) |
| `person.password` | nullable (performers) | `NOT NULL` | SQL. Performers get a random hash they cannot use (A9) |
| `login_history` | Firestore | Postgres table with `login_timestamp` (1,200 seed rows) | Firestore. The Postgres table is dropped; the seed rows become Firestore documents (section 9) |
| Firestore log shapes | `old_value`/`new_value` JSON strings, top-level `ip_address`, Firestore `Timestamp`s | seed uses `changes{old,new}`, `context{ip_address,source}`, `tags`, ISO-8601 strings | Seed shape wins **[D]** (see 8) |
| Log viewer | out of scope | Logs API in scope | Logs API in scope |
| Triggers | none (app-level concurrency) | Phase 2 portfolio had triggers/procedure/view (not in the schema SQL) | Not installed. The API uses the brief's app-level hold logic (6.2) |
| `performance.status` | not present | present | Use the SQL; seats are only holdable for `SCHEDULED` performances in the future |

## 6. Business rules

### 6.1 People and roles
- `person` is the base. `patron`, `staff`, `performer` share `person.id`. Register creates `person` + `patron` (role `PATRON`).
- Every staff member also has a patron row; create it in the same transaction if missing.
- `patron.role_id` must reference `PATRON`; `staff.role_id` must reference `STAFF` or `ADMIN`. `role` has no type column, so validate in the service layer.
- JWT claims: role comes from `staff.role_id` if a staff row exists, otherwise the patron role.
- Role change (`PUT /admin/people/{id}/role`, Admin only): to `STAFF`/`ADMIN` creates the staff row if missing (needs `assigned_venue`) and sets `staff.role_id`; to `PATRON` deletes the staff row **(A8)**. Always writes `permission_change_log`.
- Passwords: hash only; never log or return them.
- Every login attempt, success or failure, writes a `login_history` document to Firestore through the log sink.

### 6.2 Seat holds, expiry, checkout
Follow the brief's section 6 exactly: approach B (atomic conditional `UPDATE`) for holds, `xmin` optimistic concurrency at checkout, lazy expiry plus a background cleanup job, idempotent checkout, and the listed edge cases. Adjusted for the SQL:
- Status values and cart statuses from 4.1. Cleanup marks carts `ABANDONED`.
- A hold is allowed only for a `SCHEDULED` performance whose `date_time` is in the future, and never for `UNAVAILABLE` seats.
- The ticket price is copied from the resolved price tier (6.3) at hold time.
- A patron has at most one `ACTIVE` cart; all its seats are for one performance. Max seats per hold is configurable (default 8).
- Checkout creates the `"ORDER"` (`PAID`), `order_item` rows, and a `payment` row. When no `AVAILABLE`/`HELD` seats remain, set `performance.filled = true` in the same transaction and log a `performance_status_log` entry (A12).
- A declined payment (6.4) returns 402 and creates no order; the seats stay held until they expire.
- Do not install the Phase 2 portfolio triggers, procedure, or view.
- All times come from the database/server UTC clock; inject `TimeProvider`.

### 6.3 Pricing
A `price_tier` row has up to four scope columns (`section_id`, `performance_id`, `stage_id`, `event_id`); a NULL column means "any". A tier applies to a seat when every non-NULL column matches that seat's section, performance, stage, and event. The winner is the applying tier with the most non-NULL columns; if tied, the higher price wins. This is the rule the seed generator used to price the seeded tickets, so the API reproduces the seeded prices. The seed contains one all-NULL default tier (45.00), so every seat has a price. Admin performance creation takes a price per section and writes (performance, section) tiers. A service check ensures a section belongs to the performance's stage.

### 6.4 Checkout totals
subtotal = sum of ticket prices. Promo: must exist (case-insensitive), be `active`, and not past `expiry_date`. It gives `d_off` (flat) or `p_off` (fraction); reject a code with neither. Discount = `d_off`, or subtotal x `p_off`. total = max(subtotal - discount, 0), rounded to 2 decimals. Payment `amount` = total.

Mock payment: the checkout request carries `method` (one of the allowed values in 4.1) and `cardNumber`. Ignoring spaces and dashes, `4545454545454545` succeeds and any other number is declined (HTTP 402, no order created). Expiry and CVV are not checked. The card number is never stored, logged, or returned. A successful payment's `payment_reference` is `MOCK-<guid>`.

### 6.5 Patron preferences (JSONB)
Exact shape enforced by `validate_patron_preferences_jsonb` (`{}` or all of the following):
```json
{
  "accessibility": { "wheelchair_accessible": true, "companion_seat_required": false },
  "notifications": { "event_reminders": true, "reminder_hours_before": 24, "price_drop_alerts": false },
  "content_advisories": ["Flashing Lights", "Loud Noises"]
}
```
`reminder_hours_before` is a number from 1 to 168; `content_advisories` is an array of strings.
Validate DTOs to the same rules so bad input returns 400, not a database error. GIN index `idx_patron_preferences_gin` already exists.
Patron fields: `loyalty_tier` (nullable), `points_balance` (>= 0), `membership_expiry`. Change the balance only through one service method using an atomic `UPDATE ... SET points_balance = points_balance + @d WHERE id = @id AND points_balance + @d >= 0`; zero rows updated means the patron is missing or the balance would go negative. The DB functions `add_points` and `person_status` from the Phase 3 report are not in the SQL files; implement them in C# **(A6)**.

### 6.6 Reviews and embeddings
- A review belongs to a venue (`venue_id` NOT NULL) and a person. Any person can write and search reviews. Validate `rating` 1-5 in the DTO (the table has no CHECK).
- Embedded text: `title` + `body` joined, title first (title only if the body is empty). Model `sentence-transformers/all-MiniLM-L6-v2`, 384 dims, unit-length vectors.
- Search: `ORDER BY embedding <=> $1 LIMIT k` (cosine; required for the HNSW index `idx_review_embedding`). HNSW filters after searching, so for selective filters use a CTE that filters first, then ranks.
- Vectors are derived: regenerate on edit or model change.
- Whether the API exposes review search is open **(A7)**.

## 7. API surface
Use the brief's section 5 routes. Additions:

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET/PATCH | /patrons/me/preferences | Patron | Read / replace preferences |
| GET | /admin/patrons?wheelchair=true | Staff | Filter patrons by preference (containment query) |
| POST | /admin/patrons/{id}/points | Staff | Add or remove loyalty points |
| POST / GET / PUT / DELETE | /reviews | Patron / Public | Review CRUD (owner edits/deletes their own) |
| POST | /reviews/search | Public | Semantic search, only if A7 is confirmed |
| * | /api/logs/... | Staff | Logs API, see 8.2 |

## 8. Firestore logs

### 8.1 Document shapes (from `firestore_logs_seed.json`; `login_history` is generated from the SQL seed)
Each document has an integer `id`; the Firestore document id can be that `id` as a string. Fields marked `?` are optional, and the seed omits them on some documents (the model must allow it).

| Collection | Fields |
|------------|--------|
| `audit_log` | `id`, `staff_id`, `table_name`, `record_id`, `action` (`INSERT`/`UPDATE`/`DELETE`), `changed_at`, `changes {old?, new?}` (INSERT has only `new`, DELETE only `old`), `context {ip_address (nullable), source}`, `tags[]` |
| `permission_change_log` | `id`, `person_id`, `changed_by?`, `old_role?`, `new_role`, `date_time` |
| `performance_status_log` | `id`, `performance_id`, `changed_by?`, `previous?`, `new`, `date_time` |
| `error_log` | `id`, `person_id?`, `end_point`, `error_message`, `stack_trace?` |
| `login_history` | `id`, `person_id`, `ip_address`, `success`, `timestamp` (the SQL column was `login_timestamp`) |

- All timestamps are ISO-8601 UTC **strings** in the form `yyyy-MM-ddTHH:mm:ssZ` (all seed values match). Keep new documents in the same format and type. Do not mix strings and Firestore `Timestamp`s: range queries only match values of the same type. This deliberately differs from the brief.
- `error_log` has no timestamp in the seed. New app-written error documents add an optional `timestamp` (same string format) **(A13)**.
- Never log passwords, hashes, or JWTs. `changes` must exclude `event.image`.
- Seed sample values do not always match the live schema (`table_name` is `orders`/`persons`/`holds`...; performance statuses are lower-case and include `on_sale`/`sold_out`/`postponed`; `permission_change_log` role ids go up to 5 but `role` has 3 rows). Treat log strings as free text; app-written entries use real table names and the upper-case status values from 4.1.

### 8.2 Logs API **[D]**
- `LogsController`, every route under `api/logs/<endpoint>`.
- Full CRUD over the five collections; request/response payloads match 8.1.
- Remove the connection-test GET/POST.
- Count of `audit_log` documents per `action` (`INSERT`, `UPDATE`, `DELETE`), using Firestore aggregate counts. Seed: 9 / 45 / 6.
- `GET api/logs/audit-recent?daysAgo=N`: audit logs from the last N days. Compare `changed_at` to an ISO-8601 cutoff string.
- Seed dates run from 2026-09-14 to 2026-10-18, so an empty result is correct for small N. As of 2026-11-18: `daysAgo=32` returns 1, `45` returns 13, `66` returns all 60. Tests should compute expected counts from the seed relative to the current date, not hardcode them.
- Seeding: the `SeedFirestore` flag; re-running is safe (documents are overwritten by id). The JSON has 94 documents (10 permission, 60 audit, 10 performance status, 14 error); `login_history` adds 1,200 documents produced from the SQL seed (section 9). Total 1,294.

### 8.3 App-written logs (per the brief) **(A12)**
Services call an `ILogSink` that queues entries to a `Channel<T>`; a background writer saves them. A Firestore failure never fails or delays a request.
- `login_history`: every login attempt, success or failure
- `error_log`: unhandled exceptions (global handler)
- `audit_log`: every Staff/Admin change
- `permission_change_log`: role changes
- `performance_status_log`: status changes by staff, and `filled` flips (system change: no `changed_by`, `new` = `SOLD_OUT`)

## 9. Seed data
Seed files live in `seed/` (not `docs/`): `AdvDataPhase2SeedData.sql`, `theater_reviews_with_vectors.sql`, `firestore_logs_seed.json`, `generate_seed_data.py`. They are thousands of lines of inserts. Never open them in full; inspect with `head`, `grep`, `wc`. Do not copy their contents into docs.

Seeding is a deliberate step (`dotnet run -- seed`), runs only into empty tables, and never updates or deletes existing rows. **[D][B]**
1. **Split:** `tools/split_seed.py` reads `AdvDataPhase2SeedData.sql` and writes `postgres.sql` (everything except the `LOGIN_HISTORY` insert and its `setval`) and `login_history.json` (`id`, `person_id`, `ip_address`, `success`, `timestamp` = `login_timestamp` as `yyyy-MM-ddTHH:mm:ssZ` UTC). The original file is not edited.
2. **Postgres:** run `postgres.sql` only if `person` is empty. It is one transaction with fixed ids and resets every sequence.
3. **Reviews:** run `theater_reviews_with_vectors.sql` only if `review` is empty (it is re-runnable). It sets `review.venue_id` default to 1, so afterwards run `ALTER TABLE review ALTER COLUMN venue_id DROP DEFAULT`; the API must always supply a venue.
4. **Preferences:** apply the section 1 `UPDATE` of `jsonb_implementation.sql` (guarded by `preferences = '{}'`).
5. **Orders:** backfill per 4.2 item 4.
6. **Dev accounts:** see below.
7. **Firestore:** load the JSON collections and `login_history.json`, keyed by `id`.

What the seed contains (dataset "today" is 2026-10-15):
- Postgres, after the `login_history` move: 35,364 rows in 25 tables. 400 people (45 staff: 8 admins and 37 staff; every person is also a patron), 100 performers, 6 venues, 12 stages, 36 sections, 864 seats, 15 genres, 100 events, 194 performances, 13,968 performance seats, 159 price tiers, 40 promo codes, 1,561 carts, 4,687 tickets and cart items, 1,519 orders and payments, 3,782 ticket scans, 180 email-list rows, 150 password resets
- Event status is `DRAFT` or `ACTIVE`. Performances before 2026-10-15 are `COMPLETED`, later ones `SCHEDULED`; some are `CANCELLED` or `DRAFT`
- Because the real date is later, some seeded `SCHEDULED` performances are already in the past (holds require a future `date_time`), and seeded `HELD` seats have `held_until` around 2026-10-15, so they are already expired. Lazy expiry treats them as available and the first cleanup run releases them
- Sample preferences are on the first 8 patrons by id (3 need wheelchair seating, 4 have a "Flashing Lights" advisory)
- Reviews: 2,150 from 399 reviewers, all with `venue_id` 1 (the load script's fix) although 6 venues exist. Vectors are 384-dim and were built from the review body only: reviews that share a body share a vector even when their titles differ (2,150 title/body pairs but 1,423 distinct vectors). The file header's "title vectors" label is wrong. The new review API embeds title + body, so Phase 10 regenerates the seed vectors

Passwords: all 400 seeded passwords are random strings shaped like bcrypt hashes (`$2b$12$...`). No plaintext exists and none will verify. Logging in as a seeded person must return a clean 401, not throw. For dev, the seed step sets the password of one ADMIN, one STAFF, and one patron-only person from user-secrets values using `PasswordHasher`, and prints their emails **(A16)**. Never commit credentials.

## 10. Assumed defaults (confirm or replace)
| # | Default | Why |
|---|---------|-----|
| A4 | Hold expiry cleanup runs every 30-60 s in a `BackgroundService` | The brief's design |
| A6 | `add_points` and `person_status` are C# service methods, not DB functions | They are not in the SQL files |
| A7 | Review CRUD is in scope; semantic search is the last, optional phase | Needs a query-embedding method in .NET (in-process ONNX or a small Python service) |
| A8 | Demoting to `PATRON` deletes the staff row | Keeps one source of truth for role |
| A9 | Creating a performer makes `person` + `patron` + `performer`, with a random unusable password hash | `password` is NOT NULL |
| A11 | Creating a staff row requires `assigned_venue` in the request; no default | 6 seed venues, so no obvious default |
| A12 | App-written logging as in 8.3 | The collections would otherwise only hold seed data |
| A13 | Optional `timestamp` string on new `error_log` documents | A log needs a time; the seed lacks one |
| A14 | Keep the target framework `TheaterApi.csproj` already uses; do not upgrade it unasked | Avoids a surprise migration |
| A15 | The passing card number is 16 digits, `4545454545454545` (spaces and dashes ignored); a decline returns 402 and creates no order | Follows the brief's checkout step 6 |
| A16 | Dev accounts: set passwords on one seeded ADMIN, one STAFF, and one patron-only person from user secrets | No seeded password has a known plaintext |

## 11. Source map and what is still missing
Copy into `docs/sources/`: `theater-ticketing-api-brief.md` (rename from `...-updated_1.md`), `AdvDataPhase2Schema.sql`, `new_table_updates.sql`, `jsonb_implementation.sql`, `phase-1-relational-schema.md`, `phase-1-EERD.png`, `phase-2-readme.md`, `phase-2-implementation_Report.md`, `Phase_3_-_DB_-_Design_Plan.pdf`, `Phase3_Technical_Report.pdf`.
Copy into `seed/` (data, not docs): `AdvDataPhase2SeedData.sql`, `theater_reviews_with_vectors.sql`, `firestore_logs_seed.json`, `generate_seed_data.py`.
Documentation only, not build inputs: `uml_class_diagram.*`, `data_pipeline.puml`, the duplicate design-plan PDF.
Still missing: `updates.md` itself, the current `TheaterApi/` source tree, and `embed_query.ipynb` (only needed for Phase 10).
