# 05 — Risk Register

- **Sync creep** → enforce “1 upstream sync call max” in reviews; prefer events.
- **Contract drift** → CI gates; CDC required before merge.
- **Over-rich events** → keys only; detail via queries.
- **PII leaks** → schema checks; static scans.
- **Bulk overload** → chunking; backpressure; quotas.
