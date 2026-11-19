
# Part B: JSONB Query Results

## Query 1: Containment Query (@>)
Find patrons that need wheelchair accessible seating.

**Result:**

![Query 1 Result](screenshots/partB_q1.png)

## Query 2: JSON Path Query (@?)
Find patrons that have selected Flashing Lights as a content advisory.

**Result:**

![Query 2 Result](screenshots/partB_q2.png)

## Query 3: JSONB Array to Rows and Relational JOIN
List each patron's content advisories.

**Result:**

![Query 3 Result](screenshots/partB_q3.png)

## Query 4: Update Nested JSONB Value (jsonb_set)
Update patron ID 1's event reminder time to 12 hours.

**Result:**

![Query 4 Result](screenshots/partB_q4.png)

## EXPLAIN Plan: Containment Query
Check whether PostgreSQL used the GIN index for the containment query.

**Result:**

![EXPLAIN Plan](screenshots/partB_explain.png)

PostgreSQL used a sequential scan instead of the GIN index. This likely happened because the patron table has around 400 rows, so a sequential scan is more efficient.
