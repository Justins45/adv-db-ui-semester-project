-- =============================================================================
-- Person hierarchy: Person -> Patron / Staff
--
-- Mapping strategy: table-per-subclass (class-table inheritance).
--   * person holds everything shared (identity + login fields)
--   * patron and staff each hold only their own columns
--   * each subtype's primary key IS a foreign key to person.id (shared PK),
--     so a person can have a patron row, a staff row, or both (overlapping)
--
-- Requires these tables to exist first:
--   venue (id)   -- referenced by staff.assigned_venue
--   role  (id)   -- referenced by patron.role_id and staff.role_id
-- =============================================================================

-- ---------------------------------------------------------------------------
-- Superclass - Already inside the database, no changes made here but table creation is showed anyways
-- ---------------------------------------------------------------------------
--
-- CREATE TABLE person (
--     id          INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
--     first_name  VARCHAR(100) NOT NULL,
--     last_name   VARCHAR(100) NOT NULL,
--     email       VARCHAR(255) UNIQUE NOT NULL CHECK (email LIKE '%@%'),
--     phone       VARCHAR(30),
--     password    VARCHAR(255) NOT NULL
-- );

-- ---------------------------------------------------------------------------
-- Subclass: Patron - Table already exists, adding new columns
-- ---------------------------------------------------------------------------
ALTER TABLE patron
ADD preferences JSONB NOT NULL DEFAULT '{}'::jsonb,
ADD loyalty_tier      VARCHAR(50),
ADD points_balance    INTEGER NOT NULL DEFAULT 0 CHECK (points_balance >= 0),
ADD membership_expiry TIMESTAMPTZ;

-- ---------------------------------------------------------------------------
-- Subclass: Staff  - Table already exists, adding new columns
-- ---------------------------------------------------------------------------
BEGIN;
-- Add employee ID AND backfill via prefix + employee ID number
ALTER TABLE staff
    ADD COLUMN employee_id    VARCHAR(50) GENERATED ALWAYS AS ('SFF_' || id::text) STORED UNIQUE NOT NULL,
    ADD COLUMN hire_date      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ADD COLUMN assigned_venue INTEGER REFERENCES venue (id);

-- Fill existing staff to a venue.
UPDATE staff
SET assigned_venue = (SELECT MIN(id) FROM venue)
WHERE assigned_venue IS NULL;

ALTER TABLE staff ALTER COLUMN assigned_venue SET NOT NULL;

COMMIT;

-- ---------------------------------------------------------------------------
-- Indexes
-- (Postgres does not index foreign key columns automatically.)
-- ---------------------------------------------------------------------------
CREATE INDEX idx_staff_assigned_venue  ON staff (assigned_venue);

-- GIN index so queries on patron.preferences (@>, ?, ?|, ?&) don't seq-scan
CREATE INDEX idx_patron_preferences_gin ON patron USING GIN (preferences);
