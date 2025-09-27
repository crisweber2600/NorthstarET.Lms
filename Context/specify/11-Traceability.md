# 11 — Traceability

- BR → API → Event mapping (excerpt)

| Business Requirement | HTTP Endpoint(s) | Event(s) |
|---|---|---|
| CRUD Schools | POST/GET/PATCH/DELETE /v1/schools | SchoolCreated/Updated/Archived |
| CRUD Classes | POST/GET/PATCH/DELETE /v1/classes | ClassCreated/Updated/Archived |
| Enrollments | POST/DELETE /v1/enrollments | EnrollmentAdded/Removed |
| Teacher Assign | POST/DELETE /v1/classes/{classId}/teachers | TeacherAssignedToClass/TeacherUnassignedFromClass |
| Coarse Roster | GET /v1/roster/classes/{classId}/members | (query only) |
| Batch Lookup | POST /v1/lookup/students:batch | (query only) |
