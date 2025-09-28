using NorthstarET.Lms.Domain.Enums;

namespace NorthstarET.Lms.Application.DTOs;

// School DTOs
public class CreateSchoolDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PhysicalAddress { get; set; }
    public string? MailingAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
}

public class UpdateSchoolDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? PhysicalAddress { get; set; }
    public string? MailingAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
}

public class SchoolDto
{
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PhysicalAddress { get; set; }
    public string? MailingAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}

// Staff DTOs
public class CreateStaffDto
{
    public string StaffNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime HireDate { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}

public class UpdateStaffDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
}

public class StaffDto
{
    public Guid UserId { get; set; }
    public string StaffNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string EmailAddress { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime HireDate { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public UserLifecycleStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}

public class StaffListDto
{
    public Guid UserId { get; set; }
    public string StaffNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public UserLifecycleStatus Status { get; set; }
}

public class StaffDetailDto : StaffDto
{
    public List<RoleAssignmentDto> RoleAssignments { get; set; } = new();
    public List<ClassAssignmentDto> ClassAssignments { get; set; } = new();
}

public class ClassAssignmentDto
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string SchoolName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
}