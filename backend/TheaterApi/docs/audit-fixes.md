# TheaterApi: Audit Fixes

Source: `docs/audit.md` (the filled-in audit). This file has two parts:

- **Part A: Human checks.** Things only J can do or decide (installs, secrets, live databases, decisions). Codex must not attempt these.
- **Part B: Codex tasks.** Everything Codex can write, change, or verify from the repo.

Precedence is unchanged: `docs/spec.md` and `docs/plan.md` win over the API brief.

## Read this first

The audit marked every row PARTIAL and said not every row was independently verified, so most PARTIAL rows are "unknown", not "known broken". What the audit did establish:

| Established finding | Where fixed |
|---------------------|-------------|
| No `TheaterApi.Tests` project exists, so no required test exists | C1, C11 |
| Hold update runs one seat at a time, not as one atomic conditional UPDATE (5.4) | C3 |
| No `xmin` concurrency token at checkout (6.3) | C4 |
| No `IPaymentService` abstraction (6.4) | C5 |
| Most routes lack the `/api` prefix (4.1) | C6 |
| No explicit CORS (4.13) | C7 |
| Swagger summaries missing (0.6) | C8 |
| `person_status` missing (9.6) | C9 |
| Direct `UtcNow` calls remain (0.5, X9) | C2 |
| `docs/assumptions.md` absent (0.10) | C0 |
| Business logic in controllers; some concrete services (X1) | C12 (optional) |
| No Dockerfile or complete README (11.x) | C13 |
| Target framework is `net10.0`; the original is unproven (T17, 0.1) | C0, H4 |

Row 0.11 (no distinct Phase 0 commit) is not fixable and is accepted as-is. Do not rewrite history.

---

# Part A: Human checks (J)

Do these yourself. Codex tasks that depend on one say "needs H#".

| # | Check | Why it is manual | Unblocks |
|---|-------|------------------|----------|
| H1 | **Put the seed and source files in the repo.** Copy `AdvDataPhase2SeedData.sql`, `theater_reviews_with_vectors.sql`, `firestore_logs_seed.json`, `generate_seed_data.py` into `seed/`. Copy the brief (renamed `theater-ticketing-api-brief.md`), the three SQL schema files, and the Phase 1-3 reports into `docs/sources/` (spec 11). The audit found no `seed/` directory. | Files are on your machine, not in the checkout | C10, C11 |
| H2 | **Install test prerequisites.** Docker running (for a Postgres 15+ container with pgvector, e.g. `pgvector/pgvector:pg16`). Firebase CLI plus Java for the Firestore emulator. Set `FIRESTORE_EMULATOR_HOST` (e.g. `localhost:8080`). | Needs your machine; Codex's sandbox may not have Docker | C1, C11 |
| H3 | **Check secrets and git history.** Confirm no Neon connection string, JWT key, or Firestore service-account key was ever committed (e.g. `git log -p -S"Password=" -- .` or run `gitleaks`). If anything leaked, **rotate it** (Neon password, JWT key, Firestore key). Confirm `appsettings.json` holds placeholders only. | Rotation happens in the Neon and Google consoles | C10 (row 0.8) |
| H4 | **Decide the target framework.** C0 reports the original `TargetFramework` from git history. If it was not `net10.0`, A14 says revert unless you approve the upgrade. Decide: keep `net10.0` (note it in `docs/assumptions.md`) or revert. Also confirm the .NET 10 SDK is installed on any machine or host that builds this. | A14 says "do not upgrade unasked"; only you can approve | C0, C13 |
| H5 | **Give the allowed CORS origins** (e.g. your Vue admin dev URL and the production URL). | Only you know them | C7 |
| H6 | **Confirm the `/api` prefix change** is fine for any client you've already built (e.g. the Vue admin calling the old un-prefixed routes). Update those clients' base URL. | Client code is outside this repo | C6 |
| H7 | **Confirm A7: semantic search in or out.** If out, Phase 10 is marked `N/A`. If in, you must also choose how the query embedding runs in .NET (in-process ONNX vs a small Python service). | Product and tooling decision | C13 (Phase 10) |
| H8 | **Define `person_status`** if the Phase 3 report isn't available to Codex in `docs/sources/`. Say what it returns and for whom. | The spec doesn't define it (A6 only says implement it in C# ) | C9 |
| H9 | **Check Neon supports `CREATE EXTENSION vector`** on your project, and create a **scratch Neon branch** for one manual "migrate from zero" run. Never run the seed or migrations against your real data branch while auditing. | Neon console access | C11 (1.17-1.18), C13 |
| H10 | **Run the real checks Codex can't:** (1) `dotnet test` on your machine with Docker and the emulator up, and paste any failures back to Codex; (2) launch the app and open `https://localhost:5045/swagger`; (3) click through register, login, hold, checkout once. | Needs Docker, the emulator, and a browser | C11, C14 |
| H11 | **Decide whether to do C12** (moving logic out of controllers). It's a pure refactor with no new behavior, so you can skip or defer it without blocking correctness. | Scope call | C12 |
| H12 | **Choose a host and set environment variables** there (Neon string, JWT key, Firestore project and credentials). Never share one database, JWT key, or Firestore credential between instances. | Hosting account and secrets | C13 |
| H13 | **Review `docs/audit.md` after C14** and sign off, or send Codex whatever is still `PARTIAL`. | Final acceptance | - |

---

# Part B: Codex tasks

## Rules for Codex

- Do tasks in order unless a task says otherwise. One commit per task (or per small group), with a message like `fix(C3): atomic hold update`.
- Run `dotnet build` and `dotnet test` before and after each task. Do not leave the build red.
- Do not change closed decisions (spec 2). If something here conflicts with the spec, the spec wins; log the conflict in `docs/assumptions.md` and continue.
- Anything unclear: pick the simplest default, log it in `docs/assumptions.md`, continue.
- Never run migrations, the seed, or tests against a real Neon or Firestore database. Use local Postgres/Testcontainers and the Firestore emulator only.
- Do not open the seed files in full (`head`, `grep`, `wc` only).
- If a task needs something from Part A that is missing, stop that task, write what is missing in `docs/blocked.md`, and move on.
- Write each test **before** refactoring the code it covers, run it against the current code, and note whether it passed or failed in the commit message.

## C0. Preflight (no code changes)

1. Create `docs/assumptions.md` (row 0.10). Seed it with every default the code already made that you can find, plus the target framework note.
2. Run `git log --follow -p -- TheaterApi/TheaterApi.csproj` and record the **original** `TargetFramework` and every change to it. Put the answer in `docs/assumptions.md` under A14 and report it so J can do H4. Do not change the framework.
3. List every file in `docs/` and `seed/` that spec 11 expects but is missing. Write it to `docs/blocked.md` (do not invent file contents).

Done when: `docs/assumptions.md` exists, the original framework is recorded, and `docs/blocked.md` lists what J still needs to add.

## C1. Test foundation (rows 0.7 and every test row) needs H2

1. Create `TheaterApi.Tests` (xUnit, `Microsoft.AspNetCore.Mvc.Testing`) and add it to the solution so plain `dotnet test` discovers it.
2. A shared fixture that starts a real Postgres container (Testcontainers, a pgvector image, Postgres 15+ because of `NULLS NOT DISTINCT`), applies the EF migrations, and exposes a `WebApplicationFactory<Program>` pointed at it. **No EF in-memory provider** for any seat logic (brief 9).
3. A Firestore emulator fixture that reads `FIRESTORE_EMULATOR_HOST`, uses a unique project or collection prefix per test run, and cleans up. If the variable is unset, fail the Firestore-dependent tests with a clear message instead of silently skipping.
4. A fake clock (`Microsoft.Extensions.TimeProvider.Testing` `FakeTimeProvider`) registered over the real `TimeProvider` in the factory.
5. Test helpers: create a patron and get a JWT, create a staff/admin and get a JWT, create a small performance with N seats and price tiers.
6. One smoke test per fixture proving it works.

Done when: `dotnet test` prints a test summary with discovered tests and they pass.

## C2. Clock cleanup (rows 0.5, X9)

1. `grep -rn "UtcNow\|DateTime.Now" TheaterApi/` and replace every direct clock call with the injected `TimeProvider`. The audit named `Service/AuditLogService.cs`, `Services/AuthService.cs`, `Controllers/EmailListController.cs`; check for more.
2. Any SQL that uses `now()` for hold expiry or checkout validation must instead take the time as a **parameter** from `TimeProvider`, so tests with a fake clock can control it (spec 6.2: all times from the server UTC clock via `TimeProvider`). Log this in `docs/assumptions.md`.
3. Add a test that fails if `UtcNow` appears outside the `TimeProvider` registration (a simple source grep test is fine).

Done when: no direct clock calls remain except the registration; build and tests pass.

## C3. Atomic hold (row 5.4, spec 6.2 / brief 6 approach B) needs C1, C2

1. First write test 5.16 (20 concurrent holds for one seat -> exactly one 200 and nineteen 409s) and run it against the **current** code. Record the result.
2. Replace the per-seat updates with **one** conditional `UPDATE ... WHERE performance_id = @perf AND seat_id = ANY(@seatIds) AND (status = 'AVAILABLE' OR (status = 'HELD' AND held_until < @now))`. Compare affected rows with the number requested; fewer means roll back and return 409.
3. Keep the transaction, the unique indexes, and the deletion of the stale `ticket` and `cart_item` for any seat taken over from an expired hold, in the same transaction and before inserting new ones.
4. Keep `UNAVAILABLE` seats excluded, and the other rejection rules (spec 6.2).
5. Add tests 5.17-5.19 and a test for taking over an expired hold.

Done when: test 5.16 passes (run it several times) and all Phase 5 "Done when" checks pass.

## C4. Checkout concurrency token (row 6.3) needs C1

1. Map Postgres `xmin` as a concurrency token on the entity checkout updates (`performance_seat`, and `cart` if used), using the mapping that the installed Npgsql EF provider version supports.
2. Map `DbUpdateConcurrencyException` to 409.
3. Do not remove the serializable isolation unless a test proves it is safe; log the decision in `docs/assumptions.md`.
4. Add a test: two concurrent checkouts of the same cart give exactly one success.

Done when: the test passes and checkout still passes its Phase 6 checks.

## C5. Payment abstraction (row 6.4)

1. Extract `IPaymentService` and a `MockPaymentService` from the checkout flow. Rule (spec 6.4, A15): ignore spaces and dashes, `4545454545454545` succeeds, anything else is declined; successful `payment_reference` is `MOCK-<guid>`.
2. Register in DI. Declined -> 402, no order, seats stay held. Expiry and CVV are not checked.
3. The card number must never be stored, logged, or returned, including in exception messages and `ProblemDetails`.
4. Unit test the service; keep the checkout tests (6.18, 6.22).

Done when: checkout depends only on `IPaymentService`, and the tests pass.

## C6. `/api` route prefix (row 4.1) needs H6

1. Make every route start with `/api` (brief 5), via one global convention or per-controller `[Route]`, whichever fits the code. `LogsController` stays `api/logs/...`.
2. Update Swagger, tests, and any docs that list routes.

Done when: `grep` finds no controller route outside `/api`, and tests pass.

## C7. CORS (row 4.13) needs H5

1. Add an explicit CORS policy with allowed origins read from configuration (not hard-coded, no `AllowAnyOrigin`). Apply it in the correct place in the middleware pipeline.
2. Confirm `UseHttpsRedirection` / HTTPS-only behavior.
3. Add a test that a disallowed origin gets no CORS headers.

Done when: the policy exists, is configurable, and the test passes.

## C8. Swagger summaries (row 0.6)

1. Turn on XML documentation (`GenerateDocumentationFile`) and include the XML in Swagger.
2. Add a `<summary>` to every controller action, plus response types for 400/401/403/404/409 where they apply.
3. Make sure the missing-XML-comment warning (CS1591) is not silenced globally just to hide gaps.

Done when: every action shows a summary in the Swagger JSON (add a test that checks each operation has a non-empty summary).

## C9. `person_status` (row 9.6, A6) needs H8

1. Find the definition in the Phase 3 Technical Report in `docs/sources/`. If it is not there, implement the simplest reasonable behavior, log the assumption in `docs/assumptions.md`, and mark it for J to confirm.
2. Implement as a C# service method behind an interface. Test it.

Done when: the method exists, is tested, and the assumption is recorded if the source was unavailable.

## C10. Verify-then-fix (rows the audit could not establish)

For each row: inspect, fix if wrong, add a test or a repeatable check where it is feasible, and write a one-line result with evidence in `docs/audit.md`. Do not leave any of these as "needs manual confirmation" unless the cause is in Part A.

| Row | What to verify and fix |
|-----|------------------------|
| 0.8 | Grep tracked files for connection strings, keys, tokens. Confirm `.gitignore` covers key files and `appsettings*.json` has placeholders only. (History check is H3.) |
| 1.6, 1.7 | Open the Baseline migration: confirm the order (`AdvDataPhase2Schema.sql`, `new_table_updates.sql`, then sections 2-3 of `jsonb_implementation.sql`), that `review` uses a plain `CREATE TABLE` (no `DROP TABLE IF EXISTS`), and that `Down` exists. |
| 1.13, 3.6 | Find the order backfill (4.2 item 4). Confirm status `PAID`, `total_amount` from `payment.amount`, `order_item` from `cart_item` -> `ticket` -> `performance_seat`. Add a test on seeded data. |
| 2.10, 3.8 | Confirm `SeedFirestore` and the Firestore seed step load 94 + 1,200 documents keyed by `id` and are safe to re-run (needs H1). |
| 3.3-3.9, 3.19, 3.20 | Walk each seed step: only runs into empty tables, never updates or deletes existing rows. Fix any step that does. |
| 3.16 | Seat list treats expired `HELD` as available (lazy expiry). |
| 4.12 | Confirm the rate limiter is applied to login **and** password-reset request/confirm. |
| 4.14, 6.6, 8.10 | Grep logs, DTOs, `ProblemDetails`, exception messages, and Firestore log writers for passwords, hashes, JWTs, and card numbers. |
| 5.14 | Confirm structured `ILogger` messages for every hold, expiry, checkout, and conflict. |
| 6.11 | Confirm the `filled` flip happens in the checkout transaction and writes a `performance_status_log` entry with no `changed_by` and `new = SOLD_OUT`. |
| 7.1-7.12 | For every admin endpoint: role protection (Staff vs Admin-only role change), transaction boundaries, `assigned_venue` required for new staff (A11), 201 + `Location` on create. Fix gaps. |
| 8.5 | Build a table of every Staff/Admin write endpoint and whether it writes an `audit_log` entry. Add the missing ones. Confirm `event.image` is excluded from `changes`. |
| T13 | Confirm the baseline SQL does not install the Phase 2 portfolio triggers, procedure, or view. If it does, remove them from the migration (spec 6.2). |
| X3, X5, X6, X7 | Check no list response returns `password` or image bytes, status codes match (201+`Location`, 400, 401/403, 404, 409), reads use `AsNoTracking`, and the event list is paged. |

## C11. Write the missing tests (every `PARTIAL` test row)

All use the C1 fixtures and real Postgres. Mirror the plan's "Done when" lines. Name tests so the row is findable (e.g. `Row_5_16_...` or a trait).

| Phase | Test rows |
|-------|-----------|
| 1 | 1.17 (fresh DB migrates), 1.18 (27 tables, no `login_history`), 1.19 (count through EF for every mapped entity), 1.20 (bad preferences rejected by the CHECK), 1.21 (duplicate `performance_seat` fails) |
| 2 | 2.11 (CRUD round-trip), 2.12 (counts 9 / 45 / 6), 2.13 (1,200 `login_history`), 2.14 (`audit-recent` computed relative to today, not hardcoded), 2.15 (emulator only) |
| 3 | 3.18 (row counts), 3.19 (seed twice), 3.20 (seed after editing a row), 3.21 (seats endpoint), 3.22 (price resolution: each scope and the tie-break) |
| 4 | 4.15-4.21 (register; login success/failure both reach `ILogSink`; unusable hash gives 401 not 500; dev accounts log in; wrong-type role rejected; patron A cannot read patron B; admin routes 403 for patrons) |
| 5 | 5.16-5.19 (written in C3) |
| 6 | 6.16-6.22 (unit tests for `d_off`, `p_off`, floor at zero, rounding; inactive/expired/empty promo rejected; declined card; idempotency; `filled` flip; order ownership; card number absent from logs and responses) |
| 7 | 7.13, 7.14 |
| 8 | 8.12 (each of the five log types produces the expected emulator document), 8.13 (a Firestore outage does not fail or delay a request) |
| 9 | 9.11 (containment filter returns 3 wheelchair patrons), 9.12 (invalid preferences 400), 9.13 (points below zero rejected), 9.14 (review create/edit/delete, owner only) |

Seed-based tests need the `seed/` files (H1). If H1 is not done, write the tests against a small hand-built fixture, mark the seed-count ones as blocked in `docs/blocked.md`, and move on.

Done when: every row above has a passing test, and `dotnet test` reports them.

## C12. Controller logic and interfaces (row X1) optional, needs H11

Only after C11 is green.

1. Move business logic out of `ReviewsController`, `AuthController`, and `PatronsController` into services.
2. Put every service behind an interface registered in DI (`IAuthService`, `IHoldService`, `IOrderService`, `IPromoService`, `IEmailSender`, `ILogSink`, `ICatalogService`, as named in brief 13).
3. **No behavior changes.** The test suite must stay green after each controller. One commit per controller.

Done when: controllers only parse, validate, call a service, and return a status code.

## C13. Delivery and Phase 10 (rows 11.1-11.5, 10.1-10.7) needs H4, H7, H12

1. Phase 11: a Dockerfile; all configuration from environment variables; migrations and seeding stay manual; a README covering setup, secrets, migrate, seed, Swagger, and a demo script that walks the "project is complete when" scenario in brief 11; Neon `EnableRetryOnFailure`.
2. Phase 10: only if H7 says yes. Otherwise mark rows 10.1-10.7 `N/A` with the reason "A7 not confirmed" in `docs/audit.md`.

Done when: the container starts against Neon using only environment variables (J verifies with H12), and the README works from a clean clone.

## C14. Re-audit

1. Re-run `implementation-audit.md` and overwrite `docs/audit.md`.
2. **No row may be `PARTIAL` without a one-line reason** naming what is blocked and which Part A item (H#) would unblock it. "Needs manual confirmation" is not a reason on its own.
3. Every `DONE` row must cite a file/symbol or a passing test name.
4. Update the summary table and the gaps list at the end.

Done when: the summary table has `DONE` counts that match the evidence, and every non-`DONE` row has a stated blocker.
