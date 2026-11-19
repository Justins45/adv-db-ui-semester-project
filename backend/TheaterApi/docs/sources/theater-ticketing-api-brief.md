# Theater Ticketing API: Project Brief

A fully API-based backend for a theater ticketing platform. Browse events, hold seats for 10 minutes, check out. Built with ASP.NET Core, C#, EF Core, Neon (serverless Postgres) and Google Firestore (logs). Built as a single-venue product and deployed as multiple independent instances. Canada only. Written for a developer who knows Java/Spring Boot and is new to C#/.NET.

---

## 1. Goals and Scope

### Goals
- Working, deployed REST API covering the full ticketing flow.
- Correct handling of the hard problem: **no double-booking under concurrent requests**.
- Relational schema follows the EERD and is in **3NF**; logs live in Firestore (NoSQL).
- Learn the stack by building, not by reading docs front to back.

### Product framing
- **Built for a single venue:** one codebase, no tenant column in any table.
- **Expressed as multiple:** every client gets its own deployed instance with its **own Neon database, its own server, and its own Firestore database**. Instances share no data and have no cross-instance features.
- **Canada only:** no USA support (no states/ZIP codes, no multi-currency). Currency is CAD.

### In scope (MVP)
1. Registration and login, roles (Patron, Staff, Admin), login history, password reset, email-list opt-in
2. Browse events and performances (genres, cast)
3. View seats and availability for a performance
4. Hold seats for **10 minutes** (a hold is an EERD `cart`)
5. Check out (create an order from held seats), with an optional promo code and mock payment
6. View my orders
7. Automatic release of expired holds
8. Admin endpoints for events, genres, performers, venue/stage/section/seat structure, performances (with price tiers and cast), promo codes, and role changes
9. Logging to Firestore (login history, errors, audit, permission changes, performance status changes)
10. Seed data loaded once and preserved
11. Cloud deployment, one instance per venue

### Out of scope (cut unless time remains)
- Real payment processing (use a **mock payment service** behind an interface)
- Real email delivery (use a **mock email sender** behind an interface; it only logs)
- Email/PDF tickets, QR codes, and **ticket scanning** (the EERD's `ticket_scan` table is not built)
- Frontend (the backend is API-only; test with Swagger/Postman). Visual seat-map layouts are owned by the frontend; the API only returns seat identity, status and price
- Refunds, multi-currency, USA/international support
- Real-time seat updates (SignalR/WebSockets)
- Log viewer endpoints (read logs in the Firebase console)
- Multi-tenancy inside one database (isolation is by deployment)

---

## 2. Tech Stack

| Layer | Choice | Why |
|---|---|---|
| Framework | ASP.NET Core 8 Web API | Closest mental map to Spring Boot |
| Language | C# 12 | Required |
| API style | Controllers (not Minimal APIs) | More tutorials, closer to Spring `@RestController` |
| ORM | EF Core + Npgsql provider | Faster to learn than raw SQL; migrations built in |
| Relational DB | Neon (Postgres) | Free tier, standard Postgres |
| NoSQL DB | Google Firestore (`Google.Cloud.Firestore`) | Stores the log entities (append-only, schema-flexible) |
| Auth | JWT + `PasswordHasher<Person>` | See decision below |
| Validation | Data Annotations | Data Annotations first (less to learn) |
| Docs/testing | Swagger (Swashbuckle) + Postman | Built-in to the template |
| Tests | xUnit + WebApplicationFactory + Firestore emulator | Standard .NET testing |
| Background jobs | `BackgroundService` (hosted service) | Hold expiry cleanup and the Firestore log writer |
| Packaging | Docker | One image, deployed once per venue |

### Decisions with tradeoffs

**EF Core vs Dapper.** EF Core generates SQL and handles migrations, so you write less, but it can hide inefficient queries. Dapper gives full SQL control but needs more boilerplate and manual schema management. *Decision: EF Core.* Drop to raw SQL via `FromSqlRaw`/`ExecuteSqlRaw` only for the seat-locking query if needed.

**Controllers vs Minimal APIs.** Minimal APIs have less ceremony; controllers have far more learning material and structure. *Recommendation: controllers.*

**Full ASP.NET Identity vs JWT + `PasswordHasher`.** The EERD defines its own `person`, `role`, `login_history` and `password_reset_request` tables. Full Identity brings its own user/role/claims tables that would duplicate or fight the EERD, and needs extra mapping work. Using only `PasswordHasher<Person>` keeps the schema exactly the EERD and still avoids hand-rolling password hashing, but you write lockout and reset logic yourself (login history makes lockout easy). *Recommendation: JWT with `PasswordHasher<Person>`.* Decide this early and stick with it.

**Logs in Firestore vs Postgres tables.** Firestore suits append-only, high-volume, schema-flexible records and keeps write load off Neon. The cost: no foreign keys or joins (ids become plain fields), a second set of credentials and tooling, and log writes are not transactional with the business write. *Decision: Firestore for the five log entities; everything else stays in Postgres.*

**Seeding: EF `HasData` vs an idempotent seeder.** `HasData` is simple, but later migrations can overwrite seeded rows that users have since changed. A custom seeder that inserts only what is missing never touches user edits. *Recommendation: idempotent seeder* (see section 6).

**Where to enforce no-double-booking.** See section 6.

---

## 3. Architecture

```
HTTP request
   |
Controller   (HTTP only: parse, validate, return status codes)
   |
Service      (business logic, behind an interface, e.g. IHoldService)
   |-----------------------.
DbContext                 ILogSink   (queues log entries; never blocks a request)
(EF Core, data access)         |
   |                      Channel + BackgroundService writer
Neon Postgres                  |
                          Firestore
```

### Rules
- No business logic in controllers.
- Controllers return **DTOs**, never EF entities directly (avoids over-posting and circular reference problems). Never return `password` or the `image` bytes in list responses.
- Services depend on interfaces; registered in DI with `builder.Services.AddScoped<IFoo, Foo>()`.
- Async/await end to end; always pass `CancellationToken` where convenient.
- A failed or slow Firestore write must **never** fail or delay a request. Entries go through a `Channel<T>`; the writer retries, then falls back to `ILogger`.
- Secrets (Neon connection string, JWT key, Firestore credentials) live in **user secrets** locally and environment variables in production. Never in source control.

### Suggested folder layout
```
TheaterApi/
  Controllers/
  Services/          (interfaces + implementations)
  Data/              (AppDbContext, configurations, migrations, Seeding/)
  Models/            (EF entities)
  Dtos/
  Logging/           (ILogSink, Firestore writer, log document classes)
  BackgroundJobs/    (HoldExpiryService, LogWriterService)
  Middleware/        (global exception handling)
  Program.cs
TheaterApi.Tests/
```

### Spring to ASP.NET Core cheat sheet

| Spring Boot | ASP.NET Core |
|---|---|
| `@RestController` | `[ApiController]` on a controller class |
| `@Autowired` / constructor injection | Constructor injection via built-in DI |
| `application.properties` | `appsettings.json` + user secrets |
| Spring Data JPA | EF Core |
| Spring Security | Authentication/authorization middleware |
| `@Scheduled` | `BackgroundService` / `IHostedService` |
| `@Valid` | Data Annotations + `[ApiController]` auto-validation |
| `@ControllerAdvice` | Exception-handling middleware / `IExceptionHandler` |
| JUnit + MockMvc | xUnit + `WebApplicationFactory` |

---

## 4. Data Model

The entity list follows the EERD. Table and column names below are the Postgres names; EF maps them to PascalCase C# properties. Primary keys are integer identity columns unless noted.

### Identity and access (Postgres)

| Table | Columns |
|---|---|
| `person` | id, first_name, last_name, email (unique), phone, password (hash; null for people who never log in, e.g. performers) |
| `patron` | id (pk/fk to person), role_id (fk) |
| `staff` | id (pk/fk to person), role_id (fk) |
| `performer` | id (pk/fk to person), role (string, e.g. "Lead") |
| `role` | id, role (`Patron`, `Staff`, `Admin`) |
| `email_list` | person_id (pk/fk), added |
| `password_reset_request` | id, person_id (fk), time, success, status (`Requested`, `Completed`, `Expired`, `Failed`) |

`patron`, `staff` and `performer` are disjoint subtypes of `person`: a person is exactly one of them. In EF Core this is table-per-type inheritance.

### Catalog (Postgres)

| Table | Columns |
|---|---|
| `event` | id, title, description, status (`Draft`, `Live`, `Cancelled`), live_date, image (bytea) |
| `genre` | id, name, active |
| `event_genre` | genre_id, event_id (composite pk) |
| `venue` | id, name, address, city, province, postal_code |
| `stage` | id, venue_id (fk), name |
| `section` | id, stage_id (fk), name |
| `seat` | id, section_id (fk), row (string), seat_number (int) |
| `performance` | id, event_id (fk), stage_id (fk), date_time, filled (bool) |
| `performance_cast` | performer_id, performance_id (composite pk) |
| `price_tier` | id, section_id (fk), performance_id (fk), price numeric(10,2); unique (section_id, performance_id) |

### Inventory and holds (Postgres)

| Table | Columns |
|---|---|
| `performance_seat` (the key table) | id, seat_id (fk), performance_id (fk), status (`Available`, `Held`, `Sold`), held_until (timestamptz, nullable); **unique (performance_id, seat_id)** |
| `ticket` | id, performance_seat_id (fk, **unique**), date_time, price numeric(10,2) (snapshot taken at hold time) |
| `cart` | id, patron_id (fk), status (`Active`, `CheckedOut`, `Expired`) |
| `cart_item` | id, cart_id (fk), ticket_id (fk) |

### Sales (Postgres)

| Table | Columns |
|---|---|
| `order` | id, cart_id (fk, unique), promo_code_id (fk, nullable), created_at, total_amount numeric(10,2), status (`Pending`, `Paid`, `Failed`, `Cancelled`), payment_reference, idempotency_key (nullable, unique) |
| `order_item` | id, order_id (fk), performance_seat_id (fk), price_at_purchase numeric(10,2) |
| `payment` | id, order_id (fk), method (e.g. `Mock`), amount numeric(10,2), date_time |
| `promo_code` | id, code (unique, case-insensitive), d_off numeric(10,2) nullable, p_off float nullable (percent, 0 to 100), active, expiry_date |

`order` and `order_item` follow the API brief's order model; `cart_id` and `promo_code_id` come from the EERD. The ordering patron is reached through `order.cart_id → cart.patron_id`; storing `patron_id` on `order` as well would be a transitive dependency (3NF violation).

### Logs (Firestore, one collection per log, auto-generated document ids)

| Collection | Fields |
|---|---|
| `login_history` | person_id, ip_address, success, timestamp |
| `error_log` | person_id (nullable), end_point, error_message, stack_trace, timestamp |
| `audit_log` | staff_id, table_name, record_id, action, old_value, new_value, changed_at, ip_address |
| `permission_change_log` | person_id, changed_by, old_role, new_role, date_time |
| `performance_status_log` | performance_id, changed_by (staff id; null when the system flips `filled`), previous, new, date_time |

Rules: all times are Firestore `Timestamp` values in UTC; `old_value`/`new_value` are JSON strings and must **exclude `event.image`** (Firestore documents are capped at 1 MiB); never log passwords, hashes or JWTs. Because Firestore has no foreign keys, ids are plain integer fields.

### Relationships
- Event 1..* Performance; Event *..* Genre (event_genre); Performance *..* Performer (performance_cast)
- Venue 1..* Stage; Stage 1..* Section; Section 1..* Seat; Stage 1..* Performance
- Performance 1..* PerformanceSeat; Seat 1..* PerformanceSeat
- Performance 1..* PriceTier; Section 1..* PriceTier
- PerformanceSeat 0..1 Ticket; Cart 1..* CartItem; CartItem 1..1 Ticket
- Patron 1..* Cart; Cart 0..1 Order; Order 1..* OrderItem; Order 1..1 Payment
- PromoCode 0..* Order
- Person 1..* PasswordResetRequest; Person 0..1 EmailList

### Why PerformanceSeat is separate from Seat
A `Seat` is a physical chair that exists once. Its availability differs per performance. Separating them means you generate `PerformanceSeat` rows when you create a performance, and availability queries become simple filters on one table.

### Changes to the EERD required by this brief
| EERD as drawn | In this brief | Why |
|---|---|---|
| `price_tier` has stage_id and event_id | Removed | 3NF: both derive from section_id and performance_id. A service check ensures the section belongs to the performance's stage |
| `performance_seat.held_until` is `time` | `timestamptz` nullable | A time-of-day cannot represent a 10-minute hold crossing midnight |
| `payment.amount` is string | `numeric(10,2)` | Money type |
| `order` has no status/total | Adds status, total_amount, payment_reference, idempotency_key; adds `order_item` | API brief order model and idempotent checkout |
| `ticket_scan` | Not built | Ticket scanning out of scope |
| Log tables in Postgres | Moved to Firestore | Firestore stores logs |
| `login_history` and `error_log` have no timestamp | Timestamp added | A log needs a time |
| `person.password` | Nullable | Performers do not log in |
| `venue`: postal_code determines city and province | Left as one address block | Accepted trade-off; decompose only if the course requires strict 3NF |
| `performance.filled` | Kept as a stored flag | It is derived data, so it must be updated in the same transaction that sells the last seat |

### Rules
- Store all times in **UTC** (`DateTimeOffset` or `DateTime` with `Kind=Utc`; Npgsql requires UTC for `timestamptz`). Venues have no time zone column; convert only at the display layer.
- Store money as `decimal` with explicit precision, `numeric(10,2)`, never `double`. `promo_code.p_off` is a percentage, not money; convert it to `decimal` before calculating.
- Enums stored as strings for readability.

---

## 5. API Surface

All routes prefixed `/api`. Auth: **Public**, **Patron**, **Staff** (Staff or Admin), or **Admin**.

### Auth and account
| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | /auth/register | Public | Create person + patron (role `Patron`) |
| POST | /auth/login | Public | Returns JWT; writes `login_history` |
| GET | /auth/me | Patron | Current person |
| POST | /auth/password-reset/request | Public | Creates `password_reset_request`; sends token via mock email sender |
| POST | /auth/password-reset/confirm | Public | Sets new password; marks the request `Completed` |
| POST / DELETE | /account/email-list | Patron | Join / leave the email list |

### Catalog
| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | /events | Public | List `Live` events (paging, text search, genre filter; no image bytes) |
| GET | /events/{id} | Public | Event details, genres, upcoming performances, cast |
| GET | /events/{id}/image | Public | Event image |
| GET | /genres | Public | Active genres |
| GET | /performances/{id} | Public | Performance details |
| GET | /performances/{id}/seats | Public | Seats with section, row, seat number, status and price |

### Seat holds
| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | /performances/{id}/holds | Patron | Hold seats (body: seat IDs). Max seats per hold, e.g. 8 |
| GET | /holds/current | Patron | My active hold + seconds remaining |
| DELETE | /holds/{id} | Patron | Release my hold early (`{id}` is the cart id) |

### Checkout and orders
| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | /orders | Patron | Convert active hold into an order and pay. Optional `promoCode`; `Idempotency-Key` header |
| GET | /orders | Patron | My orders |
| GET | /orders/{id} | Patron | One order (must be owner) |

### Admin
| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST/PUT/DELETE | /admin/events | Staff | Manage events |
| POST/PUT | /admin/genres | Staff | Manage genres |
| POST | /admin/performers | Staff | Create performer (person + performer) |
| POST | /admin/venues | Staff | Create venue |
| POST | /admin/venues/{id}/stages | Staff | Add stage |
| POST | /admin/stages/{id}/sections | Staff | Add section |
| POST | /admin/sections/{id}/seats | Staff | Add seats (bulk: rows and seat numbers) |
| POST | /admin/performances | Staff | Create performance with a price per section; generates `price_tier` and `performance_seat` rows in one transaction |
| PUT | /admin/performances/{id}/cast | Staff | Set cast |
| POST/PUT | /admin/promo-codes | Staff | Manage promo codes |
| PUT | /admin/people/{id}/role | Admin | Change a person's role; writes `permission_change_log` |

Every Staff/Admin change writes an `audit_log` entry.

### Conventions
- Return proper status codes: 201 with `Location` for creates, 400 validation, 401/403 auth, 404 missing, **409 Conflict when a seat is already taken**.
- Use `ProblemDetails` (RFC 7807) for errors; ASP.NET supports it natively.
- Version the API (`/api/v1`) only if time allows.

---

## 6. The Core Problem: Seat Holding and No Double-Booking

### Required behavior
- A patron can hold up to N available seats for **exactly 10 minutes**.
- Two patrons requesting the same seat at the same moment: **exactly one succeeds**, the other gets 409.
- If the hold expires, the seats become available again without manual intervention.
- Checkout only succeeds if the patron's hold is still valid.
- A patron has at most one active hold (`cart` with status `Active`) at a time, and all its seats belong to one performance. Holding different seats releases the old hold first.

### Hold model (follows the EERD)
`performance_seat` has no "held by" column. A hold is expressed through the EERD chain:

`performance_seat (Held, held_until) → ticket → cart_item → cart (patron_id)`

Creating a hold, in one transaction: mark the seats `Held`, set `held_until`, create a `cart` (`Active`), and for each seat create a `ticket` (price copied from the seat's `price_tier`) and a `cart_item`. The holder of a seat is found by following that chain.

### State machine for a PerformanceSeat
```
Available --hold--> Held --checkout--> Sold
    ^                  |
    +--- expire/release+
```

### Approach options and tradeoffs

**A. Optimistic concurrency (EF Core concurrency token).** Use Postgres `xmin` as the token. It is a system column, so **no schema change** is needed. When two requests update the same row, the second fails with `DbUpdateConcurrencyException`, which you map to 409. *Pros:* idiomatic EF, no explicit locks, teaches a real concept. *Cons:* you must handle retries/conflicts correctly, and a multi-seat hold needs all-or-nothing handling inside a transaction.

**B. Atomic conditional UPDATE (single SQL statement).**
```sql
UPDATE performance_seat
SET status='Held', held_until = now() + interval '10 minutes'
WHERE performance_id=@perf AND seat_id = ANY(@seatIds)
  AND (status='Available' OR (status='Held' AND held_until < now()));
```
Compare affected rows to the number requested; if fewer, roll back and return 409. If a seat is taken over from an expired hold, delete its stale `ticket` and `cart_item` in the same transaction before creating the new ones (otherwise the unique index on `ticket.performance_seat_id` rejects the insert). *Pros:* simplest correct approach, atomic in the database, hard to get wrong. *Cons:* raw SQL via `ExecuteSqlRaw` (less "EF-pure").

**C. Pessimistic locking (`SELECT ... FOR UPDATE`).** Lock the rows, check, update. *Pros:* very explicit. *Cons:* more code, holds locks, needs careful transaction handling.

**Recommendation: B for the hold, with the unique indexes (`performance_seat (performance_id, seat_id)` and `ticket (performance_seat_id)`) and a transaction as the safety net**, and use optimistic concurrency (A, via `xmin`) on the checkout step. B is the least likely to contain a subtle bug, which matters more than elegance here. Whichever you choose, **test it with concurrent requests** (section 9).

### Treating expired holds
Two layers, both needed:
1. **Lazy expiry (correctness):** every availability query and hold attempt treats `Held` with `held_until < now()` as available. This guarantees correctness even if the background job is down.
2. **Background cleanup (tidiness):** a `BackgroundService` runs every 30-60 seconds, resets expired seats to `Available` (clearing `held_until`), deletes their `ticket` and `cart_item` rows, and marks the cart `Expired`.

### Checkout flow
1. Begin transaction.
2. Load the patron's `Active` cart. Verify all its seats are `Held` with `held_until > now()` (re-check inside the transaction).
3. If a promo code was supplied, verify it exists, is `active` and not past `expiry_date`. Compute in `decimal`: subtotal = sum of ticket prices; discount = `d_off` (flat) or subtotal × `p_off` / 100; total = max(subtotal − discount, 0), rounded to 2 decimals.
4. Call mock payment service with the total.
5. On success: mark seats `Sold`, mark cart `CheckedOut`, create `order` (`Paid`, total, payment_reference, promo_code_id, cart_id), `order_item` rows (price_at_purchase = ticket price), and a `payment` row (method, amount, date_time). If no seats remain `Available` or `Held`, set `performance.filled = true` and log a `performance_status_log` entry. Commit.
6. On failure: rollback; seats remain held until expiry (or release them explicitly).
7. Make the endpoint **idempotent** (accept an `Idempotency-Key` header, stored in `order.idempotency_key`) so a retried request returns the original order instead of creating a second one.

### Edge cases to handle explicitly
- Hold expires mid-checkout.
- Patron tries to check out seats held by someone else.
- Patron holds, then holds different seats (release the old hold first).
- Promo code is inactive, expired, or both `d_off` and `p_off` are null.
- Clock differences: always use the **database/server UTC time**, never client time.
- Seat IDs in the request that don't belong to the performance.

### Seed data and persistence
- Seed a base data set: roles, genres, a venue with its stage/section/seats, sample events and performances, sample promo codes, and one admin account (credentials from secrets, never committed).
- The seeder is **idempotent**: it inserts only what is missing (matched by a natural key such as role name, genre name, promo code, event title) and never updates or deletes existing rows.
- Anything users create or change (accounts, holds, orders, edited events) **stays**. Migrations and redeployments never reset data.
- Seeding runs as a deliberate step, like migrations, not on every app start in production.

### Firestore logging
- Services call `ILogSink` (e.g. `LogLogin(...)`, `LogError(...)`, `LogAudit(...)`); the sink enqueues to a `Channel<T>` and returns immediately.
- A `BackgroundService` drains the channel and writes to the five collections above. On a Firestore failure it retries a few times, then writes the entry to `ILogger` so it is not silently lost.
- Add a composite index when you query by `person_id` plus time (Firestore tells you which index is missing in its error message).
- Application logs (`ILogger`: every hold, expiry, checkout, conflict) are separate and go to the host's console output.

---

## 7. Learning Plan (3 to 4 weeks, part-time)

### Phase 0: Setup (1 evening)
- Install .NET 8 SDK, an IDE (Visual Studio, Rider, or VS Code + C# Dev Kit), Git, Postman, Docker.
- Create Neon project, copy connection string, store in user secrets.
- Create a Google Cloud/Firebase project with Firestore in native mode; create a service account and point `GOOGLE_APPLICATION_CREDENTIALS` at its key file (kept out of Git).
- `dotnet new webapi -controllers -n TheaterApi`, run it, open Swagger.

### Phase 1: C# and first endpoint (days 1-4)
- C# for Java developers crash course: properties, records, `async/await`, LINQ, nullable reference types, `using`, extension methods.
- Build `GET /events` returning hardcoded data, then add DI with a service interface.
- Goal: understand the request pipeline, DI, and controller basics.

### Phase 2: Data access and seeding (days 5-10)
- Add EF Core, Npgsql, `AppDbContext`, entities, relationships (including table-per-type for `person`).
- `dotnet ef migrations add Initial`, `dotnet ef database update`.
- Write the idempotent seeder. Replace the hardcoded service with real queries.
- Learn: `Include`, `AsNoTracking`, projection to DTOs with `Select`, paging.

### Phase 3: Auth (days 11-14)
- Person/patron/staff/role tables, register/login, `PasswordHasher`, JWT issuing, `[Authorize]`, roles, reading the person ID from claims.
- Password reset with the mock email sender; role-change endpoint.
- Goal: protected endpoints, a Staff/Admin split. (Log calls can hit a no-op `ILogSink` until Phase 5.)

### Phase 4: Seat holding, checkout and promo codes (days 15-20), the main event
- Performance seat generation, availability endpoint.
- Hold endpoint (approach B) building cart/ticket/cart_item, expiry background service, checkout, promo codes, mock payment.
- Concurrency tests.

### Phase 5: Firestore logging (days 21-23)
- `ILogSink`, Channel + writer service, the five collections, emulator-based tests.
- Wire login, error middleware, admin audit, role changes and `filled` flips into the sink.

### Phase 6: Hardening and delivery (days 24-28)
- Global error handling, validation, application logging.
- Tests, README, cloud deployment (see section 10), demo script.

### Concepts checklist (learn only when needed)
- [ ] DI lifetimes (Singleton / Scoped / Transient) and why a `DbContext` is Scoped
- [ ] Middleware pipeline order (auth before authorization, exception handler first)
- [ ] Model binding and validation
- [ ] DTO mapping (manual first; AutoMapper/Mapster is optional)
- [ ] EF change tracking and why `AsNoTracking` matters for reads
- [ ] EF table-per-type inheritance
- [ ] Transactions in EF (`BeginTransactionAsync`)
- [ ] Concurrency tokens (`xmin`)
- [ ] Hosted services and scoped services inside them (`IServiceScopeFactory`)
- [ ] `Channel<T>` producer/consumer
- [ ] Firestore collections, documents, queries and composite indexes
- [ ] Configuration and the options pattern
- [ ] `ILogger<T>` structured logging

### Neon-specific notes
- It's standard Postgres; nothing special to learn beyond the connection string.
- Free-tier compute **suspends when idle**, so the first request after a pause is slow. Expect it; consider a retry policy on connection (`EnableRetryOnFailure`).
- Use the **pooled** connection string for the app and the direct one for migrations if you hit issues.
- Keep `sslmode=require` in the connection string.

### Firestore-specific notes
- Firestore has no joins or foreign keys; that is why log documents carry plain ids.
- Authentication in .NET uses the service account credentials from the environment; never commit the key file.
- Use the Firebase **emulator** (`FIRESTORE_EMULATOR_HOST`) for local development and integration tests so tests never touch the real database.
- It has a free daily quota; check the current limits before load testing.

---

## 8. Non-Functional Requirements

- **Security:** hashed passwords (`PasswordHasher`), JWT with short expiry, HTTPS only, authorization checks on every order/hold endpoint (a patron must never read another patron's order, resolved via `order → cart → patron`), role checks on every admin route, rate limiting on login and password reset (built-in `AddRateLimiter`), CORS configured explicitly. Secrets are per instance.
- **Validation:** reject empty seat lists, oversized requests, seat IDs outside the performance, and promo codes that are inactive or expired.
- **Observability:** structured `ILogger` logs for every hold, expiry, checkout, and conflict; the five log entities in Firestore.
- **Performance:** indexes on `performance_seat (performance_id, status)` and `(held_until)`; unique indexes on `performance_seat (performance_id, seat_id)`, `ticket (performance_seat_id)`, `price_tier (section_id, performance_id)`, `promo_code (code)`, `order (idempotency_key)`; index on `cart (patron_id, status)` and `event (status, live_date)`; paginate event lists; exclude `image` bytes from list queries; use projections for the seat list.
- **Reliability:** correctness must not depend on the background job (lazy expiry) or on Firestore (log failures never fail a request).
- **Isolation:** no instance reads another instance's database, server, or Firestore data.

---

## 9. Testing Strategy

1. **Unit tests** (xUnit) for pure logic: price and promo calculation (flat, percent, floor at zero, rounding), hold-expiry rules.
2. **Integration tests** with `WebApplicationFactory` against a real Postgres (a separate Neon branch or a local Postgres container) and the Firestore emulator. In-memory EF providers do *not* reproduce concurrency or SQL behavior, so avoid them for the seat logic.
3. **Concurrency test (mandatory):** fire e.g. 20 simultaneous hold requests for the same seat with `Task.WhenAll`; assert exactly one 200 and nineteen 409s.
4. **Expiry test:** hold a seat, advance the time (inject an `IClock`/`TimeProvider` abstraction so tests can control time), assert the seat is available again and its ticket/cart_item are gone.
5. **Auth tests:** patron A cannot read patron B's order; patrons cannot call admin routes; only Admin can change roles.
6. **Seeding test:** run the seeder twice and after editing a seeded row; assert no duplicates and no overwritten edits.
7. **Logging tests:** a login, an error, an admin change and a role change each produce the expected Firestore document; a Firestore outage does not fail the request.

Tip: inject `TimeProvider` (built into .NET 8) instead of calling `DateTime.UtcNow` directly. It makes the 10-minute logic testable without waiting 10 minutes.

---

## 10. Deployment (required)

- Containerize with a simple Dockerfile (the SDK template can generate one).
- **One instance per venue:** each instance gets its own server, its own Neon database, and its own Firestore database (a separate project or a named database), configured entirely through environment variables: Neon connection string, JWT key, Firestore project and credentials.
- Host options: Azure App Service, Render, Railway, or Fly.io. Pick whichever has a free tier you can access as a student.
- Run migrations and seeding as deliberate steps, not automatically on app start in production.
- Never share a database, JWT key or Firestore credential between instances.

---

## 11. Milestones and Definition of Done

| Milestone | Done when |
|---|---|
| M1: Skeleton | API runs, Swagger loads, Neon connected |
| M2: Catalog | Events/performances/seat endpoints return real DB data; seeder is idempotent |
| M3: Auth | Register/login/password reset work; roles enforced; admin routes protected |
| M4: Holds | Hold works; concurrency test passes; expiry works both lazily and via background job |
| M5: Checkout | Hold becomes an order; promo codes apply; mock payment; idempotent |
| M6: Logging | All five log types land in Firestore; outages never fail requests |
| M7: Delivery | Tests green, README, deployed to the cloud as an isolated instance, demo script |

**Project is complete when:** a new patron can register, browse an event, hold seats, see them locked for others, check out (optionally with a promo code) within 10 minutes, and see the order; a competing patron is correctly blocked at every step; staff can manage events and performances; and the logs appear in Firestore, all on a deployed instance.

---

## 12. Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Spending too long learning C# basics | Follow one tutorial, then build; look things up only when stuck |
| Subtle double-booking bug | Use the atomic UPDATE approach; unique indexes as a safety net; write the concurrency test early |
| Stale ticket/cart_item rows after expiry | Delete them in both the lazy-expiry path and the cleanup job; test it |
| EF Core hides slow queries | Log SQL during development; inspect queries for the seat list |
| Neon cold starts look like bugs | Know about them; add connection retry |
| Firestore log loss or cost surprises | Channel + retry + `ILogger` fallback; use the emulator in dev; check quotas |
| EERD drift from this brief | Update the ERD with the changes in section 4 so the diagram and code agree |
| Seed data overwriting user changes | Idempotent seeder only; seeding test |
| Scope creep | Anything outside section 1's in-scope list is deferred |
| Time zone bugs | UTC everywhere; convert only at the display layer |

---

## 13. Suggested Build Order for Prompting

Build contract-first so each piece can be developed independently:

1. Entities + `AppDbContext` + first migration + idempotent seeder
2. DTOs and service interfaces (`ICatalogService`, `IAuthService`, `IHoldService`, `IOrderService`, `IPromoService`, `IPaymentService`, `IEmailSender`, `ILogSink`)
3. Catalog service + controllers
4. Auth (register, login, roles, password reset, email list)
5. Hold service + hold controller
6. Expiry background service
7. Checkout + promo codes + mock payment
8. Admin endpoints
9. Firestore log sink + writer, wired into earlier services
10. Tests
11. Error handling, README, Dockerfile, deployment

For each step, give your assistant: the project brief, the exact signatures of anything it depends on, and the instruction "output only this component."

---

## 14. Resources (pick one per topic, don't stack)

- Microsoft Learn: "Create a web API with ASP.NET Core" learning path
- Microsoft Learn: C# fundamentals (or a "C# for Java developers" guide)
- EF Core docs: Getting Started, Migrations, Concurrency Tokens, Inheritance (table-per-type)
- Npgsql EF Core provider docs: Postgres-specific mapping, `xmin` concurrency token
- Google Cloud docs: Firestore client library for C# (`Google.Cloud.Firestore`) and the Firebase Emulator Suite
- YouTube: Milan Jovanovic or Nick Chapsas for practical ASP.NET patterns
