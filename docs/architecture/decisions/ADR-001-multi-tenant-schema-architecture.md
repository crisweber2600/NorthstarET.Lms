# Architecture Decision Record (ADR-001): Multi-Tenant Architecture with Schema-Per-Tenant

## Status
**Accepted** - December 19, 2024

## Context
The NorthstarET Learning Management System (LMS) needs to serve multiple school districts while ensuring complete data isolation, regulatory compliance (FERPA), and scalable performance. Each district requires:

- Complete data isolation from other districts
- Independent scaling and maintenance capabilities  
- Compliance with educational data privacy regulations
- Performance optimization per district usage patterns
- Ability to customize business rules per district

### Options Considered

1. **Single Database with Tenant ID Column**
   - Pros: Simple implementation, single database maintenance
   - Cons: Risk of data leakage, difficult compliance auditing, shared resource contention

2. **Database-Per-Tenant**  
   - Pros: Complete isolation, independent scaling, easy compliance
   - Cons: High operational overhead, difficult cross-tenant analytics, expensive

3. **Schema-Per-Tenant** (Selected)
   - Pros: Strong isolation, manageable operations, compliance-friendly, cost-effective
   - Cons: Database-level operations complexity, schema management overhead

## Decision
We will implement a **Schema-Per-Tenant** architecture where each school district gets its own database schema within a shared database instance.

### Implementation Details

#### Schema Naming Convention
- District slug converted to schema name: `oakland-unified` → `oakland_unified`
- Platform-level tables remain in the default `dbo` schema
- Each tenant schema contains complete educational domain model

#### Database Structure
```sql
-- Platform Level (dbo schema)
dbo.district_tenants              -- Tenant registry
dbo.platform_audit_records       -- Cross-tenant audit
dbo.platform_configurations      -- Global settings

-- Tenant Level (e.g., oakland_unified schema)
oakland_unified.students          -- Student records
oakland_unified.staff             -- Staff records  
oakland_unified.schools           -- School information
oakland_unified.classes           -- Class/course data
oakland_unified.enrollments       -- Student enrollments
oakland_unified.audit_records     -- Tenant audit trail
oakland_unified.role_assignments  -- RBAC data
```

## Decision Rationale

### Security & Compliance Benefits
- **Complete Data Isolation**: SQL-level isolation prevents accidental cross-tenant queries
- **Audit Trail Separation**: Each district maintains independent audit logs for compliance
- **Regulatory Compliance**: FERPA requirements met through physical data separation
- **Tenant-Specific Retention**: Independent data retention policies per district

### Performance Benefits
- **Query Performance**: Tenant-specific indexes and optimization
- **Resource Allocation**: Database-level resource management per schema
- **Parallel Operations**: Concurrent tenant operations without blocking
- **Scalability**: Independent schema scaling based on district size

### Cost Efficiency
- **Shared Infrastructure**: Lower cost than database-per-tenant
- **Resource Sharing**: Efficient database resource utilization
- **Operational Overhead**: Manageable compared to separate databases

## Consequences

### Positive Consequences
- Strong data isolation guarantees
- Simplified compliance and audit processes
- Flexible per-tenant customization
- Cost-effective scalability model
- Enhanced security posture

### Negative Consequences  
- Increased database schema management complexity
- Cross-tenant reporting requires special handling
- Database migration coordination across tenants
- Connection pooling optimization required

## References
- [Multi-Tenant Data Architecture](https://docs.microsoft.com/en-us/azure/sql-database/saas-tenancy-app-design-patterns)
- [FERPA Compliance Guidelines](https://studentprivacy.ed.gov/audience/school-officials)

---
**Approved**: December 19, 2024