# TheaterApi

ASP.NET Core API for the Tradition LTD. theater ticketing system. It uses PostgreSQL as the source of truth and Firestore for logs. The project currently targets .NET 10 (`net10.0`).

## Requirements

- .NET 10 SDK and the repository's local `dotnet-ef` tool (`dotnet tool restore`)
- A PostgreSQL 15+ database with the `vector` extension enabled
- A Firestore project and application credentials for deployed use, or the Firestore emulator for local development
- Docker for building/running the container and for Testcontainers integration tests

## Configuration and secrets

Configuration uses the normal ASP.NET Core environment-variable mapping (`:` becomes `__`). Do not commit credentials or place them in `appsettings*.json`.

| Setting | Environment variable | Purpose |
|---|---|---|
| `ConnectionStrings:NeonDb` | `ConnectionStrings__NeonDb` | Neon PostgreSQL connection string |
| `Jwt:SigningKey` | `Jwt__SigningKey` | JWT signing key |
| `Jwt:Issuer` | `Jwt__Issuer` | JWT issuer |
| `Jwt:Audience` | `Jwt__Audience` | JWT audience |
| `Firestore:ProjectId` | `Firestore__ProjectId` | Firestore project ID |
| Google credentials | `GOOGLE_APPLICATION_CREDENTIALS` | Path to a service-account JSON file mounted outside the image/repository |
| `Cors:AllowedOrigins:0` | `Cors__AllowedOrigins__0` | First allowed browser origin; add indexed entries for more origins |
| `MockPayment:AcceptedCardNumber` | `MockPayment__AcceptedCardNumber` | Local secret for the configured mock-payment success value |
| `SeedAccounts:AdminPassword` | `SeedAccounts__AdminPassword` | Optional local admin seed password |
| `SeedAccounts:StaffPassword` | `SeedAccounts__StaffPassword` | Optional local staff seed password |
| `SeedAccounts:PatronPassword` | `SeedAccounts__PatronPassword` | Optional local patron seed password |

For local development, keep values in User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:NeonDb" "<Neon connection string>"
dotnet user-secrets set "Jwt:SigningKey" "<random signing key>"
dotnet user-secrets set "Jwt:Issuer" "TheaterApi"
dotnet user-secrets set "Jwt:Audience" "TheaterApi"
dotnet user-secrets set "Firestore:ProjectId" "<Firestore project ID>"
dotnet user-secrets set "Cors:AllowedOrigins:0" "<approved browser origin>"
```

Set `GOOGLE_APPLICATION_CREDENTIALS` in the shell to the credential file path. Never copy that file into the repository or container image. CORS has no wildcard or implicit default origin; provide the allowed origins for the deployment.

## Build, migrate, and seed

Restore the local tools and build:

```bash
dotnet tool restore
dotnet build
dotnet test
```

Apply migrations deliberately after configuring the database connection:

```bash
dotnet ef database update
```

Seeding is also deliberate and never runs automatically on app startup:

```bash
dotnet run -- seed
```

Seed inputs must be present at the paths expected by `SeedService` and `FirestoreSeeder`. Seeding skips when the people table is already populated. Do not run migrations or seeding against a database with data you have not been authorized to reset.

## Run locally and open Swagger

```bash
dotnet run
```

In Development, Swagger UI is available at `https://localhost:5045/swagger` when using the checked-in launch profile. The API routes use the `/api` prefix.

For Firestore-dependent tests, start the Firestore emulator and set `FIRESTORE_EMULATOR_HOST`. Postgres integration tests start a pgvector Testcontainer and require Docker. Tests must never point at a production Neon database or Firestore project.

## Docker

Build the image from this directory:

```bash
docker build -t theater-api .
```

Run it with deployment configuration supplied as environment variables. Mount the service-account file read-only from outside the repository:

```bash
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__NeonDb \
  -e Jwt__SigningKey \
  -e Jwt__Issuer \
  -e Jwt__Audience \
  -e Firestore__ProjectId \
  -e Cors__AllowedOrigins__0 \
  -e GOOGLE_APPLICATION_CREDENTIALS=/run/credentials/firestore.json \
  -v /path/outside/repository/firestore.json:/run/credentials/firestore.json:ro \
  theater-api
```

Provide the referenced variables through the deployment platform's secret/configuration facility. The image does not run migrations or seed data at startup. Run those steps as deliberate release operations before routing traffic to a new instance. Configure a separate Neon database, JWT key, and Firestore database/project for each venue deployment.

## Demo walkthrough

1. Start the API and open Swagger.
2. Register a patron, then log in and authorize Swagger with the returned bearer token.
3. Browse active events and performances, then inspect a performance's seats and prices.
4. Hold available seats and verify that a second patron cannot hold the same seats.
5. Check out within the 10-minute hold window using a locally configured successful mock-payment value; verify the order and its items.
6. Log in as staff and create or edit a catalogue entry. Verify that the audit entry is written to Firestore.

The mock payment service accepts only the success value defined by the product spec; keep that value in local secrets and never place it in documentation, logs, or source control.
