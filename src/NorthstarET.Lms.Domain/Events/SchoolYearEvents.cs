using NorthstarET.Lms.Domain.Enums;

namespace NorthstarET.Lms.Domain.Events;

public record SchoolYearActivatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid SchoolYearId { get; }
    public string SchoolYearName { get; }
    public string ActivatedByUserId { get; }

    public SchoolYearActivatedEvent(Guid schoolYearId, string schoolYearName, string activatedByUserId)
    {
        SchoolYearId = schoolYearId;
        SchoolYearName = schoolYearName;
        ActivatedByUserId = activatedByUserId;
    }
}

public record SchoolYearArchivedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid SchoolYearId { get; }
    public string SchoolYearName { get; }
    public string ArchivedByUserId { get; }

    public SchoolYearArchivedEvent(Guid schoolYearId, string schoolYearName, string archivedByUserId)
    {
        SchoolYearId = schoolYearId;
        SchoolYearName = schoolYearName;
        ArchivedByUserId = archivedByUserId;
    }
}

public record RoleAssignmentRevokedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    
    public Guid RoleAssignmentId { get; }
    public Guid UserId { get; }
    public Guid RoleDefinitionId { get; }
    public string Reason { get; }
    public string RevokedByUserId { get; }

    public RoleAssignmentRevokedEvent(Guid roleAssignmentId, Guid userId, Guid roleDefinitionId, string reason, string revokedByUserId)
    {
        RoleAssignmentId = roleAssignmentId;
        UserId = userId;
        RoleDefinitionId = roleDefinitionId;
        Reason = reason;
        RevokedByUserId = revokedByUserId;
    }
}