-- 6. 
--6. Finds the event that made the most money and tracks the overall sales volume
--Aggregation and Joins
SELECT
    e.title AS event_title,
    e.status AS event_status,
    COUNT(t.id) AS total_tickets_sold,
    SUM(t.price) AS total_revenue
FROM EVENT e
JOIN PERFORMANCE p ON e.id = p.event_id
JOIN PERFORMANCE_SEAT ps ON p.id = ps.performance_id
JOIN TICKET t ON ps.id = t.performance_seat_id
WHERE ps.status = 'SOLD' --maybe make capital?
GROUP BY e.title, e.status
ORDER BY total_revenue DESC NULLS LAST;

-- 7.
--7. Finds patrons whose total ticket spending is above the average spending across all patrons
-- Nested Query, Aggregation, and HAVING
SELECT 
    p.id AS patron_id,
    pers.first_name || ' ' || pers.last_name AS patron_name,
    COUNT(o.id) AS total_orders_placed,
    SUM(pay.amount) AS total_amount_spent
FROM PATRON p
JOIN PERSON pers ON p.id = pers.id
JOIN CART c ON p.id = c.patron_id
JOIN "ORDER" o ON c.id = o.cart_id
JOIN PAYMENT pay ON o.id = pay.order_id
GROUP BY p.id, pers.first_name, pers.last_name
HAVING SUM(pay.amount) > (
    SELECT AVG(patron_total)
    FROM (
        SELECT SUM(pay_sub.amount) AS patron_total
        FROM PATRON p_sub
        JOIN CART c_sub ON p_sub.id = c_sub.patron_id
        JOIN "ORDER" o_sub ON c_sub.id = o_sub.cart_id
        JOIN PAYMENT pay_sub ON o_sub.id = pay_sub.order_id
        GROUP BY p_sub.id
    ) AS subquery
)
ORDER BY total_amount_spent DESC;

-- 8.
--8. Tracks the volume of ticket orders processed or managed by each staff member/role to evaluate operational workload
-- Complex Join and Aggregation
SELECT
    p.first_name || ' ' || p.last_name AS staff_name,
    r.role AS operational_role,
    COUNT(psl.id) AS total_status_updates_made,
    MAX(psl.date_time) AS latest_update_timestamp
FROM STAFF s
JOIN PERSON p ON s.id = p.id
JOIN ROLE r ON s.role_id = r.id
JOIN PERFORMANCE_STATUS_LOG psl ON s.id = psl.changed_by
GROUP BY p.first_name, p.last_name, r.role
ORDER BY total_status_updates_made DESC;

-- 9.
--9. Calculates the affect of promo codes applied against standard ticket orders to evaluate marketing
--Filtering and Aggregation 
SELECT
    pc.code AS promo_code,
    COUNT(o.id) AS total_orders,
    SUM(pay.amount) AS total_revenue_made,
    MAX(pc.d_off) AS flat_discount_offered
FROM PROMO_CODE pc
LEFT JOIN "ORDER" o ON pc.id = o.promo_code_id
JOIN PAYMENT pay ON o.id = pay.order_id
WHERE pc.active = TRUE
GROUP BY pc.code
ORDER BY total_revenue_made DESC; 

-- 10.
--10. Measures the volume of seats that were temp held by a user but then where abandoned and taken back to the pool, showing possible UI or payment issues
--Time Extraction and Filtering
SELECT
    v.name AS venue,
    s.name AS stage,
    p.date_time AS performance_date,
    COUNT(ps.id) FILTER (WHERE ps.status = 'AVAILABLE' AND ps.held_until < CURRENT_TIMESTAMP) AS abandoned_holds,
    COUNT(ps.id) AS total_seats
FROM PERFORMANCE p
JOIN STAGE s ON p.stage_id = s.id
JOIN VENUE v ON s.venue_id = v.id
JOIN PERFORMANCE_SEAT ps ON p.id = ps.performance_id
GROUP BY v.name, s.name, p.date_time
ORDER BY abandoned_holds DESC; 