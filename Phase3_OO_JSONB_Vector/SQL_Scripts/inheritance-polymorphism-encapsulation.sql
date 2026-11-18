-- ----------------------------------------------------------
-- This Document will provide SQL examples of how the 
-- database contains the three parts required. 
--  - Inheritance
--  - Polumorphism 
--  - Encapsulation
-- Written by Justin S.
-- ---------------------------------------------------------

-- ---------------------------------------------------------
-- Inheritance
-- Using getting the parent data out of the child.
SELECT
  p.id AS person_id, 
	p.first_name || ' ' || p.last_name AS person_name,
	s.employee_id
FROM staff s
JOIN person p on p.id = s.id;

-- ---------------------------------------------------------
-- Polumorphism
-- Create function to check person status in the system,
--  wheather they are a patron or staff account.
-- ---------------------------------------------------------
CREATE OR REPLACE FUNCTION person_status(p_person_id INTEGER)
RETURNS TEXT
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    is_staff   BOOLEAN;
    is_patron  BOOLEAN;
BEGIN
    SELECT EXISTS (SELECT 1 FROM staff WHERE id = p_person_id)
      INTO is_staff;

    SELECT EXISTS (SELECT 1 FROM patron WHERE id = p_person_id)
      INTO is_patron;

    IF is_staff THEN
        RETURN 'Staff member';
    ELSIF is_patron THEN
        RETURN 'Patron Only';
    ELSE
        RETURN NULL;
    END IF;
END;
$$;

-- Same call across all persons, takes in the person id 
-- and outputs the corrisponding value
SELECT p.id,
       p.first_name || ' ' || p.last_name AS full_name,
       person_status(p.id)                AS status
FROM person p
ORDER BY p.id;

-- ---------------------------------------------------------
-- Encapsulation
-- Function adds or removes points from patron
-- ---------------------------------------------------------
CREATE OR REPLACE FUNCTION add_points(p_patron_id INTEGER, p_points INTEGER)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_new_balance INTEGER;
BEGIN
    UPDATE patron
       SET points_balance = points_balance + p_points
     WHERE id = p_patron_id
    RETURNING points_balance INTO v_new_balance;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No patron with id %', p_patron_id;
    END IF;

    RETURN v_new_balance;
END;
$$;

-- Add 90 points to patron with ID of 1
SELECT add_points(1, 90);
-- Remove 90 points from patron
SELECT add_points(1, -40);

