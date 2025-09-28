# Research: Foundational LMS with Tenant Isolation and Compliance

**Feature**: 001-foundational-lms-with  
**Date**: December 19, 2024  
**Status**: Phase 0 Complete

## Problem Domain Analysis

### Multi-Tenant Educational Systems
**Challenge**: Strict data isolation between school districts while maintaining shared infrastructure
**Solution**: Tenant-per-schema approach with row-level security policies ensuring complete data segregation
**Key Considerations**:
- FERPA compliance requires absolute data isolation
- Performance impact of tenant filtering on all queries
- Backup and disaster recovery per tenant
- Cross-tenant reporting explicitly forbidden

### Educational Domain Complexity
**Challenge**: Complex hierarchical relationships and temporal scoping
**Solution**: Domain-driven design with clear aggregate boundaries
**Key Entities & Relationships**:
- District (Tenant Root) → Schools → Classes → Enrollments
- SchoolYear provides temporal context for all academic data
- Staff can have multiple roles across schools and years
- Students maintain global identity but enrollment history per district

### Compliance & Audit Requirements
**Challenge**: FERPA compliance, data retention, legal holds, tamper-evident logs
**Solution**: Event sourcing for audit trail, immutable append-only logs with cryptographic chaining
**Implementation Approach**:
- All mutations generate immutable audit events
- SHA-256 chaining for tamper detection
- Configurable retention with legal hold overrides
- Automated purge jobs with compliance verification

## Technical Research

### Multi-Tenancy Patterns in .NET
**Evaluated Approaches**:
1. **Database-per-tenant**: Too expensive, management overhead
2. **Schema-per-tenant**: ✅ CHOSEN - Balance of isolation and efficiency
3. **Row-level security**: Insufficient isolation for FERPA compliance

**Implementation Strategy**:
- Dynamic connection strings based on tenant context
- EF Core database context per tenant schema
- Aspire service discovery for tenant-specific database connections

### RBAC Implementation
**Challenge**: Flexible role system with hierarchical permissions and delegation
**Solution**: Policy-based authorization with custom requirements
**Architecture**:
- Role definitions with capability matrices
- Hierarchical inheritance (Platform > District > School > Class)
- Temporary delegation with auto-expiry
- Deny-by-default with explicit grants

### Performance & Scale Considerations
**Requirements**:
- CRUD operations: <200ms p95
- Bulk operations: <120s for 10k records
- Audit queries: <2s on 1M records

**Optimization Strategy**:
- Read replicas for reporting queries
- Bulk operation queuing with progress tracking
- Indexed audit tables with partitioning
- Connection pooling per tenant

### External Integration Requirements
**Entra External ID Integration**:
- Identity mapping and lifecycle management
- B2B user invitation flows
- Token-based API authentication
- Identity provider federation

**SIS Integration Readiness**:
- Standardized CSV/JSON import/export formats
- Idempotent API endpoints with correlation IDs
- Webhook support for real-time synchronization
- Error handling and retry policies

## Risk Assessment

### High Risk Areas
1. **Tenant Data Leakage**: Mitigation through automated testing, schema isolation
2. **Performance Degradation**: Mitigation through caching, read replicas, query optimization
3. **Compliance Violations**: Mitigation through automated compliance checks, audit reviews
4. **Identity Provider Outages**: Mitigation through graceful degradation, cached tokens

### Technical Debt Considerations
- Multi-tenant database migrations complexity
- Testing across tenant boundaries
- Monitoring and logging per tenant
- Backup and recovery procedures

## Architecture Decisions

### ADR-001: Schema-per-Tenant Multi-Tenancy
**Decision**: Use database schema isolation for tenant separation
**Rationale**: FERPA compliance requires strong data isolation while maintaining operational efficiency
**Trade-offs**: Higher complexity vs. regulatory compliance requirements

### ADR-002: Clean Architecture with Domain-Driven Design
**Decision**: Implement 4-layer clean architecture with rich domain models
**Rationale**: Complex educational domain requires clear boundaries and testable business logic
**Trade-offs**: Initial development overhead vs. long-term maintainability

### ADR-003: Event Sourcing for Audit Trail
**Decision**: Implement immutable audit log with cryptographic chaining
**Rationale**: Compliance requires tamper-evident audit trails for all data changes
**Trade-offs**: Storage overhead vs. compliance requirements

### ADR-004: .NET Aspire for Orchestration
**Decision**: Use Aspire for service discovery and configuration management
**Rationale**: Simplifies multi-tenant service orchestration and development environment setup
**Trade-offs**: Framework lock-in vs. development productivity

## Next Steps

Phase 0 complete. Ready for Phase 1:
- Data model design with multi-tenant considerations
- API contract definitions for all functional requirements  
- Quickstart guide for development environment setup
- GitHub Copilot instructions for LMS domain context

**Confidence Level**: HIGH - Clear requirements, proven patterns, manageable complexity
**Estimated Effort**: 8-12 weeks for MVP with 3-4 developers
**Risk Level**: MEDIUM - Multi-tenancy and compliance add complexity but patterns are well-established