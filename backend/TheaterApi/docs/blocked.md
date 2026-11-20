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

## C3 — Postgres hold tests (H2)

Tests `Row_5_16_TwentyConcurrentHoldsForOneSeat_ExactlyOneSucceeds`, `Row_5_17_ExpiredHoldCleanup_DeletesTicketAndItemAndAbandonsCart`, `Row_5_18_HoldingDifferentSeats_ReleasesOldHold`, `Row_5_19_NonScheduledPerformance_IsRejected`, and `ExpiredHoldTakeover_RemovesStaleTicketAndCartItem` are written and compile. They could not run against either the old or updated hold service because the Testcontainers fixture cannot start without Docker. H2 unblocks execution; re-run the 20-request test multiple times and verify the full set.

## C4 — Checkout race integration test (H2)

`Row_6_3_TwoConcurrentCheckoutsOfSameCart_ExactlyOneSucceeds` is written and compiles, but Testcontainers cannot start without Docker. It reads its mock payment value from `THEATER_TEST_PAYMENT_CARD`; set that variable in the local test environment without committing it. H2 unblocks the database run.

## C6 — client coordination (H6)

All application routes have been moved under `/api`; the previous unprefixed URLs now return 404. J must update external clients and confirm the transition per H6. This is an intentional client-visible breaking change until H6 is completed.

## C7 — CORS origins (H5)

Origins must be supplied by J. `Program.cs` now reads `Cors:AllowedOrigins` and fails closed when it is empty; `CorsPolicyTests.Row_4_13_DisallowedOriginGetsNoCorsHeaders` is written but could not run because Docker is unavailable (H2). Production origin values remain blocked pending H5.

## C8 — Swagger verification (H2, H10)

XML documentation is enabled and all controller actions have summaries. `SwaggerSummaryTests.Row_0_6_EverySwaggerOperationHasSummary` is written, but its WebApplicationFactory fixture cannot start without Docker (H2). Launching Swagger in a browser remains H10.

## C9 — person_status (H8)

The Phase 3 Technical Report says `person_status(id)` returns a subclass-specific result, with `Patron Only` and `Staff member` examples and counts. `IPatronService.PersonStatusAsync` uses those labels based on staff-row presence; its missing-person result is null. J still needs to confirm labels and performer behavior (H8).

## C11/C14 — external verification (H2, H9, H10, H13)

Tests requiring Postgres/pgvector or Firestore emulator cannot be reported as passing unless those services are available. Fresh Neon migration and hosted Swagger/demo checks remain J's H9/H10 responsibility. Final audit sign-off remains H13.

## C11 — remaining audit tests not yet implemented (H1, H2)

The current test project contains the C3/C4 hold and checkout race tests, payment unit tests, route/clock/source safety checks, the C7/C8 tests, and the C9 contract tests. The following C11 groups are still missing; Postgres/Firestore integration coverage requires H2, and tests against the specified seed counts also require H1:

- Phase 1: 1.17–1.21 (fresh migration, schema/table count, mapped entity counts, preferences CHECK, duplicate performance seat).
- Phase 2: 2.11–2.15 (CRUD/counts, seeded login history, recent audit date, Firestore emulator).
- Phase 3: 3.18–3.22 (seed counts/idempotence, event seats route, all price-tier scopes and tie-break).
- Phase 4: 4.15–4.21 (auth and authorization integration cases).
- Phase 6: 6.16–6.22 (totals/promo, declined payment, idempotency, sold-out log, ownership and card-data absence). Payment unit coverage exists, but the named integration rows remain missing.
- Phase 7: 7.13–7.14 (performance creation and role authorization).
- Phase 8: 8.12–8.13 (all Firestore document types and outage behavior).
- Phase 9: 9.11–9.14 (seed containment, preferences, points, review CRUD/ownership).

These rows are not described as “written, not run”: they still need test code. H2 unblocks Postgres/emulator execution; H1 unblocks exact seed-data assertions.

## C13 — host / semantic search (H4, H7, H12)

Phase 10 is not authorized because A7 is not confirmed; audit rows 10.1–10.7 are marked N/A with reason “A7 not confirmed”. The Docker image builds with the .NET 10 SDK image, but Docker is unavailable here (H2), so image build/run was not verified. Production origins and deployment values remain J's H5/H12 inputs. Keep `net10.0` unless H4 explicitly approves a change.
