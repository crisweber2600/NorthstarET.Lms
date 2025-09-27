# 08 — Non-Functional Requirements

- **Performance**: Reads p95 ≤ 300ms; Writes p95 ≤ 800ms; bulk 50k rows ≤ 10 min p95.
- **Availability**: ≥ 99.9% weekly; graceful degradation on broker/DB hiccups.
- **Scalability**: Up to 2k tenants; up to 5M enrollments per tenant.
- **Security**: JWT validation, scope checks, PII minimization; audit on all writes.
- **Versioning**: `/v1` path prefix; deprecation via `Deprecation` & `Sunset` headers.
