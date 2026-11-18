-- =============================================================================
-- Part D: Bringing it together
--
-- Request (plain English):
--   "I use a wheelchair. Before I buy tickets, show me what other patrons
--    who also need wheelchair-accessible seating said about how comfortable
--    the venue was and how well they could see the stage. Don't include
--    reviews written by staff."
--
-- The query combines all three parts of Phase 3:
--   1. Vector similarity (Part C): reviews ranked by cosine distance (<=>)
--      to the embedded search text (see "Why only part of the request is
--      embedded" below).
--   2. Class hierarchy (Part A): the reviewer must be a Patron and must NOT
--      also be Staff. The hierarchy overlaps, so a staff member also has a
--      patron row. Staff are excluded because their reviews are not
--      independent customer opinions.
--   3. JSONB condition (Part B): the patron's preferences must contain
--      {"accessibility": {"wheelchair_accessible": true}}, which can use
--      the GIN index idx_patron_preferences_gin.
--
-- Run order: new_table_updates.sql -> theater_reviews_with_vectors.sql
--            -> inheritance-polymorphism-encapsulation.sql
--            -> jsonb_implementation.sql -> this file
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Query vector
-- The query_vector placeholder below must be replaced with the embedding of
--
--   "comfortable, accessible venue with a clear view of the stage"
--
-- made with the same model as the reviews
-- (sentence-transformers/all-MiniLM-L6-v2, 384 dimensions).
-- Run Embeddings/embed_query.ipynb: it embeds the text and writes
-- part_d_combined_query_filled.sql, ready to run, next to this file.
--
-- To test without an embedding tool, use an existing review's vector as the
-- query instead (a "more like this" search). Swap the line in the q CTE for:
--   SELECT embedding AS v FROM review WHERE id = 1878
-- Review 1878 reads "Clean, well-organized venue with great sightlines from
-- the balcony."
-- -----------------------------------------------------------------------------

-- -----------------------------------------------------------------------------
-- Why only part of the request is embedded
--
-- The request combines three conditions, and each one is handled by a
-- different part of the query:
--
--   "patrons who also need wheelchair-accessible seating"
--       -> JSONB filter on patron.preferences
--   "Don't include reviews written by staff"
--       -> hierarchy filter (in patron, not in staff)
--   "how comfortable the venue was and how well they could see the stage"
--       -> vector similarity (the only part embedded)
--
-- Only the third part is about what a review says. The other two are facts
-- about the reviewer, which are stored in the database, not in the review
-- text. So the search text is that part of the request, rewritten in the
-- kind of words a review would use.
--
-- Test: embedding the full request instead of the short search text.
--
--   Search text          #1 distance  #2 distance  Top 5 review ids
--   Short (used here)    0.497        0.513        1878, 1225, 857, 1101, 1489
--   Full request         0.541        0.571        1878, 1225, 857, 1101, 949
--
-- The same two relevant reviews rank first either way, but the full request
-- matches them less closely. Words such as "wheelchair", "tickets", "staff"
-- and "don't include" never appear in the reviews, so they pull the query
-- vector away from them. Embedding models also don't understand negation:
-- "don't include reviews by staff" just adds "staff" as a topic. That is why
-- the staff exclusion is a SQL condition and not part of the embedded text.
-- -----------------------------------------------------------------------------

WITH q AS (
    SELECT :query_vector::vector(384) AS v
),
-- Apply the hierarchy and JSONB filters first. MATERIALIZED makes Postgres
-- compute this candidate set before ranking. Only a handful of reviews pass
-- these filters, and an HNSW index scan applies filters only after it
-- collects its ~40 nearest neighbours (hnsw.ef_search), so a plain
-- ORDER BY ... LIMIT through the index could return few or no rows. Ranking
-- the small candidate set exactly avoids that and gives exact distances.
candidates AS MATERIALIZED (
    SELECT r.id, r.person_id, r.title, r.body, r.rating, r.embedding
    FROM review  AS r
    JOIN patron  AS pa ON pa.id = r.person_id                    -- (2) reviewer is a Patron
    WHERE NOT EXISTS (SELECT 1 FROM staff AS s WHERE s.id = r.person_id)  -- (2) ...and not Staff
      AND pa.preferences @> '{"accessibility": {"wheelchair_accessible": true}}'::jsonb  -- (3) JSONB
)
SELECT
    c.id                                        AS review_id,
    pe.first_name || ' ' || pe.last_name        AS reviewer,
    c.rating,
    c.title,
    c.body,
    ROUND((c.embedding <=> q.v)::numeric, 3)    AS cosine_distance       -- (1) vector similarity
FROM candidates AS c
JOIN person     AS pe ON pe.id = c.person_id
CROSS JOIN q
ORDER BY c.embedding <=> q.v
LIMIT 5;


-- -----------------------------------------------------------------------------
-- Supporting checks for the report
-- -----------------------------------------------------------------------------

-- How many reviews pass the hierarchy + JSONB filters (the candidate set size).
SELECT count(*) AS candidate_reviews
FROM review AS r
JOIN patron AS pa ON pa.id = r.person_id
WHERE NOT EXISTS (SELECT 1 FROM staff AS s WHERE s.id = r.person_id)
  AND pa.preferences @> '{"accessibility": {"wheelchair_accessible": true}}'::jsonb;

-- Shows what the hierarchy condition removes: wheelchair users who are also
-- staff, and how many reviews they wrote.
SELECT pa.id AS person_id, count(r.id) AS reviews_excluded
FROM patron AS pa
JOIN staff  AS s ON s.id = pa.id
LEFT JOIN review AS r ON r.person_id = pa.id
WHERE pa.preferences @> '{"accessibility": {"wheelchair_accessible": true}}'::jsonb
GROUP BY pa.id;

-- Plan for the main query (the placeholder is filled the same way).
EXPLAIN (ANALYZE, BUFFERS)
WITH q AS (
    SELECT :query_vector::vector(384) AS v
),
candidates AS MATERIALIZED (
    SELECT r.id, r.person_id, r.title, r.body, r.rating, r.embedding
    FROM review  AS r
    JOIN patron  AS pa ON pa.id = r.person_id
    WHERE NOT EXISTS (SELECT 1 FROM staff AS s WHERE s.id = r.person_id)
      AND pa.preferences @> '{"accessibility": {"wheelchair_accessible": true}}'::jsonb
)
SELECT c.id, c.title, c.embedding <=> q.v AS dist
FROM candidates AS c
CROSS JOIN q
ORDER BY c.embedding <=> q.v
LIMIT 5;
