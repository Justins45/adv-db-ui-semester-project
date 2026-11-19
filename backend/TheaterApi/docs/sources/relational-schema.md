# Relational Schema
```
PERSON (
    id INTEGER PK, 
    first_name VARCHAR(50) NOT NULL, 
    last_name VARCHAR(50) NOT NULL, 
    email VARCHAR(255) UNIQUE NOT NULL, 
    phone VARCHAR(20) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL
)

EMAIL_LIST (
    person_id INTEGER PK, FK → PERSON(id) NOT NULL
        ON DELETE CASCADE ON UPDATE CASCADE, 
    added TIMESTAMPTZ NOT NULL
)

LOGIN_HISTORY (
    id INTEGER PK, 
    person_id INTEGER FK → PERSON(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    ip_address VARCHAR(50) NOT NULL, 
    success BOOLEAN NOT NULL
)

ERROR_LOG (
    id INTEGER PK, 
    person_id INTEGER FK → PERSON(id)
        ON DELETE SET NULL ON UPDATE CASCADE, 
    end_point VARCHAR(255) NOT NULL, 
    error_message VARCHAR(500) NOT NULL, 
    stack_trace BYTEA
)

PASSWORD_RESET_REQUEST (
    id INTEGER PK, 
    person_id INTEGER FK → PERSON(id) NOT NULL
        ON DELETE CASCADE ON UPDATE CASCADE, 
    time TIMESTAMPTZ NOT NULL, 
    success BOOLEAN NOT NULL, 
    status VARCHAR(50) NOT NULL
)

ROLE (
    id INTEGER PK, 
    role VARCHAR(50) UNIQUE NOT NULL
)

PERMISSION_CHANGE_LOG (
    id INTEGER PK, 
    person_id INTEGER FK → PERSON(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    changed_by INTEGER FK → PERSON(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    old_role INTEGER FK → ROLE(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    new_role INTEGER FK → ROLE(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    date_time TIMESTAMPTZ NOT NULL
)

PATRON (
    id INTEGER PK, FK → PERSON(id)
        ON DELETE CASCADE ON UPDATE CASCADE, 
    role_id INTEGER FK → ROLE(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE
)

STAFF (
    id INTEGER PK, FK → PERSON(id)
        ON DELETE CASCADE ON UPDATE CASCADE, 
    role_id INTEGER FK → ROLE(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE
)

PERFORMER (
    id INTEGER PK, FK → PERSON(id)
        ON DELETE CASCADE ON UPDATE CASCADE, 
    role VARCHAR(50)
)

VENUE (
    id INTEGER PK, 
    name VARCHAR(50) NOT NULL, 
    address VARCHAR(255) NOT NULL, 
    city VARCHAR(100) NOT NULL, 
    province VARCHAR(50) NOT NULL, 
    postal_code VARCHAR(20) NOT NULL
)

STAGE (
    id INTEGER PK, 
    venue_id INTEGER FK → VENUE(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    name VARCHAR(50) NOT NULL
)

SECTION (
    id INTEGER PK, 
    stage_id INTEGER FK → STAGE(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    name VARCHAR(50) NOT NULL
)

EVENT (
    id INTEGER PK, 
    title VARCHAR(100) NOT NULL, 
    description VARCHAR(500), 
    status VARCHAR(50) NOT NULL, 
    live_date TIMESTAMPTZ, 
    image BYTEA
)

GENRE (
    id INTEGER PK, 
    name VARCHAR(50) NOT NULL, 
    active BOOLEAN NOT NULL
)

EVENT_GENRE (
    genre_id INTEGER PK, FK → GENRE(id) NOT NULL
        ON DELETE CASCADE ON UPDATE CASCADE, 
    event_id INTEGER PK, FK → EVENT(id) NOT NULL
        ON DELETE CASCADE ON UPDATE CASCADE
)

PERFORMANCE (
    id INTEGER PK, 
    event_id INTEGER FK → EVENT(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    stage_id INTEGER FK → STAGE(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    date_time TIMESTAMPTZ NOT NULL,
    filled BOOLEAN NOT NULL
)

PRICE_TIER (
    id INTEGER PK, 
    section_id INTEGER FK → SECTION(id)
        ON DELETE RESTRICT ON UPDATE CASCADE,  
    performance_id INTEGER FK → PERFORMANCE(id)
        ON DELETE RESTRICT ON UPDATE CASCADE,  
    stage_id INTEGER FK → STAGE(id)
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    event_id INTEGER FK → EVENT(id)
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    price NUMERIC(10,2) NOT NULL
)

PERFORMANCE_STATUS_LOG (
    id INTEGER PK, 
    performance_id INTEGER FK → PERFORMANCE(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    changed_by INTEGER FK → PERSON(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    previous VARCHAR(50) NOT NULL, 
    new VARCHAR(50) NOT NULL, 
    date_time TIMESTAMPTZ NOT NULL
)

PERFORMANCE_CAST (
    performer_id INTEGER PK, FK → PERFORMER(id) NOT NULL
        ON DELETE CASCADE ON UPDATE CASCADE, 
    performance_id INTEGER PK, FK → PERFORMANCE(id) NOT NULL
        ON DELETE CASCADE ON UPDATE CASCADE
)

SEAT (
    id INTEGER PK, 
    section_id INTEGER FK → SECTION(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    row VARCHAR(5) NOT NULL, 
    seat_number INTEGER NOT NULL
)

PERFORMANCE_SEAT (
    id INTEGER PK, 
    seat_id INTEGER FK → SEAT(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    performance_id INTEGER FK → PERFORMANCE(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    status VARCHAR(25) NOT NULL, 
    held_until TIMESTAMPTZ
)

TICKET (
    id INTEGER PK, 
    performance_seat_id INTEGER FK → PERFORMANCE_SEAT(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    date_time TIMESTAMPTZ NOT NULL, 
    price NUMERIC(10,2) NOT NULL
)

TICKET_SCAN (
    id INTEGER PK, 
    ticket_id INTEGER FK → TICKET(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    date_time TIMESTAMPTZ NOT NULL, 
    success BOOLEAN NOT NULL
)

CART (
    id INTEGER PK, 
    patron_id INTEGER FK → PATRON(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    status VARCHAR(50) NOT NULL
)

CART_ITEM (
    id INTEGER PK, 
    cart_id INTEGER FK → CART(id) NOT NULL
        ON DELETE CASCADE ON UPDATE CASCADE, 
    ticket_id INTEGER FK → TICKET(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE 
)


ORDER (
    id INTEGER PK, 
    cart_id INTEGER FK → CART(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    promo_code_id INTEGER FK → PROMO_CODE(id) 
        ON DELETE SET NULL ON UPDATE CASCADE, 
    date_time TIMESTAMPTZ NOT NULL
)

PROMO_CODE (
    id INTEGER PK, 
    code VARCHAR(50) UNIQUE NOT NULL, 
    d_off NUMERIC(10,2), 
    p_off NUMERIC(5,2), 
    active BOOLEAN NOT NULL, 
    expiry_date TIMESTAMPTZ
)

PAYMENT (
    id INTEGER PK, 
    order_id INTEGER FK → ORDER(id) UNIQUE NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    method VARCHAR(50) NOT NULL, 
    amount NUMERIC(10,2) NOT NULL, 
    date_time TIMESTAMPTZ NOT NULL
)

AUDIT_LOG (
    id INTEGER PK, 
    staff_id INTEGER FK → STAFF(id) NOT NULL
        ON DELETE RESTRICT ON UPDATE CASCADE, 
    table_name VARCHAR(50) NOT NULL, 
    record_id INTEGER NOT NULL, 
    action VARCHAR(50) NOT NULL, 
    old_value BYTEA, 
    new_value BYTEA, 
    changed_at TIMESTAMPTZ NOT NULL, 
    ip_address VARCHAR(45) NOT NULL
)
```

## Functional Dependencies and Normalization

### Tables That Already Satisfy 3NF

**PERSON**
```text
    id → first_name, last_name, email, phone, password
```

**EMAIL_LIST**
```text
    person_id → added
```

**LOGIN_HISTORY**
```text
    id → person_id, ip_address, success
```

**ERROR_LOG**
```text
    id → person_id, end_point, error_message, stack_trace
```

**PASSWORD_RESET_REQUEST**
```text
    id → person_id, time, success, status
```

**ROLE**
```text
    id → role
```

**PERMISSION_CHANGE_LOG**
```text
    id → person_id, changed_by, old_role, new_role, date_time
```

**PATRON**
```text
    id → role_id
```

**STAFF**
```text
    id → role_id
```

**PERFORMER**
```text
    id → role
```

**VENUE**
```text
    id → name, address, city, province, postal_code
```

**STAGE**
```text
    id → venue_id, name
```

**SECTION**
```text
    id → stage_id, name
```

**EVENT**
```text
    id → title, description, status, live_date, image
```

**GENRE**
```text
    id → name, active
```

**PERFORMANCE**
```text
    id → event_id, stage_id, date_time, filled
```

**PRICE_TIER**
```text
    id → section_id, performance_id, stage_id, event_id, price
```
The foreign keys represent alternative pricing scopes, allowing a price tier to apply at the section, performance, stage, or event level.
Therefore, these foreign keys do not represent transitive dependencies and no decomposition is required.


**PERFORMANCE_STATUS_LOG**
```text
    id → performance_id, changed_by, previous, new, date_time
```

**PERFORMANCE_SEAT**
```text
    id → seat_id, performance_id, status, held_until
```

**TICKET**
```text
    id → performance_seat_id, date_time, price
```

**TICKET_SCAN**
```text
    id → ticket_id, date_time, success
```

**CART**
```text
    id → patron_id, status
```

**CART_ITEM**
```text
    id → cart_id, ticket_id
```

**ORDER**
```text
    id → cart_id, promo_code_id, date_time
```

**PROMO_CODE**
```text
    id → code, d_off, p_off, active, expiry_date
```

**PAYMENT**
```text
    id → order_id, method, amount, date_time
```

**AUDIT_LOG**
```text
    id → staff_id, table_name, record_id, action, old_value, new_value, changed_at, ip_address
```

### Tables That Require Decomposition

#### SEAT

**Original table:**
```
    SEAT (
        id PK,
        stage_id FK → STAGE(id),
        section_id FK → SECTION(id),
        row,
        seat_number
    )
```

**Functional dependencies:**
```text
    id → stage_id, section_id, row, seat_number
    section_id → stage_id
```

**1NF**
Table is in 1NF. All attributes contain atomic values only.

**2NF**
Table is in 2NF. There are no partial dependencies.

**3NF**
Table is not in 3NF. There is a transitive dependency:
```text
    id → section_id → stage_id
```
Therefore, `stage_id` is redundant and can be removed from `SEAT`.

**Normalized 3NF table:**
```
    SEAT (
        id PK,
        section_id FK → SECTION(id),
        row,
        seat_number
    )
```

**Functional dependencies:**
```text
    id → section_id, row, seat_number
```
Table is in 3NF. All transitive dependencies have been removed.


## Relationship Mapping

### One-to-Many Relationships

One-to-many relationships were mapped by placing a foreign key in the table on the "many" side, referencing the primary key of the table on the "one" side.

**SEAT and PERFORMANCE_SEAT**

`PERFORMANCE_SEAT` references both `SEAT` and `PERFORMANCE` to represent a physical seat for a specific performance. This allows each seat to have different availability statuses and hold expiration times for different performances.

**CART_ITEM**

`CART_ITEM` contains foreign keys referencing `CART` and `TICKET`. This allows a cart to contain multiple tickets without storing repeating ticket information directly in the `CART` table.

**PRICE_TIER**

`PRICE_TIER` contains foreign keys referencing `EVENT`, `STAGE`, `PERFORMANCE`, and `SECTION`. These foreign keys represent alternative pricing scopes, allowing a price tier to apply at the event, stage, performance, or section level.

### Many-to-Many Relationships

Many-to-many relationships were mapped using junction tables containing foreign keys referencing both related tables. These foreign keys form a composite primary key.

**EVENT and GENRE**

The many-to-many relationship between `EVENT` and `GENRE` was resolved using `EVENT_GENRE`. It contains `event_id` and `genre_id` as foreign keys, which together form a composite primary key. This allows an event to have multiple genres and a genre to belong to multiple events.

**PERFORMER and PERFORMANCE**

The many-to-many relationship between `PERFORMER` and `PERFORMANCE` was resolved using `PERFORMANCE_CAST`. It contains `performer_id` and `performance_id` as foreign keys, which together form a composite primary key. This allows performers to participate in multiple performances and performances to have multiple performers.

### Supertype/Subtype Hierarchy

The `PERSON` supertype was mapped to the `PERFORMER`, `PATRON`, and `STAFF` subtypes, with each subtype using `id` as both its primary key and a foreign key referencing `PERSON(id)`.

`PERSON` contains shared attributes such as names and login credentials, while each subtype stores its own attributes. The hierarchy uses total, overlapping specialization, meaning every person belongs to at least one subtype and can belong to multiple subtypes at the same time.