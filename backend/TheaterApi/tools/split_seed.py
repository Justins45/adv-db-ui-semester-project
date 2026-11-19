#!/usr/bin/env python3
"""Split the PostgreSQL seed's LOGIN_HISTORY rows into Firestore JSON.

The source SQL is never modified. All other SQL, including its sequence resets,
is copied to postgres.sql.
"""
import argparse, csv, io, json, re
from datetime import datetime, timezone
from pathlib import Path

INSERT = re.compile(r"INSERT\s+INTO\s+LOGIN_HISTORY\s*\([^;]*?\)\s*VALUES\s*(.*?);", re.I | re.S)
LOGIN_SETVAL = re.compile(r"\n?\s*SELECT\s+setval\([^;]*LOGIN_HISTORY[^;]*;", re.I)
TUPLE = re.compile(r"\(([^()]*)\)", re.S)

def parse_row(text):
    values = next(csv.reader(io.StringIO(text), skipinitialspace=True), [])
    if len(values) != 5:
        raise ValueError(f"expected 5 values, got {len(values)}")
    def unquote(v):
        v = v.strip()
        if v.startswith("'") and v.endswith("'"):
            return v[1:-1].replace("''", "'")
        return v
    timestamp = unquote(values[4])
    timestamp = datetime.fromisoformat(timestamp).astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    return {"id": int(values[0]), "person_id": int(values[1]),
            "ip_address": unquote(values[2]), "success": values[3].strip().upper() == "TRUE",
            "timestamp": timestamp}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    source = args.source.read_text()
    rows = []
    for match in INSERT.finditer(source):
        rows.extend(parse_row(row) for row in TUPLE.findall(match.group(1)))
    postgres = INSERT.sub("", source)
    postgres = LOGIN_SETVAL.sub("\n", postgres)
    postgres = re.sub(r"(?im)^\s*--\s*LOGIN_HISTORY:.*\n", "", postgres)
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / "postgres.sql").write_text(postgres)
    (args.output / "login_history.json").write_text(json.dumps({"login_history": rows}, indent=2) + "\n")
    print(f"wrote {len(rows)} login_history rows")

if __name__ == "__main__":
    main()
