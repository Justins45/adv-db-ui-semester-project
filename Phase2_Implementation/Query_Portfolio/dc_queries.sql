-- Active: 1792539057597@@localhost@5433@advdb
-- 1. View
-- 
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


SELECT COUNT(*) FROM performance_seat WHERE status = 'HELD';   -- 9
CALL p_release_expired_holds(); 
SELECT COUNT(*) FROM performance_seat WHERE status = 'HELD';   -- 9