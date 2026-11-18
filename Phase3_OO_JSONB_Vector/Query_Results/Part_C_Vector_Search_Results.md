# Part C: Vector Search Results

## Task 1: Five natural-language searches, top 5 each

**Relevance rule:** a result is only considered relevant if its text addresses the main topic of the query. Where a query combines two separate claims (search 2), both have to be present. The generated reviews reuse a small set of sentences, so near duplicate results with tied distances are expected in this dataset.

### Search 1: "a moving story that made me cry"

| Rank | ID | Distance | Text (excerpt) | Relevant |
|---|---|---|---|---|
| 1 | 1013 | 0.537 | Funny, sad, and surprisingly hopeful… | Y |
| 2 | 2050 | 0.544 | Funny, sad, and surprisingly hopeful… | Y |
| 3 | 1078 | 0.544 | Funny, sad, and surprisingly hopeful… | Y |
| 4 | 1031 | 0.544 | Funny, sad, and surprisingly hopeful… | Y |
| 5 | 719 | 0.544 | Funny, sad, and surprisingly hopeful… | Y |

precision@5 = 5/5 = 1.00. This is the loosest Y: the text is emotional ("sad") but never mentions crying.

### Search 2: "the acting was great but the seats were uncomfortable"

| Rank | ID | Distance | Text (excerpt) | Relevant |
|---|---|---|---|---|
| 1 | 233 | 0.331 | Every actor felt like a real person… | N |
| 2 | 961 | 0.353 | Every actor felt like a real person… | N |
| 3 | 1464 | 0.355 | The lead actor was phenomenal… | N |
| 4 | 336 | 0.358 | Every actor felt like a real person… | N |
| 5 | 243 | 0.361 | The seats were cramped and the view was partly blocked… | N |

precision@5 = 0/5 = 0.00. Each result covers only one half of the query. No review in the dataset praises the acting and complains about the seats together, so the embedding lands between the two ideas.

### Search 3: "funny show that is good for the whole family"

| Rank | ID | Distance | Text (excerpt) | Relevant |
|---|---|---|---|---|
| 1 | 444 | 0.590 | The supporting cast stole the show… | N |
| 2 | 1938 | 0.595 | Such a fun night out. The audience was lively… | N |
| 3 | 1471 | 0.595 | Such a fun night out. The audience was lively… | N |
| 4 | 1254 | 0.595 | Such a fun night out. The audience was lively… | N |
| 5 | 1203 | 0.595 | Such a fun night out. The audience was lively… | N |

precision@5 = 0/5 = 0.00. None of the results mention family, children, or humor. "Fun night out" is the closest match, but it’s not what the query was specifically asking for.

### Search 4: "disappointing and not worth the ticket price"

| Rank | ID | Distance | Text (excerpt) | Relevant |
|---|---|---|---|---|
| 1 | 818 | 0.374 | The ticket price was high for what we got… | Y |
| 2 | 32 | 0.374 | The ticket price was high for what we got… | Y |
| 3 | 111 | 0.384 | The ticket price was high for what we got… | Y |
| 4 | 1014 | 0.384 | The ticket price was high for what we got… | Y |
| 5 | 827 | 0.387 | The ticket price was high for what we got… | Y |

precision@5 = 5/5 = 1.00.

### Search 5: "beautiful lighting and set design"

| Rank | ID | Distance | Text (excerpt) | Relevant |
|---|---|---|---|---|
| 1 | 1765 | 0.409 | The lighting design alone is worth the price of admission… | Y |
| 2 | 603 | 0.409 | The lighting design alone is worth the price of admission… | Y |
| 3 | 1594 | 0.427 | The lighting design alone is worth the price of admission… | Y |
| 4 | 784 | 0.441 | The lighting design alone is worth the price of admission… | Y |
| 5 | 722 | 0.441 | The lighting design alone is worth the price of admission… | Y |

precision@5 = 5/5 = 1.00. Lighting and set design were treated as one topic (visual design).

### Average precision@5

(1.00 + 0.00 + 0.00 + 1.00 + 1.00) / 5 = 0.60 (60%)

The weak searches (2 and 3) are kept in the results on purpose. The dataset is synthetic and was made from a small set of repeated sentences, so some queries have no truly relevant review, and the embeddings return the closest partial matches.

## Task 2: Vector search combined with a relational filter

**Query:** "a clever script with a strong ending" **Filter:** rating >= 4 AND venue_id = 1 **Rows returned:** 10

```sql
SELECT id, title, rating, embedding <=> :query_vector AS dist
FROM review
WHERE rating >= 4 AND venue_id = 1
ORDER BY embedding <=> :query_vector
LIMIT 10;
```

EXPLAIN (ANALYZE, BUFFERS) with the vector literal shortened:

```
Limit  (cost=306.42..328.22 rows=10) (actual time=0.666..3.686 rows=10)
  ->  Index Scan using idx_review_embedding on review
        (cost=306.42..3660.22 rows=1539) (actual rows=10)
        Order By: (embedding <=> '[...]'::vector)
        Filter: ((rating >= 4) AND (venue_id = 1))
Execution Time: 3.716 ms
```

**What the plan tells us**

- **The vector index handles the ordering.** The `Order By: embedding <=> ...` line means that the HNSW index returns reviews nearest first, so no separate sorting step is needed. The Limit stops after the first 10 rows that pass.
- **The relational condition is a filter, not part of the index search.** The `Filter:` line is checked against each row the index returns. The HNSW index doesn’t know about rating or venue_id, so it can’t use them to narrow the search. It’s post filtering.
- **It worked well here because the filter isn’t selective.** The planner guessed that 1,539 of 2,150 rows would pass (about 72%), so the first candidates were enough to fill 10 rows. No "Rows Removed by Filter" line shows up, which suggests none of the examined candidates were rejected. The data has a single venue, so venue_id = 1 passes every row and rating >= 4 does the real filtering.
- **It is fast:** 3.7 ms in total.
- **Limitation:** with a very selective filter, the index might return fewer than 10 rows, because HNSW examines only a limited number of candidates (hnsw.ef_search, default 40). Raising ef_search, using pgvector's iterative index scans (version 0.8 and later), or adding a partial index would address this.

## Task 3: Hybrid search (full-text + vector) vs vector-only

**Method.** Vector only search ranks by cosine distance to the query embedding. Hybrid search runs a full text phrase match (phraseto_tsquery, ranked with ts_rank_cd) with the vector search and combines the two rankings with reciprocal rank fusion, where each result earns 1/(60 + rank) from each list and then the two scores are added up.

**Test queries.** Each query needs both an exact show title and an exact venue name. A result counts as a hit only if its text contains both phrases.

| Query | Matching reviews in table | Vector-only hits in top 10 | Hybrid hits in top 10 |
|---|---|---|---|
| Second Spring + Cedar Point Playhouse | 3 | 2 | 3 |
| A Quiet Storm + Summit Stage | 8 | 5 | 8 |
| A Hundred Small Lights + Summit Stage | 8 | 6 | 8 |
| Northbound + Old Harbour Theatre | 4 | 3 | 4 |
| **Total** | **23** | **16 (70%)** | **23 (100%)** |

**Worked example: "A Quiet Storm" + "Summit Stage" (8 matching reviews existed)**

- **Vector only top 10 (ids):** 342 ✔, 1032 ✔, 47 ✔, 1431 ✔, 1008 ✔, 1480 ✘, 1612 ✘, 742 ✘, 474 ✘, 679 ✘. It found 5 of the 8 matches. The misses include other shows at the Summit Stage (id 679 is Second Spring) and a different show reviewed elsewhere.
- **Hybrid top 10 (ids):** 47 ✔, 342 ✔, 1032 ✔, 1008 ✔, 1431 ✔, 285 ✔, 336 ✔, 727 ✔, 1480 ✘, 1612 ✘. It found all 8 matches and ranked them first, and the last two slots fall back to vector-ranked results.

**Why vector only misses.** The embedding mixes "show title + venue" into one overall meaning, so the right venue with a different show, or the right show elsewhere, looks similar enough to a true match. Full text search needs both phrases to show up, so it doesn’t have this problem. Vector search is better at searching by meaning and full text search is better at searching by exact wording, and the hybrid combines them both together. On single distinctive phrases such as "Second Spring" or "Northbound" alone, both methods scored 10/10 in my tests, so the hybrid's advantage shows up when several exact terms must be matched together.

## Task 4: Search with and without the index

**Query:** "a heartfelt performance with a great ending", top 10 by cosine distance. The no-index run used `SET LOCAL enable_indexscan = off` and `enable_bitmapscan = off` inside a transaction, so the index stayed in place and was only ignored for that query.

| | Top 10 ids |
|---|---|
| With HNSW index | 697, 1516, 500, 710, 1531, 1088, 1421, 1876, 1285, 1132 |
| Without index (exact) | 697, 1516, 500, 710, 1531, 1088, 1421, 1132, 1285, 139 |

**Overlap:** 9 of 10 (recall@10 = 90%). The only difference is that the index returned id 1876 where the exact search returned id 139.

Plans (vector literal shortened):

```
WITH index:    Limit (cost=306.42..321.98)
                 -> Index Scan using idx_review_embedding
WITHOUT index: Limit (cost=611.34..611.36)
                 -> Sort (cost=611.34..616.71)
                      -> Seq Scan on review (cost=0.00..564.88)
```

**What this shows**

- Without the index, Postgres calculates the distance to all 2,150 rows with a sequential scan and then sorts them. The result is exact, but the cost grows along with table size.
- With the index, it walks the HNSW graph and reads rows in nearest first order, avoiding the full scan and sort. The estimated cost is about 322 against about 611 without it. These are planner cost units, not milliseconds, and plain EXPLAIN was used, so no timings were measured at all.
- The trade-off is accuracy for speed. HNSW is approximate. Tt follows a graph toward the nearest neighbors and can miss a true neighbor, which it did with id 139. On a table this small the exact scan is pretty cheap, so the index only gives a modest gain here, and the benefit grows with the number of rows.
- Recall can be tuned. Increasing hnsw.ef_search makes the search look at more candidates, trading some speed for a better chance of matching the exact results.
