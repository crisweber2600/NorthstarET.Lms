# 02 — User Journeys (UI-agnostic)

- **Provision Tenant**: Admin posts metadata → tenant active → `TenantProvisioned` event.
- **Create School**: District Admin creates school → `SchoolCreated` event; duplicates → 409.
- **Create Class & Assign Teacher**: Create class → `ClassCreated`; assign teacher → `TeacherAssignedToClass`.
- **Enroll Students**: Bulk upload → idempotent upserts; per-row results; `StudentCreated`/`EnrollmentAdded` events.
- **View Class Roster**: Teacher fetches members in one call; permission enforced.
- **Deactivate Student**: Registrar deactivates → `StudentDeactivated`; new enrollments blocked.

Alternates cover validation errors, permission denials (403), conflict (409), and partial successes in bulk flows.
