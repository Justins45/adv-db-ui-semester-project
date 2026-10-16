"""
Seed-data generator for the Tradition LTD. ticketing database (DBAD 4000, Phase 2).

All data is made up. The generator is seeded, so running it again produces the
exact same file. Output: AdvDataPhase2SeedData.sql (run AFTER the schema script).

Usage:
    python3 generate_seed_data.py            -> writes AdvDataPhase2SeedData.sql
    python3 generate_seed_data.py out.sql    -> writes to out.sql
"""

import random
import sys
import json
from datetime import datetime, timedelta
from zoneinfo import ZoneInfo

random.seed(4000)
TZ = ZoneInfo("America/Edmonton")
NOW = datetime(2026, 10, 15, 12, 0, tzinfo=TZ)   # "today" for the dataset
OUT = sys.argv[1] if len(sys.argv) > 1 else "AdvDataPhase2SeedData.sql"

# ---------------------------------------------------------------- helpers
def q(v):
    """Render a Python value as a SQL literal."""
    if v is None:
        return "NULL"
    if isinstance(v, bool):
        return "TRUE" if v else "FALSE"
    if isinstance(v, (int, float)):
        return str(v)
    if isinstance(v, datetime):
        return "'" + v.astimezone(TZ).isoformat(sep=" ", timespec="seconds") + "'"
    if isinstance(v, Bytes):
        return "convert_to(" + q(v.text) + ", 'UTF8')"
    return "'" + str(v).replace("'", "''") + "'"


class Bytes:
    """Marker for BYTEA columns: stored as UTF-8 bytes of the given text."""
    def __init__(self, text):
        self.text = text


def money(x):
    return round(x + 1e-9, 2)


def local(y, m, d, h=0, mi=0):
    return datetime(y, m, d, h, mi, tzinfo=TZ)


def rand_between(a, b):
    """Random datetime between a and b."""
    if b <= a:
        return a
    return a + timedelta(seconds=random.randint(0, int((b - a).total_seconds())))


tables = {}   # table -> (columns, rows)


def add(table, cols, row):
    tables.setdefault(table, (cols, []))[1].append(row)


# ---------------------------------------------------------------- ROLE
ROLES = {"ADMIN": 1, "STAFF": 2, "PATRON": 3}
for name, rid in ROLES.items():
    add("ROLE", ["id", "role"], [rid, name])

# ---------------------------------------------------------------- PERSON (400)
FIRST = ["Liam", "Olivia", "Noah", "Emma", "Ethan", "Ava", "Lucas", "Sophia", "Mason", "Isabella",
         "Logan", "Mia", "Benjamin", "Charlotte", "Jacob", "Amelia", "William", "Harper", "James", "Evelyn",
         "Aiden", "Abigail", "Owen", "Emily", "Carter", "Ella", "Jack", "Avery", "Wyatt", "Scarlett",
         "Arjun", "Priya", "Wei", "Mei", "Hiroshi", "Yuki", "Mateo", "Lucia", "Omar", "Layla",
         "Kwame", "Amara", "Dmitri", "Anya", "Rafael", "Camila", "Sanjay", "Anika", "Tariq", "Zara",
         "Connor", "Maeve", "Declan", "Siobhan", "Henrik", "Ingrid", "Tomas", "Elena", "Kai", "Leilani",
         "Nathan", "Grace", "Samuel", "Chloe", "Daniel", "Hannah", "Gabriel", "Nora", "Julian", "Lily"]
LAST = ["Smith", "Brown", "Tremblay", "Martin", "Roy", "Wilson", "MacDonald", "Gagnon", "Johnson", "Taylor",
        "Campbell", "Anderson", "Leblanc", "Lee", "Kelly", "Clark", "White", "Thompson", "Moore", "Young",
        "Patel", "Singh", "Chen", "Wang", "Nguyen", "Kim", "Sato", "Garcia", "Rodriguez", "Hernandez",
        "Okafor", "Mensah", "Ivanova", "Petrov", "Haddad", "Rahman", "Kowalski", "Novak", "Larsen", "Berg",
        "Fraser", "Murphy", "O'Brien", "Sullivan", "Fontaine", "Bouchard", "Cardinal", "Lavallee", "Morin", "Stewart"]
DOMAINS = ["email.com", "mailbox.ca", "inbox.net", "webmail.org", "shawmail.ca", "telus.net"]
AREA = ["403", "587", "825", "780"]

persons = []
used_email, used_phone = set(), set()
for pid in range(1, 401):
    fn, ln = random.choice(FIRST), random.choice(LAST)
    base = f"{fn}.{ln}".lower().replace("'", "")
    email = f"{base}@{random.choice(DOMAINS)}"
    n = 2
    while email in used_email:
        email = f"{base}{n}@{random.choice(DOMAINS)}"
        n += 1
    used_email.add(email)
    phone = None
    while phone is None or phone in used_phone:
        phone = f"+1-{random.choice(AREA)}-{random.randint(200, 999)}-{random.randint(0, 9999):04d}"
    used_phone.add(phone)
    pw = "$2b$12$" + "".join(random.choice("./ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
                             for _ in range(53))
    created = rand_between(local(2024, 1, 1), local(2026, 9, 30))
    persons.append({"id": pid, "first": fn, "last": ln, "created": created})
    add("PERSON", ["id", "first_name", "last_name", "email", "phone", "password"],
        [pid, fn, ln, email, phone, pw])

ALL_PIDS = [p["id"] for p in persons]

# ---------------------------------------------------------------- PATRON (every person, per design)
for pid in ALL_PIDS:
    add("PATRON", ["id", "role_id"], [pid, ROLES["PATRON"]])

# ---------------------------------------------------------------- STAFF (45: 8 admins, 37 staff)
staff_ids = sorted(random.sample(ALL_PIDS, 45))
admin_ids = staff_ids[:8]
regular_staff = staff_ids[8:]
for sid in staff_ids:
    add("STAFF", ["id", "role_id"], [sid, ROLES["ADMIN"] if sid in admin_ids else ROLES["STAFF"]])
non_staff = [p for p in ALL_PIDS if p not in staff_ids]
former_staff = sorted(random.sample(non_staff, 20))   # used to work there, now patrons only

# ---------------------------------------------------------------- PERFORMER (100)
BILLING = ["Lead Actor", "Lead Actress", "Supporting Actor", "Supporting Actress", "Ensemble",
           "Understudy", "Lead Vocalist", "Pianist", "Violinist", "Cellist", "Dancer", "Principal Dancer",
           "Comedian", "Narrator", "Guitarist", "Drummer", "Soprano", "Tenor", "Baritone", None]
performer_ids = sorted(random.sample(ALL_PIDS, 100))
for pid in performer_ids:
    add("PERFORMER", ["id", "role"], [pid, random.choice(BILLING)])

# ---------------------------------------------------------------- VENUE (6) / STAGE (12) / SECTION (36) / SEAT (864)
VENUES = [
    ("Playwright West", "123 Theater Way SW", "Calgary", "Alberta", "T2P 1J9"),
    ("Bow River Playhouse", "48 Riverfront Ave SE", "Calgary", "Alberta", "T2G 4K2"),
    ("Northern Lights Hall", "10220 Jasper Ave NW", "Edmonton", "Alberta", "T5J 3R8"),
    ("Parkland Stage House", "4912 Ross St", "Red Deer", "Alberta", "T4N 1X6"),
    ("Coulee Arts Centre", "305 4 Ave S", "Lethbridge", "Alberta", "T1J 0N7"),
    ("Cascade Mountain Theatre", "211 Bear St", "Banff", "Alberta", "T1L 1A3"),
]
STAGE_NAMES = [["Main Stage", "Studio Stage", "VIP Lounge Stage"],
               ["Main Stage", "Black Box"],
               ["Grand Stage", "Recital Hall", "Studio Stage"],
               ["Main Stage", "Cabaret Room"],
               ["Main Stage"],
               ["Alpine Stage"]]
SECTION_NAMES = ["Orchestra", "Mezzanine", "Balcony"]
ROWS_PER_SECTION, SEATS_PER_ROW = 3, 8          # 24 seats/section, 72 seats/stage

stage_venue, stage_name, stage_seats, section_stage = {}, {}, {}, {}
seat_section = {}
stage_id = section_id = seat_id = 0
for vid, v in enumerate(VENUES, 1):
    add("VENUE", ["id", "name", "address", "city", "province", "postal_code"], [vid, *v])
    for sname in STAGE_NAMES[vid - 1]:
        stage_id += 1
        stage_venue[stage_id], stage_name[stage_id] = vid, sname
        stage_seats[stage_id] = []
        add("STAGE", ["id", "venue_id", "name"], [stage_id, vid, sname])
        for secname in SECTION_NAMES:
            section_id += 1
            section_stage[section_id] = stage_id
            add("SECTION", ["id", "stage_id", "name"], [section_id, stage_id, secname])
            for r in "ABC"[:ROWS_PER_SECTION]:
                for n in range(1, SEATS_PER_ROW + 1):
                    seat_id += 1
                    seat_section[seat_id] = section_id
                    stage_seats[stage_id].append(seat_id)
                    add("SEAT", ["id", "section_id", "row", "seat_number"], [seat_id, section_id, r, n])

# ---------------------------------------------------------------- GENRE (15) / EVENT (100) / EVENT_GENRE
GENRES = ["Musical Theater", "Drama", "Comedy", "Tragedy", "Opera", "Ballet", "Contemporary Dance",
          "Stand-Up Comedy", "Classical Concert", "Jazz", "Folk Music", "Children's Theatre",
          "Improv", "Spoken Word", "Vaudeville"]
INACTIVE_GENRES = {"Vaudeville", "Spoken Word"}
for gid, g in enumerate(GENRES, 1):
    add("GENRE", ["id", "name", "active"], [gid, g, g not in INACTIVE_GENRES])
active_genre_ids = [i for i, g in enumerate(GENRES, 1) if g not in INACTIVE_GENRES]

ADJ = ["Silent", "Crimson", "Midnight", "Last", "Golden", "Broken", "Wandering", "Painted", "Hollow",
       "Electric", "Forgotten", "Velvet", "Northern", "Paper", "Restless", "Secret", "Little", "Burning",
       "Prairie", "Glass"]
NOUN = ["Lantern", "Harbour", "Orchard", "Echo", "Waltz", "Kingdom", "Letters", "Carousel", "Sparrow",
        "Garden", "Chorus", "Mirror", "Lullaby", "Frontier", "Tide", "Masquerade", "Compass", "Overture",
        "Railway", "Cabaret"]
FORMATS = ["The {a} {n}", "{a} {n}", "A {a} {n}", "{n} of the {a}", "The {n} Sessions", "{a} {n}: Live"]

events = []
titles = set()
first_shows = sorted(rand_between(local(2026, 4, 1), local(2027, 2, 28)) for _ in range(100))
draft_slots = set(random.sample(range(70, 100), 10))     # 10 future events still in draft
for eid in range(1, 101):
    t = None
    while t is None or t in titles:
        t = random.choice(FORMATS).format(a=random.choice(ADJ), n=random.choice(NOUN))
        if t.startswith("A ") and t[2] in "AEIOU":
            t = "An " + t[2:]
    titles.add(t)
    first_show = first_shows[eid - 1].replace(hour=0, minute=0, second=0)
    live = (first_show - timedelta(days=random.randint(60, 150))).replace(hour=9, minute=0)
    draft = (eid - 1) in draft_slots or live > NOW
    if (eid - 1) in draft_slots:
        live = None if random.random() < 0.4 else max(live, NOW + timedelta(days=random.randint(5, 40)))
    status = "DRAFT" if draft else "ACTIVE"
    genres = random.sample(active_genre_ids, random.choice([1, 1, 2, 2, 3]))
    desc = None if random.random() < 0.08 else (
        f"{t} is an original {GENRES[genres[0]-1].lower()} production presented by Tradition LTD. "
        f"Running time approximately {random.choice([75, 90, 110, 120, 140])} minutes"
        f"{' with one intermission' if random.random() < 0.6 else ''}.")
    venue = random.choices(range(1, 7), weights=[30, 20, 22, 12, 9, 7])[0]
    events.append({"id": eid, "title": t, "status": status, "live": live, "first": first_show,
                   "venue": venue, "genres": genres})
    add("EVENT", ["id", "title", "description", "status", "live_date", "image"],
        [eid, t, desc, status, live, None])
    for g in genres:
        add("EVENT_GENRE", ["genre_id", "event_id"], [g, eid])

# ---------------------------------------------------------------- PERFORMANCE (~200)
performances = []
booked = set()                 # (stage, datetime) to avoid double-booking a stage
pid_counter = 0
for ev in events:
    stages = [s for s, v in stage_venue.items() if v == ev["venue"]]
    stage = random.choice(stages)
    n_shows = random.choices([1, 2, 2, 3], k=1)[0]
    day = ev["first"]
    shows = 0
    tries = 0
    while shows < n_shows and tries < 30:
        tries += 1
        hour, minute = random.choice([(19, 30), (19, 30), (20, 0), (13, 30)])
        dt = local(day.year, day.month, day.day, hour, minute)
        if (stage, dt) not in booked:
            booked.add((stage, dt))
            pid_counter += 1
            if ev["status"] == "DRAFT":
                pstatus = "DRAFT"
            elif dt < NOW:
                pstatus = "COMPLETED"
            else:
                pstatus = "SCHEDULED"
            performances.append({"id": pid_counter, "event": ev["id"], "stage": stage, "dt": dt,
                                 "status": pstatus})
            shows += 1
        day = day + timedelta(days=random.choice([1, 1, 2, 7]))

# cancel a few (some past, some future)
cancellable = [p for p in performances if p["status"] in ("COMPLETED", "SCHEDULED")]
for p in random.sample(cancellable, 8):
    p["status"] = "CANCELLED"
perf_by_id = {p["id"]: p for p in performances}

# ---------------------------------------------------------------- PRICE_TIER (~170)
tiers = []   # dicts with event, performance, stage, section, price


def add_tier(event=None, performance=None, stage=None, section=None, price=0.0):
    tiers.append({"event": event, "performance": performance, "stage": stage, "section": section,
                  "price": money(price)})


add_tier(price=45.00)                                                     # the single default row
for s in stage_venue:                                                      # every stage
    base = 65.00 if "VIP" in stage_name[s] else random.choice([39.00, 42.50, 45.00, 49.00, 55.00])
    add_tier(stage=s, price=base)
for sec, s in section_stage.items():                                       # every section
    name = SECTION_NAMES[(sec - 1) % 3]
    add_tier(section=sec, price={"Orchestra": 59.00, "Mezzanine": 47.50, "Balcony": 34.00}[name]
             + random.choice([0, 0, 2.5, 5.0]))
special_events = random.sample([e["id"] for e in events], 50)              # special-priced events
for e in special_events:
    add_tier(event=e, price=random.choice([49.99, 54.99, 59.99, 64.99, 74.99, 89.99]))
weekend = [p for p in performances
           if p["dt"].weekday() in (4, 5) and p["dt"].hour >= 19 and p["status"] != "CANCELLED"]
for p in random.sample(weekend, min(50, len(weekend))):                    # Fri/Sat night premium
    add_tier(performance=p["id"], price=random.choice([64.99, 69.99, 74.99, 79.99]))
combo = 0
for e in random.sample(special_events, 20):                                # event + section combos
    ev_stages = {p["stage"] for p in performances if p["event"] == e}
    st = next(iter(ev_stages))
    orch = [sec for sec, s in section_stage.items() if s == st][0]          # orchestra section
    add_tier(event=e, section=orch, price=random.choice([99.99, 109.99, 124.99]))
    combo += 1

for tid, t in enumerate(tiers, 1):
    t["id"] = tid
    add("PRICE_TIER", ["id", "section_id", "performance_id", "stage_id", "event_id", "price"],
        [tid, t["section"], t["performance"], t["stage"], t["event"], t["price"]])


def ticket_price(perf, seat):
    """Technical report rule: the matching tier with the most filled-in columns wins
    (ties broken by the higher price)."""
    ctx = {"event": perf["event"], "performance": perf["id"], "stage": perf["stage"],
           "section": seat_section[seat]}
    best = None
    for t in tiers:
        if all(t[k] is None or t[k] == ctx[k] for k in ctx):
            score = (sum(t[k] is not None for k in ctx), t["price"])
            if best is None or score > best[0]:
                best = (score, t["price"])
    return best[1]

# ---------------------------------------------------------------- PROMO_CODE (40)
promos = []
PROMO_WORDS = ["SAVE", "SPRING", "FALL", "STUDENT", "SENIOR", "MATINEE", "FAMILY", "VIP", "EARLYBIRD",
               "HOLIDAY", "MEMBER", "OPENING", "ENCORE", "YYC", "YEG", "FRIENDS", "GROUP", "WELCOME"]
codes = set()
for cid in range(1, 41):
    c = None
    while c is None or c in codes:
        c = random.choice(PROMO_WORDS) + str(random.choice([5, 10, 15, 20, 25, 26, 27, 50]))
    codes.add(c)
    if random.random() < 0.5:
        d_off, p_off = money(random.choice([5, 10, 15, 20, 25])), None
    else:
        d_off, p_off = None, random.choice([0.10, 0.15, 0.20, 0.25, 0.30])
    expiry = None if random.random() < 0.15 else rand_between(local(2026, 3, 1), local(2027, 6, 30))
    active = (expiry is None or expiry > NOW) and random.random() > 0.1
    start = rand_between(local(2026, 1, 1), local(2026, 9, 1))
    promos.append({"id": cid, "d": d_off, "p": p_off, "exp": expiry, "start": start})
    add("PROMO_CODE", ["id", "code", "d_off", "p_off", "active", "expiry_date"],
        [cid, c, d_off, p_off, active, expiry])

# ---------------------------------------------------------------- PERFORMANCE_SEAT / CART / TICKET / ORDER / PAYMENT
PS_COLS = ["id", "seat_id", "performance_id", "status", "held_until"]
ps_id = ticket_id = cart_id = cart_item_id = order_id = payment_id = 0
perf_seat_ids = {}          # (perf, seat) -> performance_seat id
sold_tickets = []           # (ticket_id, perf, ps_id, purchase time, patron)
METHODS = ["VISA", "MASTERCARD", "CREDIT_CARD", "DEBIT_CARD", "PAYPAL", "APPLE_PAY", "GOOGLE_PAY"]
patron_pool = ALL_PIDS
active_cart_patrons = set()

for p in performances:
    seats = stage_seats[p["stage"]]
    status_map = {s: "AVAILABLE" for s in seats}
    held = {}
    if p["status"] in ("CANCELLED", "DRAFT"):
        if p["status"] == "CANCELLED":
            status_map = {s: "UNAVAILABLE" for s in seats}
    else:
        # a couple of seats out of service on some performances
        for s in random.sample(seats, random.choice([0, 0, 0, 1, 2])):
            status_map[s] = "UNAVAILABLE"
        sellable = [s for s in seats if status_map[s] == "AVAILABLE"]
        ev = events[p["event"] - 1]
        demand = random.choice([0.15, 0.25, 0.35, 0.45, 0.6, 0.75])
        sellout = random.random() < 0.08                  # sell-out
        if sellout:
            demand = 1.0
        if p["status"] == "SCHEDULED" and not sellout:
            demand *= 0.6                                 # future shows are still selling
        to_sell = int(len(sellable) * demand)
        # sell in contiguous groups of 1-6 seats
        order_seats, i = [], 0
        start = random.randint(0, max(0, len(sellable) - to_sell))
        chosen = sellable[start:start + to_sell] if demand < 1.0 else sellable
        while i < len(chosen):
            g = random.choice([1, 2, 2, 2, 3, 4, 4, 5, 6])
            order_seats.append(chosen[i:i + g])
            i += g
        sale_open = ev["live"] or (p["dt"] - timedelta(days=45))
        sale_close = min(p["dt"] - timedelta(hours=1), NOW - timedelta(hours=2))
        for group in order_seats:
            patron = random.choice(patron_pool)
            ordered_at = rand_between(sale_open, sale_close)
            cart_id += 1
            add("CART", ["id", "patron_id", "status"], [cart_id, patron, "CHECKED_OUT"])
            total = 0.0
            for s in group:
                status_map[s] = "SOLD"
            group_tickets = []
            for s in group:
                group_tickets.append((s, ticket_price(p, s)))
                total += group_tickets[-1][1]
            # promo code on ~15% of orders, only if valid at that time
            promo = None
            if random.random() < 0.15:
                valid = [pr for pr in promos if pr["start"] <= ordered_at and (pr["exp"] is None or pr["exp"] > ordered_at)]
                if valid:
                    promo = random.choice(valid)
            amount = total
            if promo:
                amount = total * (1 - promo["p"]) if promo["p"] is not None else total - promo["d"]
            amount = money(max(0.0, amount))
            order_id += 1
            add("ORDER", ["id", "cart_id", "promo_code_id", "date_time"],
                [order_id, cart_id, promo["id"] if promo else None, ordered_at])
            payment_id += 1
            add("PAYMENT", ["id", "order_id", "method", "amount", "date_time"],
                [payment_id, order_id, random.choice(METHODS), amount, ordered_at + timedelta(seconds=random.randint(20, 240))])
            for s, price in group_tickets:
                held[s] = ("SOLD", cart_id, ordered_at - timedelta(minutes=random.randint(2, 15)), price, patron)

        # live, active carts holding seats for upcoming shows
        if p["status"] == "SCHEDULED" and random.random() < 0.2:
            free = [s for s in seats if status_map[s] == "AVAILABLE"]
            if free:
                patron = random.choice([x for x in patron_pool if x not in active_cart_patrons])
                active_cart_patrons.add(patron)
                cart_id += 1
                add("CART", ["id", "patron_id", "status"], [cart_id, patron, "ACTIVE"])
                k = min(len(free), random.choice([1, 2, 2, 4]))
                st = random.randint(0, len(free) - k)
                for s in free[st:st + k]:
                    status_map[s] = "HELD"
                    held[s] = ("HELD", cart_id, NOW - timedelta(minutes=random.randint(1, 9)), ticket_price(p, s), patron)

    for s in seats:
        ps_id += 1
        perf_seat_ids[(p["id"], s)] = ps_id
        held_until = None
        if status_map[s] == "HELD":
            held_until = held[s][2] + timedelta(minutes=15)
        add("PERFORMANCE_SEAT", PS_COLS, [ps_id, s, p["id"], status_map[s], held_until])
        if s in held:
            kind, cid, created, price, patron = held[s]
            ticket_id += 1
            add("TICKET", ["id", "performance_seat_id", "date_time", "price"], [ticket_id, ps_id, created, price])
            cart_item_id += 1
            add("CART_ITEM", ["id", "cart_id", "ticket_id"], [cart_item_id, cid, ticket_id])
            if kind == "SOLD":
                sold_tickets.append((ticket_id, p, ps_id, created, patron))
    # filled = nothing left to buy (out-of-service seats don't count)
    p["filled"] = (p["status"] not in ("CANCELLED", "DRAFT")
                   and all(v in ("SOLD", "UNAVAILABLE") for v in status_map.values()))

# abandoned carts (no items left, tickets were released)
for _ in range(35):
    cart_id += 1
    add("CART", ["id", "patron_id", "status"], [cart_id, random.choice(patron_pool), "ABANDONED"])

# write performances now that "filled" is known
for p in performances:
    add("PERFORMANCE", ["id", "event_id", "stage_id", "date_time", "status", "filled"],
        [p["id"], p["event"], p["stage"], p["dt"], p["status"], p["filled"]])

# ---------------------------------------------------------------- PERFORMANCE_CAST
for p in performances:
    for perf in random.sample(performer_ids, random.randint(2, 6)):
        add("PERFORMANCE_CAST", ["performer_id", "performance_id"], [perf, p["id"]])

# ---------------------------------------------------------------- TICKET_SCAN (completed shows)
scan_id = 0
for tid, p, _, _, _ in sold_tickets:
    if p["status"] != "COMPLETED":
        continue
    if random.random() < 0.08:            # no-show
        continue
    when = p["dt"] - timedelta(minutes=random.randint(5, 45))
    if random.random() < 0.04:            # someone tried a screenshot / re-entry first
        scan_id += 1
        add("TICKET_SCAN", ["id", "ticket_id", "date_time", "success"], [scan_id, tid, when - timedelta(minutes=3), False])
    scan_id += 1
    add("TICKET_SCAN", ["id", "ticket_id", "date_time", "success"], [scan_id, tid, when, True])
    if random.random() < 0.03:            # re-scan after entry is refused (re-entry handled by theatre)
        scan_id += 1
        add("TICKET_SCAN", ["id", "ticket_id", "date_time", "success"],
            [scan_id, tid, p["dt"] + timedelta(minutes=random.randint(40, 70)), False])

# ---------------------------------------------------------------- EMAIL_LIST (~180)
for pid in sorted(random.sample(ALL_PIDS, 180)):
    add("EMAIL_LIST", ["person_id", "added"], [pid, rand_between(persons[pid - 1]["created"], NOW)])

# ---------------------------------------------------------------- LOGIN_HISTORY (1200)
for lid in range(1, 1201):
    pid = random.choice(staff_ids) if random.random() < 0.3 else random.choice(ALL_PIDS)
    ip = random.choice([f"192.168.{random.randint(0, 5)}.{random.randint(2, 254)}",
                        f"10.0.{random.randint(0, 20)}.{random.randint(2, 254)}",
                        f"{random.randint(24, 207)}.{random.randint(0, 255)}.{random.randint(0, 255)}.{random.randint(1, 254)}"])
    add("LOGIN_HISTORY", ["id", "person_id", "ip_address", "success", "login_timestamp"],
        [lid, pid, ip, random.random() > 0.12, rand_between(local(2026, 1, 1), NOW)])

# ---------------------------------------------------------------- ERROR_LOG (150)
ERRORS = [("/api/checkout", "Payment gateway timeout"), ("/api/checkout", "Database timeout"),
          ("/api/cart/add", "Seat no longer available"), ("/api/cart/add", "Hold expired before checkout"),
          ("/api/events", "Image upload exceeded size limit"), ("/api/login", "Too many failed attempts"),
          ("/api/password-reset", "Reset token expired"), ("/api/tickets/scan", "Ticket not valid for this performance"),
          ("/api/promo/apply", "Promo code expired"), ("/admin/performances", "Stage already booked at this time"),
          ("/api/seats", "Seat map failed to load"), ("/admin/reports", "Query exceeded time limit")]
for eid in range(1, 151):
    ep, msg = random.choice(ERRORS)
    trace = None
    if random.random() < 0.6:
        trace = Bytes(f"Error: {msg}\n    at handler ({ep.strip('/').replace('/', '.')}.js:{random.randint(10, 400)})\n"
                      f"    at Router.dispatch (router.js:{random.randint(50, 200)})\n    at processTicksAndRejections (node:internal)")
    add("ERROR_LOG", ["id", "person_id", "end_point", "error_message", "stack_trace", "created_at"],
        [eid, None if random.random() < 0.3 else random.choice(ALL_PIDS), ep, msg, trace,
         rand_between(local(2026, 1, 1), NOW)])

# ---------------------------------------------------------------- PASSWORD_RESET_REQUEST (150)
for rid in range(1, 151):
    when = rand_between(local(2026, 1, 1), NOW)
    if when > NOW - timedelta(minutes=30):
        status = "PENDING"
    else:
        status = random.choices(["COMPLETED", "EXPIRED", "FAILED", "PENDING"], weights=[70, 18, 7, 0])[0]
    add("PASSWORD_RESET_REQUEST", ["id", "person_id", "time", "success", "status"],
        [rid, random.choice(ALL_PIDS), when, status == "COMPLETED", status])

# ---------------------------------------------------------------- PERMISSION_CHANGE_LOG (~100)
pcl = []
founders = admin_ids[:2]
for sid in staff_ids + former_staff:
    hired = rand_between(local(2024, 3, 1), local(2026, 6, 1))
    by = founders[1] if sid == founders[0] else founders[0]
    pcl.append([sid, by, ROLES["PATRON"], ROLES["STAFF"], hired])
    if sid in admin_ids:
        pcl.append([sid, by, ROLES["STAFF"], ROLES["ADMIN"], hired + timedelta(days=random.randint(30, 200))])
    if sid in former_staff:
        pcl.append([sid, random.choice(admin_ids), ROLES["STAFF"], ROLES["PATRON"],
                    hired + timedelta(days=random.randint(60, 400))])
pcl.sort(key=lambda r: r[4])
for i, r in enumerate(pcl, 1):
    add("PERMISSION_CHANGE_LOG", ["id", "person_id", "changed_by", "old_role", "new_role", "date_time"], [i, *r])

# ---------------------------------------------------------------- PERFORMANCE_STATUS_LOG
psl = []
for p in performances:
    if p["status"] == "DRAFT":
        continue
    ev = events[p["event"] - 1]
    published = ev["live"] or (p["dt"] - timedelta(days=40))
    psl.append([p["id"], random.choice(staff_ids), "DRAFT", "SCHEDULED", published - timedelta(days=random.randint(1, 10))])
    if p["status"] == "COMPLETED":
        psl.append([p["id"], random.choice(staff_ids), "SCHEDULED", "COMPLETED", p["dt"] + timedelta(hours=3)])
    elif p["status"] == "CANCELLED":
        psl.append([p["id"], random.choice(admin_ids), "SCHEDULED", "CANCELLED",
                    rand_between(published, min(p["dt"], NOW) - timedelta(hours=1))])
psl.sort(key=lambda r: r[4])
for i, r in enumerate(psl, 1):
    add("PERFORMANCE_STATUS_LOG", ["id", "performance_id", "changed_by", "previous", "new", "date_time"], [i, *r])

# ---------------------------------------------------------------- AUDIT_LOG (300)
for aid in range(1, 301):
    table = random.choice(["EVENT", "PERFORMANCE", "PRICE_TIER", "PROMO_CODE", "VENUE", "STAGE"])
    action = random.choices(["UPDATE", "INSERT", "DELETE"], weights=[65, 30, 5])[0]
    if table == "EVENT":
        ev = random.choice(events); rec = ev["id"]
        old = {"title": ev["title"], "status": "DRAFT"}; new = {"title": ev["title"], "status": ev["status"]}
    elif table == "PERFORMANCE":
        p = random.choice(performances); rec = p["id"]
        old = {"status": "SCHEDULED"}; new = {"status": p["status"]}
    elif table == "PRICE_TIER":
        t = random.choice(tiers); rec = t["id"]
        old = {"price": money(t["price"] - 5)}; new = {"price": t["price"]}
    elif table == "PROMO_CODE":
        rec = random.randint(1, 40); old = {"active": True}; new = {"active": False}
    elif table == "VENUE":
        rec = random.randint(1, 6); old = {"postal_code": "T0A 0A0"}; new = {"postal_code": VENUES[rec - 1][4]}
    else:
        rec = random.randint(1, len(stage_venue)); old = {"name": "Stage " + str(rec)}; new = {"name": stage_name[rec]}
    if action == "INSERT":
        old = None
    if action == "DELETE":
        new = None
    add("AUDIT_LOG", ["id", "staff_id", "table_name", "record_id", "action", "old_value", "new_value",
                      "changed_at", "ip_address"],
        [aid, random.choice(staff_ids), table, rec, action,
         Bytes(json.dumps(old)) if old else None, Bytes(json.dumps(new)) if new else None,
         rand_between(local(2026, 1, 1), NOW), f"10.0.{random.randint(0, 3)}.{random.randint(2, 254)}"])

# ---------------------------------------------------------------- write file
ORDER = ["ROLE", "PERSON", "VENUE", "EVENT", "GENRE", "PROMO_CODE", "EMAIL_LIST", "LOGIN_HISTORY",
         "ERROR_LOG", "PASSWORD_RESET_REQUEST", "PATRON", "STAFF", "PERFORMER", "STAGE", "EVENT_GENRE",
         "PERMISSION_CHANGE_LOG", "AUDIT_LOG", "CART", "SECTION", "PERFORMANCE", "SEAT",
         "PERFORMANCE_STATUS_LOG", "PERFORMANCE_CAST", "PRICE_TIER", "PERFORMANCE_SEAT", "TICKET",
         "TICKET_SCAN", "CART_ITEM", "ORDER", "PAYMENT"]
assert set(ORDER) == set(tables), set(ORDER) ^ set(tables)
SERIAL_TABLES = [t for t in ORDER if t not in ("PATRON", "STAFF", "PERFORMER", "EMAIL_LIST",
                                                "EVENT_GENRE", "PERFORMANCE_CAST")]


def ident(t):
    return '"ORDER"' if t == "ORDER" else t


with open(OUT, "w", encoding="utf-8") as f:
    f.write("-- Tradition LTD. ticketing database: sample data (all made up)\n")
    f.write("-- Generated by generate_seed_data.py (random seed 4000). Run AFTER AdvDataPhase2Schema.sql.\n")
    f.write(f"-- Dataset 'today' is {NOW.date()}: earlier performances are COMPLETED, later ones SCHEDULED.\n\n")
    f.write("BEGIN;\n\n")
    for t in ORDER:
        cols, rows = tables[t]
        f.write(f"-- {t}: {len(rows)} rows\n")
        for i in range(0, len(rows), 500):
            chunk = rows[i:i + 500]
            f.write(f"INSERT INTO {ident(t)} ({', '.join(cols)}) VALUES\n")
            f.write(",\n".join("    (" + ", ".join(q(v) for v in r) + ")" for r in chunk))
            f.write(";\n")
        f.write("\n")
    f.write("-- Explicit ids were inserted, so move each SERIAL sequence past the highest id\n")
    for t in SERIAL_TABLES:
        f.write(f"SELECT setval(pg_get_serial_sequence('{ident(t)}', 'id'), (SELECT MAX(id) FROM {ident(t)}));\n")
    f.write("\nCOMMIT;\n")

print(f"Wrote {OUT}")
for t in ORDER:
    print(f"  {t:<24}{len(tables[t][1]):>7}")
