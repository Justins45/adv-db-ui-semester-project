# Phase 3: Object-Oriented Design, JSONB and Vector Search

## Purpose

Phase 3 extends the Phase 2 theatre ticketing database (Tradition LTD.) in three ways:

- **Part A, object-oriented design:** a `Person → Patron / Staff` hierarchy using class table inheritance. It shows inheritance, polymorphism (a function that works across the hierarchy) and encapsulation (functions as class operations, and business rules enforced in the database).
- **Part B, JSONB:** patron preferences that vary from patron to patron (accessibility, notifications, content advisories) are stored in `patron.preferences`. A CHECK constraint validates the JSON and a GIN index speeds up searches.
- **Part C, vector search:** 2,150 theatre reviews are embedded with an open model and stored in a pgvector column with an HNSW index. The searches are evaluated with precision@5, a filtered search, a hybrid full-text + vector search, and a comparison with and without the index.
- **Part D, bringing it together:** one query that combines vector similarity, a condition on the class hierarchy and a JSONB condition.

## Folder contents

| Folder | Contents |
|---|---|
| `Design_Plan/` | Design plan (PDF): class hierarchy and mapping strategy, JSONB vs relational, embedding model, distance measure and index, and where the authoritative data lives |
| `Diagrams/` | UML class diagram and data pipeline diagram, as PlantUML source (`.puml`) and rendered images (`.png`) |
| `SQL_Scripts/` | Schema changes, Part A, Part B and Part D scripts (see the run order below) |
| `Embeddings/` | Source review text (`theater_reviews.csv`), the vector load file (`theater_reviews_with_vectors.sql`), and the notebook that embeds search queries (`embed_query.ipynb`) |
| `Query_Results/` | Part C results: precision@5 tables, filtered and hybrid search, and the with/without-index comparison |
| `Technical_Report/` | Technical report |

## Setup

### Prerequisites

Phase 3 builds on the Phase 2 database. You need:

- A local PostgreSQL 18 server with the pgvector extension installed (we used PostgreSQL 18.6 and pgvector 0.8.7)
- An empty database to load the scripts into, for example one created with `createdb theater`
- The Phase 2 schema and seed data loaded first (scripts 1 and 2 below)

### Run order

Run the scripts in this order, from the repository root. Each one must finish before the next starts.

| # | Script | What it does |
|---|---|---|
| 1 | `Phase2_Implementation/SQL_Scripts/AdvDataPhase2Schema.sql` | Phase 2 tables |
| 2 | `Phase2_Implementation/SQL_Scripts/AdvDataPhase2SeedData.sql` | Phase 2 sample data (400 people) |
| 3 | `Phase3_OO_JSONB_Vector/SQL_Scripts/new_table_updates.sql` | Adds patron and staff columns, enables pgvector, creates `review` with `embedding vector(384)`, and creates the indexes |
| 4 | `Phase3_OO_JSONB_Vector/Embeddings/theater_reviews_with_vectors.sql` | Loads the 2,150 reviews with their vectors |
| 5 | `Phase3_OO_JSONB_Vector/SQL_Scripts/inheritance-polymorphism-encapsulation.sql` | Part A functions and demo queries |
| 6 | `Phase3_OO_JSONB_Vector/SQL_Scripts/jsonb_implementation.sql` | Part B: sample preferences, CHECK constraint, GIN index and JSONB queries |
| 7 | `Phase3_OO_JSONB_Vector/SQL_Scripts/part_d_combined_query_filled.sql` | Part D query with the query vector filled in |

Scripts 5 and 6 also run their demo statements, which change patron 1's points balance and reminder hours. `part_d_combined_query.sql` is the template version with a `:query_vector` placeholder. See [Regenerating embeddings](#regenerating-embeddings).

### Enabling pgvector

`new_table_updates.sql` runs:

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

This only enables pgvector. The extension must already be installed on the PostgreSQL server:

- Arch Linux: `sudo pacman -S pgvector`
- Debian/Ubuntu (PGDG repository): `sudo apt install postgresql-18-pgvector`
- macOS (Homebrew): `brew install pgvector`
- Other systems: see the [pgvector install guide](https://github.com/pgvector/pgvector#installation)

To check that it's enabled:

```sql
SELECT extversion FROM pg_extension WHERE extname = 'vector';
```

### Loading the vectors

Run `Embeddings/theater_reviews_with_vectors.sql` as a whole file. In pgAdmin 4, open it in the Query Tool and run the whole file. With psql:

```sh
psql -d theater -f Phase3_OO_JSONB_Vector/Embeddings/theater_reviews_with_vectors.sql
```

The file inserts each review with its vector using `INSERT ... ON CONFLICT (id) DO UPDATE`, so it is safe to run again. It then creates the HNSW index if it doesn't already exist, moves the `review.id` identity counter past the loaded ids, and runs a self-check query. Expect `reviews = 2150`, `with_vectors = 2150` and `dimensions = 384`.

## Embeddings

| | |
|---|---|
| Model | [`sentence-transformers/all-MiniLM-L6-v2`](https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2) |
| Version | Hugging Face revision `1110a243fdf4706b3f48f1d95db1a4f5529b4d41` (used for the query vectors) |
| Dimensions | 384 (`vector(384)`), unit length |
| Distance and index | Cosine (`<=>`), HNSW with `vector_cosine_ops` (defaults `m = 16`, `ef_construction = 64`) |
| Text embedded | The review **body** |
| Where it ran | Locally, on CPU, in VS Code with Python. No paid embedding service was used. |

**Review vectors:** `theater_reviews_with_vectors.sql` was generated on 2026-11-17 in VS Code with all-MiniLM-L6-v2. Each stored vector is identical to an embedding of that review's body alone, so the body is the embedded text. The file's header comment says "title vectors", but that is out of date.

**Query vectors:** [`Embeddings/embed_query.ipynb`](Embeddings/embed_query.ipynb) embeds a search sentence with the same model and checks that it has 384 dimensions and unit length. It then replaces every `:query_vector` placeholder in a SQL file and writes a ready-to-run copy, for example `part_d_combined_query_filled.sql`.

### Regenerating embeddings

1. Install the dependencies: `pip install -r Phase3_OO_JSONB_Vector/Embeddings/requirements.txt`
2. Open `Embeddings/embed_query.ipynb` in VS Code or Jupyter.
3. Set `QUERY_TEXT`, `SQL_FILE` and `OUT_FILE` in step 2 of the notebook, then run all cells.

The first run downloads the model, about 90 MB.

> **TODO:** The script that produced `theater_reviews_with_vectors.sql` from `theater_reviews.csv` is not in the repository yet. Add it, or extend the notebook to embed every review body and write the load file, so the review vectors can be regenerated from the raw text.

## Text dataset

| | |
|---|---|
| File | `Embeddings/theater_reviews.csv` (324 KB) |
| Columns | `id`, `person_id`, `rating`, `title`, `body` |
| Rows | 2,150 reviews from 399 distinct reviewers (`person_id` → `person.id`) |
| Ratings | 1★ 185, 2★ 238, 3★ 188, 4★ 487, 5★ 1,052 |
| Body length | 7–34 words (average 20.9). No empty or very short bodies, and none over the model's 256-token limit, so no text was truncated. |
| Duplicates | 1,392 distinct bodies. The text is built from a small set of template sentences, so many reviews share a body (and therefore a vector), and tied distances appear in search results. |
| Venue | The source has no venue column. Every review is assigned `venue_id = 1`, although the review text names several venues. |
| Source | Synthetic (generated) data. **TODO:** name the tool used to generate it and describe how it was used. |
| Licence | **TODO:** state the licence (for example, generated by the team and released with this repository under its licence). |

## Dependencies and environment

| Component | Version |
|---|---|
| PostgreSQL | 18, running locally (tested on 18.6) |
| pgvector | 0.8 or later, needed for iterative index scans (tested on 0.8.7) |
| Client | pgAdmin 4 (or psql) |
| Python | 3.10 or later (tested on 3.14) |
| Python packages | `sentence-transformers` (tested 6.3.0, with `transformers` 5.23.0 and `torch` 2.15.0 CPU), plus Jupyter or the VS Code Jupyter extension |
| Diagrams | PlantUML (VS Code extension or plantuml.com) |

## Credentials

Never commit passwords, API keys or connection strings.

- **pgAdmin 4:** enter the host, port, user and password when you register your local server. pgAdmin stores them in its own settings, outside this repository.
- **psql:** set the connection details as environment variables in your own shell, so they never appear in a file in this repository:
  ```sh
  export PGHOST=localhost PGPORT=5432 PGUSER=<user> PGDATABASE=theater
  export PGPASSWORD=<password>    # or put the password in ~/.pgpass instead
  psql -f <script.sql>
  ```
  If you keep these in a file, use a local `.env` file and make sure it is listed in `.gitignore`.
- **Notebook:** the notebook needs no credentials. It writes a SQL file that you run in your own SQL client.
