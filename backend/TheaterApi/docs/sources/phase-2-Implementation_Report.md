# Implementation Report – Tradition LTD.

This report summarizes t

## Environment Setup

### **PostgreSQL Setup**

- **PostgreSQL Version:** PostgreSQL 18.6
- **Client Tools:** pgAdmin 4, VS Code with Microsoft PostgreSQL extension

### **Firebase Project Setup**

- **NoSQL Database:** Cloud Firestore for storing the system’s logs
- **Backend Integration:** ASP.NET Core using the `Google.Cloud.Firestore` NuGet package

## PostgreSQL Implementation

### 1. Relational Schema

We implemented our relational database using PostgreSQL based on the relational schema we created in Phase 1, using the same tables, data types, primary and foreign keys, and `NOT NULL` and `UNIQUE` constraints. Our Phase 2 schema added further `CHECK` constraints to validate data values, and `DEFAULT` values to automatically fall back to. We also created indexes for efficient data retrieval.

### 2. Sample Data

We then generated sample data to populate our database. We used Claude (Anthropic's AI assistant) to help write a Python script, `generate_seed_data.py`, that generates realistic, made-up data for every table and writes it to `AdvDataPhase2SeedData.sql`, which is run after the schema script.

The script inserts tables in dependency order so that foreign keys are always valid, and follows the business rules in our schema, such as performances before the dataset's "today" being `COMPLETED` and later ones `SCHEDULED`, and tickets only existing for held or sold seats. It uses a fixed random seed, so running it again produces exactly the same data, and it resets each `SERIAL` sequence after inserting explicit IDs so new rows don't conflict. The resulting dataset includes 400 people, 6 venues, 100 events, 194 performances, almost 14,000 performance seats, and about 4,700 tickets, giving our queries enough data to produce meaningful results.

### 3. Query Portfolio

Finally, we developed a portfolio of 10 SQL queries to show different database operations for retrieving useful information. These queries include complex joins, nested queries, aggregations, views, triggers, stored procedures, and transactions. For example:

- **Query 1:** Creates a view that breaks down seat availability, ticket revenue, and percentage of seats sold for each performance.
  - **Business Insights:** Helps staff identify performances with low sales, and monitor overall sales performance. 
  - **Covers:** views, complex joins, aggregations, grouping
- **Query 2:** Uses `EXPLAIN ANALYZE` to compare the query plan for looking up a cart's items before and after adding an index on `cart_item.cart_id`.
  - **Business Insights:** Shows how indexing foreign keys speeds up common lookups, keeping the checkout experience fast as the number of carts grows.
  - **Covers:** indexes, query performance analysis
- **Query 3:** Creates a stored procedure that releases seats whose hold has expired, removing their cart items and tickets, putting the seats back on sale, and marking empty carts as abandoned.
  - **Business Insights:** Returns unclaimed seats to the pool so they can be sold to other patrons, preventing lost sales from abandoned checkouts.
  - **Covers:** stored procedures, multi-table deletes, updates, nested queries
- **Query 4:** Creates a trigger that runs before a ticket is inserted, rejecting the booking if the seat doesn't exist or isn't available, and otherwise placing a 10-minute hold on the seat. It is tested with transactions for both a successful and a failed booking.
  - **Business Insights:** Prevents double-booking by locking the seat row so two patrons can't buy the same seat at the same time.
  - **Covers:** functions, triggers, row locking, transactions, rollbacks
- **Query 5:** Creates a trigger that keeps each performance's `filled` flag in sync whenever its seats' statuses change. It is tested in a transaction that sells out a performance and then refunds a seat.
  - **Business Insights:** Ensures staff and patrons always see accurate sold-out information, without relying on the application to update it after every sale, release, or cancellation.
  - **Covers:** functions, triggers, nested queries, transactions, rollbacks
- **Query 7:** Uses a nested query to find application endpoints with an above-average number of errors.
  - **Business Insights:** Helps developers determine problematic endpoints and prioritize troubleshooting.
  - **Covers:** nested queries, aggregations, grouping

## NoSQL Implementation (Cloud Firestore)

### 1. Data Selection

We chose to store our ticketing system's logs in Cloud Firestore because our system has four types of logs that record different information. A NoSQL database allows for flexible document structure so each type can have different fields and nested data. PostgreSQL remains the source of truth for core ticketing data such as patrons, tickets, and performances, while Firestore manages the system logs.

We specifically chose Firestore because

### 2. Sample Data

Our Firestore data is organized into four collections which we populated with generated sample log documents. We implemented a seeding script to load the sample log documents into their corresponding Firestore collections.

### 3. Backend Integration

We connected Cloud Firestore to our ASP.NET Core backend using the `Google.Cloud.Firestore` NuGet package so that our application can access and manage the log documents stored in Firestore. 

### 4. CRUD Operations and Queries

Finally, we implemented CRUD operations to manage the log documents, as well as 2 queries using the Firestore SDK:

- **Query 1:** Uses aggregate counts to determine the number of audit logs for each action type (`INSERT`, `UPDATE`, and `DELETE`).
- **Query 2:** Retrieves recent audit logs for a set number of days.

## Object-Oriented and `JSONB` Mapping

### 1. Entity Selection and UML Class Diagram

We chose to represent our `VENUE` entity as an object-oriented data structure because it has a natural hierarchy of stages, sections, and seats that can be represented with nested objects. We created a UML class diagram to show the attributes, composition, and relationships between the `VENUE`, `STAGE`, `SECTION`, and `SEAT` entities.

### 2. `JSONB` Mapping

We then mapped the `VENUE` entity to PostgreSQL `JSONB` by leaving its standard attributes as relational columns, and using a new `JSONB` column to store the nested stage, section, and seat information, adding additional information for certain sections or seats, such as accessibility attributes.

Different theatre venues may have different accessibility information, so this approach gives us the flexibility to store optional accessibility attributes for certain sections or individual seats, without adding new columns to our relational tables. However, we are keeping the `VENUE`, `STAGE`, `SECTION`, and `SEAT` entities fully relational in our main database to maintain their relationships and data integrity, since seats are referenced by transactional data such as ticket orders.

### 3. Query

Finally, we created a query that reads inside the `JSONB` arrays and objects to help staff compare accessible seating capacity across different venue stages.

## Implementation Challenges

Blah
