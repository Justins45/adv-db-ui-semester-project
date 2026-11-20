# Assumptions and defaults

Defaults observed in the implementation or prescribed by the spec. Items marked for confirmation remain assumptions, not closed decisions.

| ID | Current default | Reason / alternative | Confirmation |
|---|---|---|---|
| A4 | Hold cleanup runs as a hosted background service on a 30-second interval. | Within the specified 30–60 second window; an on-demand cleanup was the alternative. | None requested. |
| A6 | Patron points changes are implemented in C# with an atomic SQL update. | The spec says not to add a database function. | `person_status` behavior is undefined; blocked pending H8. |
| A7 | Semantic review search is not confirmed and is not implemented. | Requires product/tooling decision and query embedding implementation. | J confirmation required (H7). |
| A8 | Demoting a person to PATRON removes their staff row. | Keeps staff role data consistent with the role claim rule. | None requested. |
| A9 | Performer records use `person` + `patron` + `performer`; the password is a generated unusable value. | `person.password` is NOT NULL and every person has a patron row. | None requested. |
| A11 | New staff require an assigned venue; no default venue is selected. | The schema requires `assigned_venue` and the deployment has no specified default. | None requested. |
| A12 | Application log events use the asynchronous channel and Firestore writer. | Keeps Firestore failures outside request processing. | None requested. |
| A13 | Newly written error logs may include an ISO-8601 UTC timestamp string. | Seed error documents have no timestamp; a new error event needs one. | None requested. |
| A14 | Keep `net10.0`. Git history shows the initial project commit already targeted `net10.0`; no framework change appears in the project history. | Avoids an unrequested framework change. | J should confirm the .NET 10 build host (H4). |
| A15 | Mock card comparison ignores spaces and dashes; only the prescribed test card succeeds. | Follows spec 6.4. | None requested. |
| A16 | No seeded-account password setup is assumed unless configured locally; credentials must come from user secrets and never be committed. | Seeded password values are unusable. | Verify seed workflow after H1. |
| C0-1 | Auth rate limit is 10 requests per minute. | Existing `Program.cs` fixed-window policy default; no product-specific limit is specified. | None requested. |
| C0-2 | Firestore document IDs are string forms of integer `id` values from the seed. | Matches the seed and makes re-seeding idempotent. | None requested. |
| C0-3 | No API version segment is added; routes use `/api` per the brief. | Versioning is optional in the brief and would change client URLs further. | H6 must confirm the `/api` prefix transition. |
| C2-1 | Application timestamps and comparisons use the injected `TimeProvider`; hold SQL receives its timestamp as a parameter. | Makes time behavior controllable with the fake clock; no database `now()` expressions were found. | None requested. |
| C3-1 | Hold claims use PostgreSQL's default `READ COMMITTED` isolation plus one atomic conditional `UPDATE`; a short row-level update determines the winner. | The spec requires approach B and an encompassing transaction, but does not require serializable isolation. | Re-run the concurrent Postgres test after H2. |
| C5-1 | The accepted mock-payment input is supplied through `MockPayment:AcceptedCardNumber` in user-secrets or environment configuration, rather than stored in source. | The task forbids embedding card values in code, tests, logs, or documentation. The service declines when no accepted value is configured. | Set the local environment value before checkout tests or demonstrations. |
| C6-1 | All controllers use the `/api` prefix; the old root diagnostic endpoint was removed. | The route prefix is required by the brief and the root response was an unrequested diagnostic endpoint. | H6: notify/update existing external clients before they call the changed paths. |
| C9-1 | `person_status` returns `Staff member` when a staff row exists and `Patron Only` otherwise; a missing person returns null. | The Phase 3 report identifies these subclass-specific labels and reports those two groups, but does not specify performers or exact missing-ID behavior. The simplest mapping uses staff membership. | J should confirm labels and performer behavior (H8). |
