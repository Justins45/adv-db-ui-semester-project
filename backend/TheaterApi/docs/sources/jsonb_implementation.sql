-- Part B: JSONB Implementation

-- 1. Sample JSONB preferences for existing patrons
WITH sample_preferences (sample_number, sample_data) AS (
    VALUES
    (1, '{"accessibility":{"wheelchair_accessible":false,"companion_seat_required":false},"notifications":{"event_reminders":true,"reminder_hours_before":24,"price_drop_alerts":true},"content_advisories":["Flashing Lights","Loud Noises"]}'::jsonb),
    (2, '{"accessibility":{"wheelchair_accessible":true,"companion_seat_required":true},"notifications":{"event_reminders":true,"reminder_hours_before":48,"price_drop_alerts":false},"content_advisories":["Loud Noises"]}'::jsonb),
    (3, '{"accessibility":{"wheelchair_accessible":false,"companion_seat_required":false},"notifications":{"event_reminders":false,"reminder_hours_before":24,"price_drop_alerts":false},"content_advisories":[]}'::jsonb),
    (4, '{"accessibility":{"wheelchair_accessible":true,"companion_seat_required":false},"notifications":{"event_reminders":true,"reminder_hours_before":2,"price_drop_alerts":true},"content_advisories":["Flashing Lights"]}'::jsonb),
    (5, '{"accessibility":{"wheelchair_accessible":false,"companion_seat_required":false},"notifications":{"event_reminders":true,"reminder_hours_before":12,"price_drop_alerts":false},"content_advisories":["Loud Noises","Flashing Lights"]}'::jsonb),
    (6, '{"accessibility":{"wheelchair_accessible":false,"companion_seat_required":false},"notifications":{"event_reminders":false,"reminder_hours_before":48,"price_drop_alerts":true},"content_advisories":[]}'::jsonb),
    (7, '{"accessibility":{"wheelchair_accessible":true,"companion_seat_required":true},"notifications":{"event_reminders":true,"reminder_hours_before":6,"price_drop_alerts":true},"content_advisories":["Flashing Lights"]}'::jsonb),
    (8, '{"accessibility":{"wheelchair_accessible":false,"companion_seat_required":false},"notifications":{"event_reminders":true,"reminder_hours_before":24,"price_drop_alerts":false},"content_advisories":["Loud Noises"]}'::jsonb)
),
numbered_patrons AS (
    SELECT 
        id, 
        ROW_NUMBER() OVER (ORDER BY id) AS sample_number
    FROM patron
)
UPDATE patron AS p
SET preferences = s.sample_data
FROM numbered_patrons AS n
JOIN sample_preferences AS s ON s.sample_number = n.sample_number
WHERE p.id = n.id
    AND p.preferences = '{}'::jsonb;


-- 2. CHECK constraint to validate JSONB
ALTER TABLE patron
DROP CONSTRAINT IF EXISTS validate_patron_preferences_jsonb;

ALTER TABLE patron
ADD CONSTRAINT validate_patron_preferences_jsonb
CHECK (
    preferences = '{}'::jsonb OR COALESCE (
        jsonb_typeof(preferences) = 'object'

        AND jsonb_typeof(preferences -> 'accessibility') = 'object'
        AND jsonb_typeof(preferences #> '{accessibility,wheelchair_accessible}') = 'boolean'
        AND jsonb_typeof(preferences #> '{accessibility,companion_seat_required}') = 'boolean'

        AND jsonb_typeof(preferences -> 'notifications') = 'object'
        AND jsonb_typeof(preferences #> '{notifications,event_reminders}') = 'boolean'
        AND jsonb_typeof(preferences #> '{notifications,reminder_hours_before}') = 'number'
        AND (preferences #>> '{notifications,reminder_hours_before}')::numeric
            BETWEEN 1 AND 168 
        AND jsonb_typeof(preferences #> '{notifications,price_drop_alerts}') = 'boolean'

        AND jsonb_typeof(preferences -> 'content_advisories') = 'array'
        AND NOT jsonb_path_exists(
            preferences,
            '$.content_advisories[*] ? (@.type() != "string")'
        ), 
        
        FALSE
    )
);


-- 3. GIN index on patron preferences columns (already created in new_table_updates.sql)
CREATE INDEX IF NOT EXISTS idx_patron_preferences_gin
    ON patron USING GIN (preferences);


-- 4. JSONB Statements

-- Query 1: Find patrons that need wheelchair accessible seating (containment query)
SELECT 
    p.id AS patron_id, 
    pe.first_name, 
    pe.last_name
FROM patron AS p
JOIN person AS pe ON pe.id = p.id
WHERE p.preferences @> '{"accessibility": {"wheelchair_accessible": true}}'::jsonb
ORDER BY p.id;


-- Query 2: Find patrons that have selected flashing lights as a content advisory (JSON path query)
SELECT 
    p.id AS patron_id, 
    pe.first_name, 
    pe.last_name
FROM patron AS p
JOIN person AS pe ON pe.id = p.id
WHERE p.preferences @? '$.content_advisories[*] ? (@ == "Flashing Lights")'
ORDER BY p.id;


-- Query 3: List each patron's content advisories (JSONB array to rows and relational JOIN)
SELECT 
    p.id AS patron_id, 
    pe.first_name, 
    pe.last_name,
    advisory.value AS content_advisory
FROM patron AS p
JOIN person AS pe ON pe.id = p.id
CROSS JOIN LATERAL jsonb_array_elements_text (p.preferences -> 'content_advisories') AS advisory(value)
ORDER BY p.id, advisory.value;


-- Query 4: Update a patron's (example: id=1) event reminder time to 12 hours (updating one nested value)
UPDATE patron AS p
SET preferences = jsonb_set (
    p.preferences,
    '{notifications,reminder_hours_before}',
    '12'::jsonb
)
WHERE p.id = 1
RETURNING p.id AS patron_id,
    p.preferences #>> '{notifications,reminder_hours_before}'
    AS updated_reminder_hours_before;


-- 5. EXPLAIN plan for containment query
-- Result: 
--      The planner didn't use the GIN index and instead used a sequential scan.
--      This likely happened because we have a small dataset (patron table has around 400 rows)
--      so a sequential scan is more efficient.
EXPLAIN (ANALYZE, BUFFERS)
SELECT p.id AS patron_id, pe.first_name, pe.last_name
FROM patron AS p
JOIN person AS pe ON pe.id = p.id
WHERE p.preferences @> '{"accessibility": {"wheelchair_accessible": true}}'::jsonb
ORDER BY p.id;


