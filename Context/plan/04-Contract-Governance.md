# 04 — Contract Governance (CI Gates)

- **OpenAPI Lint** (spectral) — enforce headers, security, pagination.
- **Breaking Change Check** — compare against previous tag; fail on breaking diffs.
- **Event Schema Validation** — jsonschema; prohibit PII fields in events.
- **CDC** — Pact provider verification for consumers (e.g., Assessment).
- **SDK Generation** — produce/publish `NorthStar.Lms.Client` on tag; consumers pinned by SemVer.
