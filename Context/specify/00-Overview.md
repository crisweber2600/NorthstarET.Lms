# LMS — Spec-Kit Outputs (/specify)

**Date:** 2025-09-26

This folder contains the specification artifacts for the **LMS service** prepared using the Spec‑Kit method (Gather → Summarize → Validate → Commit/Reconcile). Artifacts are structured and versioned for CI consumption.

**Contents**
- `01-Business-Requirements.md`
- `02-User-Journeys.md`
- `03-Domain-Model.md`
- `04-HTTP-API-v1.openapi.yml`
- `05-Event-Schemas/` (JSON Schemas, draft 2020-12)
- `06-Security-and-Tenancy.md`
- `07-Error-Model.md`
- `08-Non-Functional-Requirements.md`
- `09-Observability.md`
- `10-Glossary.md`
- `11-Traceability.md`

**Conventions**
- OpenAPI: 3.1.0 with global security and standardized headers.
- Events: topic namespace `lms.roster.v1.*`, envelope + payload per schema.
- All artifacts enforce **tenantId**, **correlationId**, idempotency and **problem+json**.
