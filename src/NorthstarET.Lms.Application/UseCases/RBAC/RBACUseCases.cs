using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs.RBAC;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.ValueObjects;

namespace NorthstarET.Lms.Application.UseCases.RBAC;

public class AssignRoleUseCase
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IRoleDefinitionRepository _roleDefinitionRepository;
    private readonly IAuditService _auditService;

    public AssignRoleUseCase(
        IRoleAssignmentRepository roleAssignmentRepository,
        IRoleDefinitionRepository roleDefinitionRepository,
        IAuditService auditService)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
        _roleDefinitionRepository = roleDefinitionRepository;
        _auditService = auditService;
    }

    public async Task<Result<RoleAssignmentDto>> ExecuteAsync(
        AssignRoleRequest request,
        string assignedByUserId)
    {
        // Validate role definition exists
        var roleDefinition = await _roleDefinitionRepository.GetByIdAsync(request.RoleDefinitionId);
        if (roleDefinition == null)
        {
            return Result<RoleAssignmentDto>.Failure("Role definition not found");
        }

        // Validate scope constraints
        var validationResult = ValidateRoleScope(roleDefinition, request);
        if (!validationResult.IsSuccess)
        {
            return Result<RoleAssignmentDto>.Failure(validationResult.Error);
        }

        // Check for existing assignment
        var existingAssignment = await _roleAssignmentRepository.GetActiveAssignmentAsync(
            request.UserId, request.RoleDefinitionId, request.SchoolId, request.ClassId, request.SchoolYearId);
        
        if (existingAssignment != null)
        {
            return Result<RoleAssignmentDto>.Failure("User already has this role assignment");
        }

        // Create role assignment
        var assignment = new RoleAssignment(
            request.UserId,
            request.RoleDefinitionId,
            request.SchoolId,
            request.ClassId,
            request.SchoolYearId,
            request.EffectiveDate,
            request.ExpirationDate);

        // Handle delegation if specified
        if (request.DelegatedByUserId != null)
        {
            assignment.SetDelegation(request.DelegatedByUserId.Value, request.DelegationExpiry);
        }

        await _roleAssignmentRepository.AddAsync(assignment);
        await _roleAssignmentRepository.SaveChangesAsync();

        // Generate audit record
        await _auditService.LogAsync(
            "RoleAssigned",
            typeof(RoleAssignment).Name,
            assignment.Id,
            assignedByUserId,
            new { request.UserId, request.RoleDefinitionId, roleDefinition.Name, Scope = GetScopeDescription(request) });

        var dto = new RoleAssignmentDto
        {
            Id = assignment.Id,
            UserId = assignment.UserId,
            RoleDefinitionId = assignment.RoleDefinitionId,
            RoleName = roleDefinition.Name,
            SchoolId = assignment.SchoolId,
            ClassId = assignment.ClassId,
            SchoolYearId = assignment.SchoolYearId,
            EffectiveDate = assignment.EffectiveDate,
            ExpirationDate = assignment.ExpirationDate,
            IsActive = assignment.IsActive,
            DelegatedByUserId = assignment.DelegatedByUserId,
            DelegationExpiry = assignment.DelegationExpiry
        };

        return Result<RoleAssignmentDto>.Success(dto);
    }

    private Result ValidateRoleScope(RoleDefinition roleDefinition, AssignRoleRequest request)
    {
        switch (roleDefinition.Scope)
        {
            case RoleScope.Platform:
                if (request.SchoolId.HasValue || request.ClassId.HasValue || request.SchoolYearId.HasValue)
                    return Result.Failure("Platform roles cannot be scoped to specific schools, classes, or years");
                break;
            
            case RoleScope.District:
                if (request.SchoolId.HasValue || request.ClassId.HasValue)
                    return Result.Failure("District roles cannot be scoped to specific schools or classes");
                break;
            
            case RoleScope.School:
                if (!request.SchoolId.HasValue)
                    return Result.Failure("School roles must specify a school");
                if (request.ClassId.HasValue)
                    return Result.Failure("School roles cannot be scoped to specific classes");
                break;
            
            case RoleScope.Class:
                if (!request.ClassId.HasValue)
                    return Result.Failure("Class roles must specify a class");
                break;
        }

        return Result.Success();
    }

    private string GetScopeDescription(AssignRoleRequest request)
    {
        var parts = new List<string>();
        if (request.SchoolId.HasValue) parts.Add($"School:{request.SchoolId}");
        if (request.ClassId.HasValue) parts.Add($"Class:{request.ClassId}");
        if (request.SchoolYearId.HasValue) parts.Add($"Year:{request.SchoolYearId}");
        return string.Join(", ", parts);
    }
}

public class RevokeRoleUseCase
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IAuditService _auditService;

    public RevokeRoleUseCase(
        IRoleAssignmentRepository roleAssignmentRepository,
        IAuditService auditService)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
        _auditService = auditService;
    }

    public async Task<Result> ExecuteAsync(Guid assignmentId, string revokedByUserId)
    {
        var assignment = await _roleAssignmentRepository.GetByIdAsync(assignmentId);
        if (assignment == null)
        {
            return Result.Failure("Role assignment not found");
        }

        if (!assignment.IsActive)
        {
            return Result.Failure("Role assignment is already inactive");
        }

        assignment.Revoke(revokedByUserId);
        await _roleAssignmentRepository.SaveChangesAsync();

        await _auditService.LogAsync(
            "RoleRevoked",
            typeof(RoleAssignment).Name,
            assignment.Id,
            revokedByUserId,
            new { assignment.UserId, assignment.RoleDefinitionId });

        return Result.Success();
    }
}

public class GetUserRolesUseCase
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IRoleDefinitionRepository _roleDefinitionRepository;

    public GetUserRolesUseCase(
        IRoleAssignmentRepository roleAssignmentRepository,
        IRoleDefinitionRepository roleDefinitionRepository)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
        _roleDefinitionRepository = roleDefinitionRepository;
    }

    public async Task<Result<List<UserRoleDto>>> ExecuteAsync(Guid userId, DateTime? asOfDate = null)
    {
        var effectiveDate = asOfDate ?? DateTime.UtcNow;
        var assignments = await _roleAssignmentRepository.GetActiveAssignmentsByUserAsync(userId, effectiveDate);
        var roleDefinitionIds = assignments.Select(a => a.RoleDefinitionId).Distinct();
        var roleDefinitions = await _roleDefinitionRepository.GetByIdsAsync(roleDefinitionIds);

        var userRoles = assignments.Select(assignment =>
        {
            var roleDefinition = roleDefinitions.First(rd => rd.Id == assignment.RoleDefinitionId);
            return new UserRoleDto
            {
                AssignmentId = assignment.Id,
                RoleDefinitionId = roleDefinition.Id,
                RoleName = roleDefinition.Name,
                RoleDescription = roleDefinition.Description,
                Scope = roleDefinition.Scope.ToString(),
                Permissions = roleDefinition.Permissions,
                SchoolId = assignment.SchoolId,
                ClassId = assignment.ClassId,
                SchoolYearId = assignment.SchoolYearId,
                EffectiveDate = assignment.EffectiveDate,
                ExpirationDate = assignment.ExpirationDate,
                IsDelegated = assignment.DelegatedByUserId.HasValue,
                DelegatedByUserId = assignment.DelegatedByUserId,
                DelegationExpiry = assignment.DelegationExpiry
            };
        }).ToList();

        return Result<List<UserRoleDto>>.Success(userRoles);
    }
}

public class CheckPermissionUseCase
{
    private readonly IRoleAuthorizationService _roleAuthorizationService;

    public CheckPermissionUseCase(IRoleAuthorizationService roleAuthorizationService)
    {
        _roleAuthorizationService = roleAuthorizationService;
    }

    public async Task<Result<bool>> ExecuteAsync(
        Guid userId,
        string permission,
        Guid? schoolId = null,
        Guid? classId = null,
        Guid? schoolYearId = null)
    {
        var hasPermission = await _roleAuthorizationService.HasPermissionAsync(
            userId, permission, schoolId, classId, schoolYearId);

        return Result<bool>.Success(hasPermission);
    }
}