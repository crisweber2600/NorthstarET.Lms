# API Contracts: Student Management

**Feature**: 001-foundational-lms-with  
**Domain**: Student Management  
**Base URL**: `/api/v1/students`
**Tenant Context**: All operations scoped to authenticated user's district

## Authentication & Authorization
- DistrictAdmin: Full CRUD access
- SchoolUser: Access limited to assigned schools  
- Staff: Read access to assigned classes only

## Student CRUD Operations

### Create Student
```http
POST /api/v1/students
Authorization: Bearer {token}
Content-Type: application/json

{
  "studentNumber": "STU-2024-001234",
  "firstName": "Maria",
  "lastName": "Garcia", 
  "dateOfBirth": "2010-06-15",
  "enrollmentDate": "2024-08-15",
  "programs": {
    "isSpecialEducation": false,
    "isGifted": true,
    "isEnglishLanguageLearner": true,
    "accommodationTags": ["extended-time", "large-print"]
  },
  "guardians": [
    {
      "firstName": "Carmen",
      "lastName": "Garcia",
      "email": "carmen.garcia@email.com",
      "phone": "+1-510-555-0123",
      "relationshipType": "Parent",
      "isPrimary": true,
      "canPickup": true
    }
  ]
}
```

**Response (201 Created)**:
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "studentNumber": "STU-2024-001234",
  "firstName": "Maria",
  "lastName": "Garcia",
  "dateOfBirth": "2010-06-15",
  "status": "Active",
  "enrollmentDate": "2024-08-15",
  "currentGradeLevel": "Grade6",
  "programs": {
    "isSpecialEducation": false,
    "isGifted": true, 
    "isEnglishLanguageLearner": true,
    "accommodationTags": ["extended-time", "large-print"]
  },
  "createdDate": "2024-12-19T10:00:00Z"
}
```

### Get Student
```http
GET /api/v1/students/{userId}
Authorization: Bearer {token}
```

**Response includes full academic history**:
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "studentNumber": "STU-2024-001234",
  "firstName": "Maria",
  "lastName": "Garcia",
  "dateOfBirth": "2010-06-15",
  "status": "Active",
  "enrollmentDate": "2024-08-15",
  "currentGradeLevel": "Grade6",
  "programs": {
    "isSpecialEducation": false,
    "isGifted": true,
    "isEnglishLanguageLearner": true,
    "accommodationTags": ["extended-time", "large-print"]
  },
  "currentEnrollments": [
    {
      "classId": "class-math-6a",
      "className": "Mathematics 6A",
      "schoolName": "Lincoln Elementary",
      "gradeLevel": "Grade6",
      "enrollmentDate": "2024-08-15"
    }
  ],
  "enrollmentHistory": [
    {
      "schoolYear": "2023-2024", 
      "gradeLevel": "Grade5",
      "schoolName": "Lincoln Elementary",
      "status": "Completed"
    }
  ],
  "guardians": [
    {
      "guardianId": "guardian-123",
      "firstName": "Carmen",
      "lastName": "Garcia",
      "relationshipType": "Parent",
      "isPrimary": true,
      "canPickup": true,
      "effectiveDate": "2024-08-15"
    }
  ],
  "identityMapping": {
    "externalId": "entra-external-id-456",
    "issuer": "https://login.microsoftonline.com/tenant-id",
    "mappedDate": "2024-08-15T09:30:00Z"
  }
}
```

### List Students
```http
GET /api/v1/students?page=1&size=50&gradeLevel=Grade6&status=Active&schoolId=school-123
Authorization: Bearer {token}
```

**Query Parameters**:
- `page`, `size`: Pagination
- `gradeLevel`: Filter by current grade level
- `status`: Active, Transferred, Graduated, Withdrawn
- `schoolId`: Filter by school (if user has access)
- `classId`: Filter by class (if user has access)
- `hasProgram`: Filter by program flags (specialEducation, gifted, ell)
- `search`: Full-text search on name or student number

### Update Student
```http
PUT /api/v1/students/{userId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "firstName": "Maria Isabel",
  "programs": {
    "isSpecialEducation": true,
    "isGifted": true,
    "isEnglishLanguageLearner": false,
    "accommodationTags": ["extended-time", "large-print", "quiet-space"]
  }
}
```

## Enrollment Management

### Enroll Student in Class
```http
POST /api/v1/students/{userId}/enrollments
Authorization: Bearer {token}
Content-Type: application/json

{
  "classId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "gradeLevel": "Grade6", 
  "enrollmentDate": "2024-08-15"
}
```

### Withdraw Student from Class
```http
DELETE /api/v1/students/{userId}/enrollments/{enrollmentId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "withdrawalDate": "2024-12-15",
  "withdrawalReason": "Transferred to different district"
}
```

### Transfer Student Between Schools
```http
POST /api/v1/students/{userId}/transfer
Authorization: Bearer {token}
Content-Type: application/json

{
  "fromSchoolId": "school-123",
  "toSchoolId": "school-456", 
  "effectiveDate": "2024-01-15",
  "reason": "Family relocation",
  "maintainGradeLevel": true,
  "transferClasses": [
    {
      "fromClassId": "class-math-6a",
      "toClassId": "class-math-6b"
    }
  ]
}
```

## Grade Progression & Rollover

### Individual Grade Promotion
```http
POST /api/v1/students/{userId}/promote
Authorization: Bearer {token}
Content-Type: application/json

{
  "fromSchoolYear": "2023-2024",
  "toSchoolYear": "2024-2025",
  "fromGradeLevel": "Grade5",
  "toGradeLevel": "Grade6",
  "promotionDate": "2024-06-15"
}
```

### Bulk Grade Rollover (Preview)
```http
POST /api/v1/students/bulk-rollover/preview
Authorization: Bearer {token}
Content-Type: application/json

{
  "fromSchoolYear": "2023-2024",
  "toSchoolYear": "2024-2025", 
  "gradeTransitions": [
    { "from": "Grade5", "to": "Grade6" },
    { "from": "Grade6", "to": "Grade7" }
  ],
  "schoolFilters": ["school-123", "school-456"],
  "excludeWithdrawn": true
}
```

**Response (200 OK)**:
```json
{
  "previewId": "rollover-preview-789",
  "summary": {
    "totalStudents": 1250,
    "eligibleStudents": 1205,
    "excludedStudents": 45,
    "gradeTransitions": {
      "Grade5_to_Grade6": 623,
      "Grade6_to_Grade7": 582
    }
  },
  "exclusions": [
    {
      "studentId": "student-123",
      "reason": "Withdrawn status",
      "details": "Student withdrew 2024-05-15"
    }
  ],
  "validUntil": "2024-12-19T11:00:00Z"
}
```

### Execute Bulk Rollover
```http
POST /api/v1/students/bulk-rollover/execute
Authorization: Bearer {token}
Content-Type: application/json

{
  "previewId": "rollover-preview-789",
  "confirmationToken": "CONFIRM-ROLLOVER-789"
}
```

## Guardian Management

### Add Guardian Relationship
```http
POST /api/v1/students/{userId}/guardians
Authorization: Bearer {token}
Content-Type: application/json

{
  "guardianId": "existing-guardian-123",
  "relationshipType": "StepParent",
  "isPrimary": false,
  "canPickup": true,
  "effectiveDate": "2024-12-01"
}
```

### Update Guardian Relationship
```http
PUT /api/v1/students/{userId}/guardians/{relationshipId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "canPickup": false,
  "endDate": "2024-12-31",
  "reason": "Custody change"
}
```

## Bulk Operations

### Bulk Student Import
```http
POST /api/v1/students/bulk-import
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: students.csv
schoolId: school-123
gradeLevel: Grade6
enrollmentDate: 2024-08-15
errorHandling: "best-effort"
```

**CSV Format**:
```csv
studentNumber,firstName,lastName,dateOfBirth,isSpecialEducation,isGifted,isELL,guardian1FirstName,guardian1LastName,guardian1Email
STU-001,John,Smith,2010-03-15,false,true,false,Jane,Smith,jane.smith@email.com
STU-002,Maria,Garcia,2010-07-22,true,false,true,Carmen,Garcia,carmen.garcia@email.com
```

### Bulk Export Students
```http
POST /api/v1/students/bulk-export
Authorization: Bearer {token}
Content-Type: application/json

{
  "format": "csv",
  "filters": {
    "schoolIds": ["school-123", "school-456"],
    "gradeLevel": "Grade6", 
    "includeWithdrawn": false,
    "includeGuardians": true,
    "includeEnrollmentHistory": false
  },
  "deliveryMethod": "download" // or "email"
}
```

**Response (202 Accepted)**:
```json
{
  "exportId": "student-export-456",
  "status": "Processing",
  "estimatedRecords": 1250,
  "estimatedCompletionTime": "2024-12-19T10:05:00Z"
}
```

## Identity Integration

### Map Student to External Identity
```http
POST /api/v1/students/{userId}/identity-mapping
Authorization: Bearer {token}
Content-Type: application/json

{
  "externalId": "entra-external-id-789",
  "issuer": "https://login.microsoftonline.com/tenant-id"
}
```

### Student Lifecycle Events
```http
POST /api/v1/students/{userId}/lifecycle
Authorization: Bearer {token}
Content-Type: application/json

{
  "eventType": "suspend", // suspend, reinstate, graduate, withdraw
  "effectiveDate": "2024-12-20T00:00:00Z",
  "reason": "Disciplinary action - 10 day suspension",
  "metadata": {
    "returnDate": "2024-12-30T00:00:00Z",
    "conditions": ["Complete community service", "Parent meeting required"]
  }
}
```

## RBAC Examples

### District Admin Access
- Full CRUD on all students in district
- Can perform bulk operations
- Can access enrollment history across schools

### School User Access
- CRUD on students in assigned schools only
- Limited bulk operations (school-scoped)
- Cannot access other schools' students

### Teacher Access
- Read-only access to students in assigned classes
- Can view current enrollment and basic info
- Cannot modify student records directly

## Performance Considerations
- Student list endpoints limited to 100 records per request
- Bulk operations queued and processed asynchronously
- Search operations use full-text indexing
- Enrollment history queries may have higher latency for large datasets