# 03 — Domain Model (high level)

**Entities**
- Tenant(District), School(tenantId), Class(schoolId), Student(tenantId), Teacher(tenantId), Enrollment(studentId,classId), ClassTeacher(classId,teacherId)

**Invariants**
- Enrollment requires active Student, active Class, same tenant.
- Deactivated Students cannot receive new Enrollments.
- ClassTeacher unique per (classId, teacherId).

**Indexes (illustrative)**
- IX_School_TenantId_Name; IX_Class_SchoolId_Name; IX_Student_TenantId_FamilyName; IX_Enrollment_ClassId; IX_Enrollment_StudentId.
