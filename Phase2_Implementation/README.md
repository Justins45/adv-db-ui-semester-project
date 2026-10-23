# Phase 2 - Implementation and Query Mastery

## Table of Contents

1. [Setup Instructions](#setup-instructions)
2. [Datasets](#datasets)
3. [Dependencies and Environment Notes](#dependencies-and-environment-notes)
4. [Supplying Credentials](#supplying-credentials)

## Setup Instructions

**Hosted Prostgress - Neon Cloud Database**

Create a project, copy the connection string, and supply it as described in [Supplying Credentials](#supplying-credentials).

### Seed the Firestore collection

1. Create a Firebase project and enable Firestore.
2. Provide credentials (see [Supplying Credentials](#supplying-credentials)).
3. Replace Firebase project name in `TheaterApi/appsettings.json`
3. Flip the `SeedFirestore` flag in `TheaterApi/appsettings.Development.json`
4. Run Project, and flip flag to false (it will re-write to it is okay to run again with flag as true)

### 5. Run the API

```bash
dotnet run
```

Then open Swagger at `https://localhost:5045/swagger`.

## Datasets

### PostgreSQL

| Item | Details |
|------|---------|
| Tables | asdasdasda |
| Row counts | 67 |
| Source / generation method | AI Generated data, hand made Schema |

### Firestore

| Item | Details |
|------|---------|
| Collection(s) | `audit_log`, `permission_change_log`, `performance_status_log`, `error_log` |
| Document count | 104 Documents Total |
| Source / generation method | AI generated Data |

## Environment Notes

- **PostgreSQL:** Cloud Hosted Neon Database
- **Firebase / Firestore:** project setup requirements
- **OS tested on:** Linux 7.2.11-arch1-1
- **Ports used:** dotnet defualt **port:5045**


## Supplying Credentials

**No passwords, API keys, connection strings, or Firebase service-account key files are committed to this repository.** Provide them locally using one of the methods below.

### PostgreSQL connection string

**.NET User Secrets (recommended for local development)**

```bash
cd TheaterApi
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Neon" "Host=<your-host>.neon.tech;Database=<db>;Username=<user>;Password=<pw>;SSL Mode=Require"
```

### Firebase service-account key

1. In the Firebase console, go to Project settings, then Service accounts, then Generate new private key.
2. Save the JSON file **outside the repo**, or inside it under a path listed in `.gitignore`.
3. Point the app to it:

```bash
# macOS / Linux
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/service-account.json"

# Windows (PowerShell)
$env:GOOGLE_APPLICATION_CREDENTIALS = "C:\path\to\service-account.json"
```

