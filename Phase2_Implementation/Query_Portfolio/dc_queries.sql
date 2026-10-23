-- 1. View
-- v_performance_availability: one row per performance with a seat-status
-- breakdown, revenue from sold tickets, and the percentage of sellable seats sold.
CREATE OR REPLACE VIEW v_performance_availability AS
SELECT p.id AS performance_id,
    p.date_time,
    v.name AS venue,
    s.name AS stage,
    e.title AS event,
    p.status AS performance_status,
    -- FILTER lets a single pass over the seats count each status separately
    COUNT(ps.id) FILTER (WHERE ps.status = 'AVAILABLE') AS available_seats,
    COUNT(ps.id) FILTER (WHERE ps.status = 'HELD') AS held_seats,
    COUNT(ps.id) FILTER (WHERE ps.status = 'SOLD') AS sold_seats,
    COUNT(ps.id) FILTER (WHERE ps.status = 'UNAVAILABLE') AS unavailable_seats,
    COUNT(ps.id) AS total_seats,
    -- Revenue only counts tickets on SOLD seats; COALESCE turns "no sales" into 0 instead of NULL
    COALESCE(SUM(t.price) FILTER (WHERE ps.status = 'SOLD'), 0) AS total_revenue,
    -- Sold % is measured against sellable seats (UNAVAILABLE excluded).
    -- NULLIF avoids a divide-by-zero when a performance has no sellable seats.
    ROUND(100.0 * COUNT(ps.id) FILTER (WHERE ps.status = 'SOLD')
        / NULLIF(COUNT(ps.id) FILTER (WHERE ps.status <> 'UNAVAILABLE'), 0), 1) AS sold_percentage
FROM performance p
    JOIN event e ON p.event_id = e.id
    JOIN stage s ON p.stage_id = s.id
    JOIN venue v ON v.id = s.venue_id
    JOIN performance_seat ps ON p.id = ps.performance_id
    -- LEFT JOIN so seats without a ticket (available/unavailable) are still counted
    LEFT JOIN ticket t ON t.performance_seat_id = ps.id
    GROUP BY p.id, e.title, v.name, s.name, p.date_time, p.status;


-- Example usage: the 10 upcoming (scheduled) performances with the weakest sales,
-- with revenue formatted as currency and the sold percentage shown with a % sign.
SELECT event, venue, stage, date_time,
        available_seats, held_seats, sold_seats, total_seats,
        to_char(total_revenue, 'FM$999,999,990.00') AS revenue,
        sold_percentage || '%' AS sold_pct
FROM v_performance_availability
WHERE performance_status = 'SCHEDULED'
ORDER BY v_performance_availability.sold_percentage ASC
LIMIT 10;


-- 2. Explain Analyze
-- Compare the query plan for looking up a cart's items before and after indexing cart_id.
-- Before the index, Postgres has to scan the whole cart_item table (Seq Scan).
EXPLAIN ANALYZE
SELECT * FROM cart_item WHERE cart_id = 1000;

-- cart_id is a foreign key, and Postgres doesn't index foreign keys automatically
CREATE INDEX idx_cart_item_cart ON cart_item(cart_id);

-- After the index, the planner can jump straight to matching rows (Index / Bitmap Index Scan)
EXPLAIN ANALYZE
SELECT * FROM cart_item WHERE cart_id = 1000;


-- 3. Stored Procedure
-- p_release_expired_holds: cleans up seats whose hold has expired so they can be
-- booked again. Rows are removed child-first (cart_item -> ticket) so foreign keys
-- aren't violated, then the seats are freed and any carts left empty are abandoned.
CREATE OR REPLACE PROCEDURE p_release_expired_holds()
LANGUAGE plpgsql
AS $$
DECLARE
    v_released INT;
BEGIN
    -- Remove cart items that point to tickets on expired held seats
    DELETE FROM cart_item ci
        USING ticket t, performance_seat ps
        WHERE ci.ticket_id = t.id
            AND t.performance_seat_id = ps.id
            AND ps.status = 'HELD'
            AND ps.held_until < current_timestamp;

    -- Remove the tickets themselves for those expired holds
    DELETE FROM ticket t
        USING performance_seat ps
        WHERE t.performance_seat_id = ps.id
            AND ps.status = 'HELD'
            AND ps.held_until < current_timestamp;

    -- Put the seats back on sale
    UPDATE performance_seat
        SET status = 'AVAILABLE'
        WHERE status = 'HELD'
            AND held_until < current_timestamp;

    -- Capture how many seats the UPDATE above released
    GET DIAGNOSTICS v_released = ROW_COUNT;

    -- Any active cart that no longer has items is marked abandoned
    UPDATE cart
        SET status = 'ABANDONED'
        WHERE status = 'ACTIVE'
            AND NOT EXISTS (SELECT 1 FROM cart_item ci WHERE ci.cart_id = cart.id);

    RAISE NOTICE 'Released % expired held seats and updated carts.', v_released;

END;
$$;


-- Test: count held seats, run the cleanup, then count again to see the difference
SELECT COUNT(*) FROM performance_seat WHERE status = 'HELD';
CALL p_release_expired_holds();
SELECT COUNT(*) FROM performance_seat WHERE status = 'HELD';


-- 4. Function and Trigger

-- f_check_seat_availability: runs before a ticket is inserted. It rejects the
-- ticket if the seat doesn't exist or isn't AVAILABLE; otherwise it places a
-- 10-minute hold on the seat.
CREATE OR REPLACE FUNCTION f_check_seat_availability()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_status VARCHAR(25);
BEGIN
    -- FOR UPDATE locks the seat row so two concurrent bookings can't both
    -- see it as AVAILABLE and grab the same seat
    SELECT status
    INTO v_status
    FROM performance_seat
    WHERE id = NEW.performance_seat_id FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Seat with ID % does not exist.', NEW.performance_seat_id;
    ELSIF v_status <> 'AVAILABLE' THEN
        RAISE EXCEPTION 'Seat is not available for booking. Current status: %', v_status;
    ELSE
        -- Hold the seat; p_release_expired_holds frees it if checkout doesn't finish in time
        UPDATE performance_seat
        SET status = 'HELD', held_until = current_timestamp + INTERVAL '10 minutes'
        WHERE id = NEW.performance_seat_id;
    END IF;
    -- Returning NEW lets the ticket insert go ahead unchanged
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_check_seat_availability ON ticket;

-- BEFORE INSERT so a bad booking is stopped before the ticket row is written
CREATE TRIGGER trg_check_seat_availability
BEFORE INSERT ON ticket
FOR EACH ROW
EXECUTE FUNCTION f_check_seat_availability();




-- Transaction to test a successful trigger and rollback
-- Book an available seat, confirm it is now HELD with a held_until time,
-- then roll back so the test data is left untouched.
BEGIN;
INSERT INTO ticket (performance_seat_id, price)
SELECT id, 50.00 FROM performance_seat WHERE status = 'AVAILABLE' LIMIT 1;
SELECT ps.id, ps.status, ps.held_until
FROM performance_seat ps
WHERE ps.id = (SELECT performance_seat_id FROM ticket ORDER BY id DESC LIMIT 1);
ROLLBACK;


-- Transaction to test a failed trigger and rollback
-- Trying to book a SOLD seat should raise the "not available" exception
BEGIN;
INSERT INTO ticket (performance_seat_id, price)
SELECT id, 50.00 FROM performance_seat WHERE status = 'SOLD' LIMIT 1;
ROLLBACK;


-- 5. Audit log update on promo_code table

-- f_audit_changes: generic audit trigger function that writes a row to audit_log
-- for every insert, update, or delete, storing the row before and after as JSON.
-- The acting staff member is read from the session setting app.staff_id, which
-- must be set first (SET app.staff_id = '...') or current_setting() will error.
CREATE OR REPLACE FUNCTION f_audit_changes()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO audit_log (staff_id, table_name, record_id, action, old_value, new_value, ip_address)
    VALUES (
        current_setting('app.staff_id')::INTEGER,
        UPPER(TG_TABLE_NAME),                       -- table that fired the trigger
        COALESCE(NEW.id, OLD.id),                   -- NEW is null on DELETE, OLD is null on INSERT
        TG_OP,                                      -- 'INSERT', 'UPDATE' or 'DELETE'
        convert_to(to_jsonb(OLD)::text, 'UTF8'),    -- row as JSON, stored as bytes
        convert_to(to_jsonb(NEW)::text, 'UTF8'),
        '127.0.0.1'                                 -- placeholder; no real client IP is captured
    );
    -- Return value is ignored for AFTER triggers, but returning the row is the usual convention
    RETURN COALESCE(NEW, OLD);
END;
$$;

-- AFTER trigger so only changes that actually succeeded get logged
CREATE OR REPLACE TRIGGER trg_audit_promo_code
AFTER INSERT OR UPDATE OR DELETE ON promo_code
FOR EACH ROW EXECUTE FUNCTION f_audit_changes();



-- Test: act as staff member 2, deactivate a promo code, check the audit log,
-- then reactivate it (the reactivation is also logged).
BEGIN;
SET app.staff_id = '2';
UPDATE promo_code SET active = FALSE WHERE code = 'SAVE5';
SELECT action, table_name, record_id, old_value, new_value, staff_id, changed_at
FROM audit_log
WHERE staff_id = 2 AND table_name = 'PROMO_CODE'
ORDER BY changed_at DESC;
UPDATE promo_code SET active = TRUE WHERE code = 'SAVE5';
COMMIT;