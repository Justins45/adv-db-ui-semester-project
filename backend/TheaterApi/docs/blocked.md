# Blocked and external prerequisites

## C0 — expected files missing from this checkout (H1)

The source documents are already present in `docs/sources/` under these names, so they do not need to be copied again: `AdvDataPhase2Schema.sql`, `new_table_updates.sql`, `jsonb_implementation.sql`, `theater-ticketing-api-brief.md`, `Phase 3 - DB - Design Plan.pdf`, `Phase3_Technical_Report.pdf`, `phase-2-Implementation_Report.md`, `phase-2-README.md`, `relational-schema.md`, `EERD.png`, and the Phase 3 diagrams/reports.

The following expected inputs are absent:

- `seed/AdvDataPhase2SeedData.sql`
- `seed/theater_reviews_with_vectors.sql`
- `seed/firestore_logs_seed.json`
- `seed/generate_seed_data.py`
- `docs/sources/phase-1-relational-schema.md` (a differently named `relational-schema.md` exists)
- `docs/sources/phase-1-EERD.png` (a differently named `EERD.png` exists)
- `docs/sources/phase-2-readme.md` and `docs/sources/phase-2-implementation_Report.md` (case/name variants exist)
- `docs/sources/updates.md`

Seed-dependent tests must use hand-built fixtures until H1 supplies the seed files. Tests that assert the exact seed counts remain blocked by H1.

## C1 — container/emulator prerequisites (H2)

Docker (`docker info`) and Firebase CLI are unavailable; Java is installed, but `FIRESTORE_EMULATOR_HOST` is unset. C1 fixtures and smoke tests are written and compile. Runtime checks are blocked by H2: `PostgresFixture_AppliesMigrationsAndConnects` fails before connecting because Docker is unavailable, and `FirestoreFixture_ConnectsToEmulator` fails with the explicit missing-`FIRESTORE_EMULATOR_HOST` message. Re-run both after J provides Docker, Firebase CLI, Java/emulator, and the emulator host.

## C6 — client coordination (H6)

The requested `/api` route prefix changes existing URLs. Code changes can be made, but J must update external clients and confirm the transition.

## C7 — CORS origins (H5)

Origins must be supplied by J. Implement a configuration-driven allowlist with no implicit wildcard; local policy behavior can be tested with an explicit test origin. Production origin values remain blocked pending H5.

## C9 — person_status (H8)

No definition was supplied in the available Phase 3 report/spec. Implement the simplest service behavior only if it can be grounded in the report; otherwise the service contract and test remain blocked pending H8.

## C11/C14 — external verification (H2, H9, H10, H13)

Tests requiring Postgres/pgvector or Firestore emulator cannot be reported as passing unless those services are available. Fresh Neon migration and hosted Swagger/demo checks remain J's H9/H10 responsibility. Final audit sign-off remains H13.

## C13 — host / semantic search (H4, H7, H12)

Phase 10 is not authorized because A7 is not confirmed; audit rows 10.1–10.7 must be N/A with reason “A7 not confirmed”. Container execution against Neon and deployment-specific configuration remain blocked pending H12. Keep `net10.0` unless H4 explicitly approves a change.
