# Plan - TheaterApi build order

Each phase ends with checks you can run. Do not move on until they pass. Commit once per phase.
Anything unclear: pick the simplest default, log it in `docs/assumptions.md`, continue. Section numbers refer to `docs/spec.md`.
The API brief's milestones (M1-M7) map to phases below; its section 6 is the algorithm for holds and checkout.

## Phase 0 - Baseline (brief M1)
- Inspect the existing `TheaterApi/` project. Keep what works (Neon connection, Firestore setup, `LogsController` pieces); note the target framework and keep it (A14).
- Remove the connection-test GET/POST.
- Add: global exception handler returning ProblemDetails, `TimeProvider` registration, Swagger with summaries.
- Create `TheaterApi.Tests` (xUnit, `WebApplicationFactory`).

Done when: `dotnet build` and `dotnet test` pass; Swagger loads at https://localhost:5045/swagger; no test endpoints remain; no secrets in git.

## Phase 1 - Schema
- Hand-write EF models and the `AppDbContext` mapping for the 25 tables (no `login_history`), `review`, and the 4.2 changes (spec 3, 4). `Person` has optional one-to-one `Patron`, `Staff`, `Performer` (no TPT inheritance). Map `"ORDER"` exactly; do not map `ticket_scan`.
- `Baseline` migration = the raw SQL of `AdvDataPhase2Schema.sql`, `new_table_updates.sql`, and sections 2-3 of `jsonb_implementation.sql` (spec 3.1, 3.2); then normal migrations that drop `login_history` and apply 4.2.
- Enable the `vector` extension.

Done when: a fresh Neon database migrates from zero; 27 tables exist (excluding the EF history table) and `login_history` is not one of them; `SELECT count(*)` through EF works for every mapped entity (catches model/SQL drift); a bad preferences document is rejected by the CHECK; a second `performance_seat` for the same seat and performance fails.

## Phase 2 - Logs API (Firestore)
- `LogsController` under `api/logs/<endpoint>` (spec 8.2): CRUD over five collections, per-action counts, `audit-recent?daysAgo=N`.
- `tools/split_seed.py` (spec 9 step 1): writes `postgres.sql` and `login_history.json`.
- Models allow the optional fields in 8.1; timestamps are strings.
- Seeding via the `SeedFirestore` flag: 94 documents from the JSON plus 1,200 `login_history` documents from the split; safe to re-run.

Done when: every endpoint works in Swagger; create/read/update/delete round-trips; the action counts are 9 / 45 / 6 on the seed; `login_history` holds 1,200 documents; `audit-recent` agrees with a count computed from the seed relative to today; integration tests use the Firestore emulator, not the real database.

## Phase 3 - Seed and catalogue (brief M2)
- `dotnet run -- seed`: the seven steps in spec 9 (split, Postgres, reviews, preferences, order backfill, dev accounts, Firestore), each only into empty tables.
- Public read endpoints: events (paged, search, genre filter, no image bytes), event detail, genres, performance detail, performance seats with resolved price (spec 6.3), event image.

Done when: row counts match spec 9; running the seed twice, or after editing a seeded row, changes nothing; `/performances/{id}/seats` returns status and price for every seat of a seeded performance; price-resolution unit tests cover each scope and the tie-break.

## Phase 4 - Auth (brief M3)
- Register, login (JWT), `/auth/me`, password reset request/confirm with the mock email sender, email-list join/leave (spec 6.1).
- Introduce `ILogSink` with an in-memory implementation; login attempts go through it (the Firestore writer arrives in Phase 8).
- Roles `PATRON`/`STAFF`/`ADMIN`; role validation per spec 6.1; rate limiting on login and reset.

Done when: tests cover register, login success and failure (both reach `ILogSink`), a seeded person with an unusable hash gets 401 not 500, the dev accounts can log in, a wrong-type role is rejected, patron A cannot read patron B's data, admin routes return 403 for patrons.

## Phase 5 - Holds (brief M4, the core)
- `POST /performances/{id}/holds`, `GET /holds/current`, `DELETE /holds/{id}` (spec 6.2).
- Lazy expiry plus the background cleanup service (A4).

Done when: 20 simultaneous hold requests for one seat give exactly one 200 and nineteen 409s; an expired hold makes the seat available again and its ticket and cart item are gone (cart `ABANDONED`); holding different seats releases the old hold; holds for a non-`SCHEDULED` performance are rejected.

## Phase 6 - Checkout and promo codes (brief M5)
- `POST /orders` with an optional promo code and `Idempotency-Key`; mock payment service (card `4545 4545 4545 4545` passes, anything else is declined with 402 and no order; spec 6.4); `GET /orders`, `GET /orders/{id}`.
- Promo code management (Staff).

Done when: totals use ticket price snapshots; `d_off` and `p_off` (a fraction) both calculate correctly, floor at zero, round to 2 decimals; inactive, expired, or empty promo codes are rejected; a declined card creates no order and leaves the seats held; the card number appears in no log or response; retrying with the same key returns the original order; `filled` flips when the last seat sells; a patron cannot read another patron's order.

## Phase 7 - Admin endpoints
- Events, genres, performers (A9), venues/stages/sections/seats (bulk), performances (price per section, performance seats and price tiers created in one transaction, cast), role changes (spec 6.1).

Done when: creating a performance generates one `performance_seat` per seat of its stage and the (performance, section) tiers; a section from another stage is rejected; only Admin can change roles.

## Phase 8 - Log sink wiring (brief M6)
- Replace the in-memory `ILogSink` with `Channel<T>` + a Firestore background writer, wired into auth (`login_history`), errors, admin changes, role changes, and `filled` flips (spec 8.3).

Done when: each log type produces the expected Firestore document (emulator tests); a Firestore outage does not fail or delay a request.

## Phase 9 - Preferences, points, reviews
- Preferences get/patch and the wheelchair filter, points service (spec 6.5), review CRUD (spec 6.6, A7). Store `embedding` as NULL unless Phase 10 is confirmed.

Done when: the containment filter returns the 3 wheelchair patrons on the seed; invalid preferences return 400; a points change below zero is rejected; review create/edit/delete works and only the owner can edit.

## Phase 10 (optional, needs A7) - Semantic search
- Regenerate the seed review vectors with title + body (the seed vectors use the body only), so seed and new reviews share one method.
- Query embedding with the same model (all-MiniLM-L6-v2, 384 dims); `ORDER BY embedding <=> $1 LIMIT k`; filter-first CTE for filtered searches; embeddings regenerated on review edit.

Done when: a search returns ranked reviews; the unfiltered query plan uses `idx_review_embedding`; the wheelchair example from the Technical Report (staff excluded) works.

## Phase 11 - Delivery (brief M7)
- Dockerfile; all configuration from environment variables; migrations and seeding as manual steps.
- README: setup, secrets, migrate, seed, Swagger, demo script.

Done when: the container starts against Neon using only environment variables; a clean clone follows the README to a working Swagger page; the project-complete scenario in the brief's section 11 passes.
