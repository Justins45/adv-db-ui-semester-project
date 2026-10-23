--Finds the event that made the most money and tracks the overall sales volume
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

--finds application endpoints making an above average # of errors to prioritize developer resources
--Nested Query and Aggregation
SELECT
    end_point,
    COUNT(id) AS error_count
FROM ERROR_LOG
GROUP BY end_point
HAVING COUNT (id) > (
    SELECT AVG(endpoint_errors) 
    FROM (
        SELECT COUNT(id) AS endpoint_errors
        FROM ERROR_LOG
        GROUP BY end_point
    ) AS subquery
)
ORDER BY error_count DESC;

--Tracks the # of manual system changes made by staff members to see workload distribution and also security
--Complex Join
SELECT
    p.first_name || ' ' || p.last_name AS staff_name,
    r.role AS operational_role,
    COUNT(a.id) AS total_changes,
    MAX(a.changed_at) as last_change_timestamp
FROM AUDIT_LOG a
JOIN STAFF s ON a.staff_id = s.id
JOIN PERSON p ON s.id = p.id
JOIN ROLE r ON s.role_id = r.id
GROUP BY staff_name, r.role
ORDER BY last_change_timestamp DESC;

--Calculates the affect of promo codes applied against standard ticket orders to evaluate marketing
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

--Measures the volume of seats that were temp held by a user but then where abandoned and taken back to the pool, showing possible UI or payment issues
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