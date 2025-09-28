# API Contracts: District Management

**Feature**: 001-foundational-lms-with  
**Domain**: Platform Administration  
**Base URL**: `/api/v1/districts`

## Authentication
All endpoints require authentication. PlatformAdmin role required for most operations.

## District CRUD Operations

### Create District
```http
POST /api/v1/districts
Authorization: Bearer {token}
Content-Type: application/json

{
  "slug": "oakland-unified",
  "displayName": "Oakland Unified School District",
  "quotas": {
    "maxStudents": 50000,
    "maxStaff": 5000,
    "maxAdmins": 100
  }
}
```

**Response (201 Created)**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "slug": "oakland-unified",
  "displayName": "Oakland Unified School District",
  "status": "Active",
  "quotas": {
    "maxStudents": 50000,
    "maxStaff": 5000,
    "maxAdmins": 100
  },
  "createdDate": "2024-12-19T10:00:00Z",
  "createdByUserId": "admin-user-id"
}
```

**Error Responses**:
- `400 Bad Request`: Invalid slug format or duplicate slug
- `403 Forbidden`: Not a PlatformAdmin
- `409 Conflict`: District slug already exists

### Get District
```http
GET /api/v1/districts/{id}
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "slug": "oakland-unified", 
  "displayName": "Oakland Unified School District",
  "status": "Active",
  "quotas": {
    "maxStudents": 50000,
    "maxStaff": 5000,
    "maxAdmins": 100,
    "currentStudents": 12543,
    "currentStaff": 1205,
    "currentAdmins": 23
  },
  "createdDate": "2024-12-19T10:00:00Z",
  "lastModified": "2024-12-19T10:00:00Z"
}
```

### List Districts
```http
GET /api/v1/districts?page=1&size=20&status=Active
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "slug": "oakland-unified",
      "displayName": "Oakland Unified School District", 
      "status": "Active",
      "currentStudents": 12543,
      "currentStaff": 1205,
      "createdDate": "2024-12-19T10:00:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "size": 20,
    "totalItems": 156,
    "totalPages": 8,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

### Update District
```http
PUT /api/v1/districts/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "displayName": "Oakland Unified School District - Updated",
  "quotas": {
    "maxStudents": 60000,
    "maxStaff": 6000, 
    "maxAdmins": 150
  }
}
```

### District Lifecycle Management

#### Suspend District
```http
POST /api/v1/districts/{id}/suspend
Authorization: Bearer {token}
Content-Type: application/json

{
  "reason": "Policy violation - unauthorized data access",
  "effectiveDate": "2024-12-20T00:00:00Z"
}
```

#### Reactivate District
```http
POST /api/v1/districts/{id}/reactivate
Authorization: Bearer {token}
Content-Type: application/json

{
  "reason": "Policy compliance verified"
}
```

#### Delete District
```http
DELETE /api/v1/districts/{id}
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
  "canDelete": true,
  "retentionCheck": {
    "studentsEligibleForPurge": 1250,
    "studentsOnLegalHold": 23,
    "staffEligibleForPurge": 145,
    "staffOnLegalHold": 2
  },
  "message": "District scheduled for deletion after retention period compliance"
}
```

**Error Response (409 Conflict)**:
```json
{
  "canDelete": false,
  "errors": [
    "12 student records have active legal holds",
    "Retention period not met for 2,345 student records",
    "3 staff records have active legal holds"
  ]
}
```

## Quota Management

### Check Quota Status
```http
GET /api/v1/districts/{id}/quota-status
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
  "students": {
    "limit": 50000,
    "current": 12543,
    "available": 37457,
    "utilizationPercent": 25.1
  },
  "staff": {
    "limit": 5000,
    "current": 1205,
    "available": 3795,
    "utilizationPercent": 24.1
  },
  "admins": {
    "limit": 100,
    "current": 23,
    "available": 77,
    "utilizationPercent": 23.0
  }
}
```

### Update Quotas
```http
PATCH /api/v1/districts/{id}/quotas
Authorization: Bearer {token}
Content-Type: application/json

{
  "maxStudents": 75000,
  "maxStaff": 7500,
  "maxAdmins": 200
}
```

## Bulk Operations

### Bulk Import Districts
```http
POST /api/v1/districts/bulk-import
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: districts.csv
errorHandling: "best-effort" | "all-or-nothing" | "threshold-based"
thresholdPercent: 5
```

**CSV Format**:
```csv
slug,displayName,maxStudents,maxStaff,maxAdmins
oakland-unified,Oakland Unified School District,50000,5000,100
berkeley-unified,Berkeley Unified School District,12000,1200,50
```

**Response (202 Accepted)**:
```json
{
  "jobId": "bulk-import-3fa85f64",
  "status": "Processing",
  "totalRecords": 25,
  "estimatedCompletionTime": "2024-12-19T10:05:00Z"
}
```

### Get Bulk Operation Status
```http
GET /api/v1/districts/bulk-operations/{jobId}
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
  "jobId": "bulk-import-3fa85f64",
  "status": "Completed",
  "totalRecords": 25,
  "successfulRecords": 23,
  "failedRecords": 2,
  "startTime": "2024-12-19T10:02:00Z",
  "completionTime": "2024-12-19T10:04:30Z",
  "errors": [
    {
      "row": 15,
      "error": "Duplicate slug: 'duplicate-district'",
      "record": { "slug": "duplicate-district", "displayName": "..." }
    },
    {
      "row": 18, 
      "error": "Invalid quota: maxStudents cannot exceed 100000",
      "record": { "slug": "large-district", "maxStudents": 150000 }
    }
  ]
}
```

## Error Handling

### Standard Error Response
```json
{
  "error": "ValidationError",
  "message": "Request validation failed",
  "details": [
    {
      "field": "slug",
      "message": "Slug must contain only lowercase letters, numbers, and hyphens"
    },
    {
      "field": "quotas.maxStudents", 
      "message": "Must be between 1 and 100,000"
    }
  ],
  "correlationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2024-12-19T10:00:00Z"
}
```

### HTTP Status Codes
- `200 OK`: Successful GET/PUT/PATCH
- `201 Created`: Successful POST
- `202 Accepted`: Async operation started
- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: District not found
- `409 Conflict`: Business rule violation (duplicate slug, deletion constraints)
- `422 Unprocessable Entity`: Valid format but business logic error
- `429 Too Many Requests`: Rate limit exceeded
- `500 Internal Server Error`: Unexpected server error

## Rate Limiting
- Platform operations: 100 requests/minute per PlatformAdmin
- Bulk operations: 5 concurrent jobs per PlatformAdmin
- Standard CRUD: 1000 requests/minute per tenant

## Idempotency
All CREATE and UPDATE operations support idempotency keys:
```http
POST /api/v1/districts
Idempotency-Key: unique-operation-key-12345
```

Idempotency keys are valid for 24 hours. Duplicate requests with same key return original response.

## Audit Trail
All district operations generate audit records:
- Operation type (Create, Update, Suspend, Delete)
- Acting user and timestamp
- Before/after values for changes
- IP address and user agent
- Correlation ID for bulk operations

Audit records are immutable and tamper-evident through cryptographic chaining.