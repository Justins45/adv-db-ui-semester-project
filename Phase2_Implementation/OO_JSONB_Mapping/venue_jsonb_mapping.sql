-- JSONB Mapping for VENUE Entity

DROP TABLE IF EXISTS venue_jsonb_mapping;

-- Create table with venue attributes and a JSONB column to store seating configuration
CREATE TABLE venue_jsonb_mapping (
    id              INTEGER PRIMARY KEY,
    name            VARCHAR(50)     NOT NULL,
    address         VARCHAR(255)    NOT NULL,
    city            VARCHAR(100)    NOT NULL,
    province        VARCHAR(50)     NOT NULL,
    postal_code     VARCHAR(20)     NOT NULL,
    seating_config  JSONB           NOT NULL
);

-- Insert sample venue with stages, sections, and seats
-- JSONB data has optional accessability attributes for certain sections or individual seats
INSERT INTO venue_jsonb_mapping (
    id, 
    name, 
    address, 
    city, 
    province,
    postal_code, 
    seating_config
)
VALUES (
    1,
    'Playwright West',
    '123 Theater Way SW',
    'Calgary',
    'Alberta',
    'T2P 1J9',
    '{
        "stages": [
            {
                "id": 1,
                "name": "Main Stage",
                "sections": [
                    {
                        "id": 3,
                        "name": "Balcony",
                        "accessible": false,
                        "seats": [
                            {
                                "id": 49,
                                "row": "A",
                                "seat_number": 1
                            },
                            {
                                "id": 50,
                                "row": "A",
                                "seat_number": 2,
                                "accessible": true
                            }
                        ]
                    }
                ]
            },
            {
                "id": 2,
                "name": "Studio Stage",
                "sections": [
                    {
                        "id": 4,
                        "name": "Orchestra",
                        "seats": [
                            {
                                "id": 73,
                                "row": "A",
                                "seat_number": 1,
                                "accessible": true
                            },
                            {
                                "id": 74,
                                "row": "A",
                                "seat_number": 2,
                                "accessible": true
                            }
                        ]
                    }
                ]
            }
        ]
    }'::jsonb -- convert the JSON string to JSONB
);

-- Query: Compare accessible seating capacity across all stages, from greatest to least
-- Counts and displays the number of accessibile seats for each stage 
SELECT
    stage ->> 'name' AS stage_name,
    COUNT(*) FILTER (WHERE seat @> '{"accessible": true}') AS accessible_seats
FROM venue_jsonb_mapping,
     jsonb_array_elements(seating_config -> 'stages') AS stage,
     jsonb_array_elements(stage -> 'sections') AS section,
     jsonb_array_elements(section -> 'seats') AS seat
GROUP BY venue_jsonb_mapping.id, stage ->> 'id', stage ->> 'name'
ORDER BY accessible_seats DESC;