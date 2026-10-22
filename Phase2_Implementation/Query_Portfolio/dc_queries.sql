-- 1. View
CREATE OR REPLACE VIEW v_performance_availability AS
SELECT p.id AS performance_id, 
    p.date_time,
    v.name AS venue,
    s.name AS stage,
    e.title AS event,
    p.status AS performance_status,
    COUNT(ps.id) FILTER (WHERE ps.status = 'AVAILABLE') AS available_seats,
    COUNT(ps.id) FILTER (WHERE ps.status = 'HELD') AS held_seats,
    COUNT(ps.id) FILTER (WHERE ps.status = 'SOLD') AS sold_seats,
    COUNT(ps.id) FILTER (WHERE ps.status = 'UNAVAILABLE') AS unavailable_seats,
    COUNT(ps.id) AS total_seats,
    COALESCE(SUM(t.price) FILTER (WHERE ps.status = 'SOLD'), 0) AS total_revenue,
    ROUND(100.0 * COUNT(ps.id) FILTER (WHERE ps.status = 'SOLD')
        / NULLIF(COUNT(ps.id) FILTER (WHERE ps.status <> 'UNAVAILABLE'), 0), 1) AS sold_percentage
FROM performance p
    JOIN event e ON p.event_id = e.id
    JOIN stage s ON p.stage_id = s.id
    JOIN venue v ON v.id = s.venue_id
    JOIN performance_seat ps ON p.id = ps.performance_id
    LEFT JOIN ticket t ON t.performance_seat_id = ps.id
    GROUP BY p.id, e.title, v.name, s.name, p.date_time, p.status;


SELECT event, venue, stage, date_time,
        available_seats, held_seats, sold_seats, total_seats,
        to_char(total_revenue, 'FM$999,999,990.00') AS revenue,
        sold_percentage || '%' AS sold_pct 
FROM v_performance_availability
WHERE performance_status = 'SCHEDULED'
ORDER BY v_performance_availability.sold_percentage ASC
LIMIT 10;


-- 2. Explain Analyze
EXPLAIN ANALYZE
SELECT * FROM cart_item WHERE cart_id = 1000;

CREATE INDEX idx_cart_item_cart ON cart_item(cart_id);

EXPLAIN ANALYZE
SELECT * FROM cart_item WHERE cart_id = 1000;


-- 3. Stored Procedure
CREATE OR REPLACE PROCEDURE p_release_expired_holds()
LANGUAGE plpgsql
AS $$
DECLARE
    v_released INT;
BEGIN
    DELETE FROM cart_item ci
        USING ticket t, performance_seat ps
        WHERE ci.ticket_id = t.id
            AND t.performance_seat_id = ps.id
            AND ps.status = 'HELD'
            AND ps.held_until < current_timestamp;

    DELETE FROM ticket t 
        USING performance_seat ps
        WHERE t.performance_seat_id = ps.id
            AND ps.status = 'HELD'
            AND ps.held_until < current_timestamp;

    UPDATE performance_seat
        SET status = 'AVAILABLE'
        WHERE status = 'HELD'
            AND held_until < current_timestamp;

    GET DIAGNOSTICS v_released = ROW_COUNT;

    UPDATE cart
        SET status = 'ABANDONED'
        WHERE status = 'ACTIVE'
            AND NOT EXISTS (SELECT 1 FROM cart_item ci WHERE ci.cart_id = cart.id);

    RAISE NOTICE 'Released % expired held seats and updated carts.', v_released;

END;
$$;


SELECT COUNT(*) FROM performance_seat WHERE status = 'HELD';  
CALL p_release_expired_holds(); 
SELECT COUNT(*) FROM performance_seat WHERE status = 'HELD';  


-- 4. Function and Trigger

CREATE OR REPLACE FUNCTION f_check_seat_availability()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE 
    v_status VARCHAR(25);
BEGIN
    SELECT status
    INTO v_status
    FROM performance_seat
    WHERE id = NEW.performance_seat_id FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Seat with ID % does not exist.', NEW.performance_seat_id;
    ELSIF v_status <> 'AVAILABLE' THEN
        RAISE EXCEPTION 'Seat is not available for booking. Current status: %', v_status;
    ELSE 
        UPDATE performance_seat
        SET status = 'HELD', held_until = current_timestamp + INTERVAL '10 minutes'
        WHERE id = NEW.performance_seat_id;
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_check_seat_availability ON ticket;

CREATE TRIGGER trg_check_seat_availability
BEFORE INSERT ON ticket
FOR EACH ROW
EXECUTE FUNCTION f_check_seat_availability();




-- Transaction to test a successful trigger and rollback
BEGIN;
INSERT INTO ticket (performance_seat_id, price)
SELECT id, 50.00 FROM performance_seat WHERE status = 'AVAILABLE' LIMIT 1;
SELECT ps.id, ps.status, ps.held_until
FROM performance_seat ps
WHERE ps.id = (SELECT performance_seat_id FROM ticket ORDER BY id DESC LIMIT 1);
ROLLBACK;


-- Transaction to test a failed trigger and rollback
BEGIN;
INSERT INTO ticket (performance_seat_id, price)
SELECT id, 50.00 FROM performance_seat WHERE status = 'SOLD' LIMIT 1;
ROLLBACK;


-- 5. Audit log update on promo_code table

CREATE OR REPLACE FUNCTION f_audit_changes()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO audit_log (staff_id, table_name, record_id, action, old_value, new_value, ip_address)
    VALUES (
        current_setting('app.staff_id')::INTEGER,
        UPPER(TG_TABLE_NAME),
        COALESCE(NEW.id, OLD.id),
        TG_OP,
        convert_to(to_jsonb(OLD)::text, 'UTF8'),
        convert_to(to_jsonb(NEW)::text, 'UTF8'),
        '127.0.0.1'
    );
    RETURN COALESCE(NEW, OLD);
END;
$$;

CREATE OR REPLACE TRIGGER trg_audit_promo_code
AFTER INSERT OR UPDATE OR DELETE ON promo_code
FOR EACH ROW EXECUTE FUNCTION f_audit_changes();



SET app.staff_id = '2';
UPDATE promo_code SET active = FALSE WHERE code = 'SAVE5';
SELECT action, table_name, record_id, old_value, new_value, staff_id, changed_at
FROM audit_log 
WHERE staff_id = 2 AND table_name = 'PROMO_CODE'
ORDER BY changed_at DESC;
UPDATE promo_code SET active = TRUE WHERE code = 'SAVE5';