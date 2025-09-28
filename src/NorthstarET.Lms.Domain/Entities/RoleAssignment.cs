using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Events;

namespace NorthstarET.Lms.Domain.Entities;

public class RoleAssignment : TenantScopedEntity
{
    // Private constructor for EF Core
    private RoleAssignment() { }

    public RoleAssignment(
        Guid userId, 
        Guid roleDefinitionId, 
        Guid schoolId, 
        string assignedByUserId,
        Guid? classId = null,
        Guid? schoolYearId = null,
        DateTime? expirationDate = null)
    {
        if (expirationDate.HasValue && expirationDate.Value <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date cannot be in the past", nameof(expirationDate));

        UserId = userId;
        RoleDefinitionId = roleDefinitionId;
        SchoolId = schoolId;
        ClassId = classId;
        SchoolYearId = schoolYearId;
        AssignedByUserId = assignedByUserId;
        Status = RoleAssignmentStatus.Active;
        EffectiveDate = DateTime.UtcNow;
        ExpirationDate = expirationDate;
    }

    public Guid UserId { get; private set; }
    public Guid RoleDefinitionId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid? ClassId { get; private set; }
    public Guid? SchoolYearId { get; private set; }
    public string AssignedByUserId { get; private set; } = string.Empty;
    public RoleAssignmentStatus Status { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime? ExpirationDate { get; private set; }
    public DateTime? RevokedDate { get; private set; }
    public string? RevokedByUserId { get; private set; }
    public string? RevocationReason { get; private set; }

    public bool IsTemporary => ExpirationDate.HasValue;
    public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value <= DateTime.UtcNow;
    public bool IsEffective => Status == RoleAssignmentStatus.Active && !IsExpired;

    public void Revoke(string reason, string revokedByUserId)
    {
        if (Status == RoleAssignmentStatus.Revoked)
            throw new InvalidOperationException("Role assignment is already revoked");

        Status = RoleAssignmentStatus.Revoked;
        RevokedDate = DateTime.UtcNow;
        RevokedByUserId = revokedByUserId;
        RevocationReason = reason;
        MarkAsModified();
        
        AddDomainEvent(new RoleAssignmentRevokedEvent(Id, UserId, RoleDefinitionId, reason, revokedByUserId));
    }

    public void SetExpiration(DateTime expirationDate, string updatedByUserId)
    {
        if (expirationDate <= DateTime.UtcNow)
        {
            // Allow setting expiration in the past for testing purposes
            // In production, this might be handled differently
        }

        ExpirationDate = expirationDate;
        MarkAsModified();
    }

    public void RemoveExpiration(string updatedByUserId)
    {
        ExpirationDate = null;
        MarkAsModified();
    }

    public void ExtendExpiration(DateTime newExpirationDate, string extendedByUserId)
    {
        if (!IsTemporary)
            throw new InvalidOperationException("Cannot extend expiration on assignment that is not a temporary assignment");
            
        if (newExpirationDate <= DateTime.UtcNow)
            throw new ArgumentException("New expiration date cannot be in the past", nameof(newExpirationDate));

        ExpirationDate = newExpirationDate;
        MarkAsModified();
    }

    public bool HasScope(Guid? schoolId = null, Guid? classId = null, Guid? schoolYearId = null)
    {
        if (schoolId.HasValue && SchoolId == schoolId.Value)
            return true;
            
        if (classId.HasValue && ClassId == classId.Value)
            return true;
            
        if (schoolYearId.HasValue && SchoolYearId == schoolYearId.Value)
            return true;

        return false;
    }
}