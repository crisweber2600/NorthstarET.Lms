# 06 — Security & Tenancy

- **AuthN:** OAuth2 (JWT bearer), mTLS between services (optional).
- **AuthZ:** Role + scope checks at API; per-tenant ownership checks at application layer.
- **Tenancy:** `X-Tenant-Id` required on every request; enforced at DB level (separate DB or schema); included in every event.
- **Headers:** `X-Correlation-Id`, `X-Causation-Id`, `Idempotency-Key` (for POST/PUT where applicable).
- **PII:** No PII in events; field-level encryption for sensitive attributes at rest.
