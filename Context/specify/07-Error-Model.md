# 07 — Error Model

- Use `application/problem+json` with fields: `type`, `title`, `status`, `detail`, `instance`, plus `traceId`, `correlationId`.
- Status codes: 400, 401, 403, 404, 409, 422, 429, 5xx.
- Include `Retry-After` for 429/503 where backoff is recommended.
- Conflicts (e.g., duplicate school name) → 409 with `conflictId` and links to the conflicting resource.
