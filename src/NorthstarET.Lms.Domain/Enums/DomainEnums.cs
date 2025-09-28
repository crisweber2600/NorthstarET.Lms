namespace NorthstarET.Lms.Domain.Enums;

public enum DistrictStatus
{
    Active,
    Suspended,
    PendingDeletion,
    Archived
}

public enum UserLifecycleStatus
{
    Active,
    Suspended,
    Withdrawn,
    Transferred,
    Graduated
}

public enum GradeLevel
{
    PreK,
    Kindergarten,
    Grade1,
    Grade2,
    Grade3,
    Grade4,
    Grade5,
    Grade6,
    Grade7,
    Grade8,
    Grade9,
    Grade10,
    Grade11,
    Grade12
}

public enum SchoolYearStatus
{
    Planning,
    Active,
    Archived
}

public enum RoleAssignmentStatus
{
    Active,
    Revoked,
    Expired,
    Suspended
}

public enum AuditEventType
{
    Create,
    Update,
    Delete,
    BulkOperation,
    SecurityViolation,
    RoleAssigned,
    RoleRevoked,
    LoginAttempt,
    DataPurged
}

public enum EnrollmentStatus
{
    Active,
    Transferred,
    Graduated,
    Withdrawn,
    Inactive
}

public enum SchoolType
{
    Elementary,
    Middle,
    High,
    K12,
    Alternative,
    Charter
}

public enum RelationshipType
{
    Parent,
    Guardian,
    StepParent,
    Grandparent,
    EmergencyContact
}

public enum RoleScope
{
    Platform,
    District,
    School,
    Class
}

public enum GuardianRelationshipType
{
    Parent,
    Guardian,
    StepParent,
    Grandparent,
    Aunt,
    Uncle,
    Sibling,
    FosterParent,
    Other
}