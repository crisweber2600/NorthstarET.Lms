using NorthstarET.Lms.Application.Common;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Domain.Entities;
using NorthstarET.Lms.Domain.Enums;
using NorthstarET.Lms.Domain.Services;

namespace NorthstarET.Lms.Application.Services;

public class RoleAuthorizationService
{
    private readonly IRoleDefinitionRepository _roleDefinitionRepository;
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContext;
    private readonly IAuditService _auditService;

    public RoleAuthorizationService(
        IRoleDefinitionRepository roleDefinitionRepository,
        IRoleAssignmentRepository roleAssignmentRepository,
        IUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContext,
        IAuditService auditService)
    {
        _roleDefinitionRepository = roleDefinitionRepository;
        _roleAssignmentRepository = roleAssignmentRepository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _auditService = auditService;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission)
    {
        var roleAssignments = await _roleAssignmentRepository.GetActiveRolesByUserIdAsync(userId);
        
        foreach (var assignment in roleAssignments)
        {
            var roleDefinition = await _roleDefinitionRepository.GetByIdAsync(assignment.RoleDefinitionId);
            if (roleDefinition != null && roleDefinition.HasPermission(permission))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<IEnumerable<string>> GetEffectivePermissionsAsync(Guid userId)
    {
        var roleAssignments = await _roleAssignmentRepository.GetActiveRolesByUserIdAsync(userId);
        var permissions = new HashSet<string>();

        foreach (var assignment in roleAssignments)
        {
            var roleDefinition = await _roleDefinitionRepository.GetByIdAsync(assignment.RoleDefinitionId);
            if (roleDefinition != null)
            {
                foreach (var permission in roleDefinition.GetPermissions())
                {
                    permissions.Add(permission);
                }
            }
        }

        return permissions;
    }

    public async Task<Result<RoleAssignmentDto>> AssignRoleAsync(AssignRoleDto assignRoleDto, string assignedBy)
    {
        // Validate role definition exists
        var roleDefinition = await _roleDefinitionRepository.GetByIdAsync(assignRoleDto.RoleDefinitionId);
        if (roleDefinition == null)
        {
            return Result.Failure<RoleAssignmentDto>("Role definition not found");
        }

        // Check for duplicate assignment
        var existingAssignment = await _roleAssignmentRepository.GetActiveRoleAssignmentAsync(
            assignRoleDto.UserId, 
            assignRoleDto.RoleDefinitionId, 
            assignRoleDto.SchoolId, 
            assignRoleDto.ClassId);
        
        if (existingAssignment != null)
        {
            return Result.Failure<RoleAssignmentDto>("User already has this role assignment");
        }

        // Create role assignment
        RoleAssignment roleAssignment;
        if (assignRoleDto.ClassId.HasValue)
        {
            roleAssignment = new RoleAssignment(
                assignRoleDto.UserId,
                assignRoleDto.RoleDefinitionId,
                assignRoleDto.SchoolId,
                assignedBy,
                assignRoleDto.ClassId);
        }
        else if (assignRoleDto.SchoolId.HasValue)
        {
            roleAssignment = new RoleAssignment(
                assignRoleDto.UserId,
                assignRoleDto.RoleDefinitionId,
                assignRoleDto.SchoolId,
                assignedBy);
        }
        else
        {
            roleAssignment = new RoleAssignment(
                assignRoleDto.UserId,
                assignRoleDto.RoleDefinitionId,
                assignRoleDto.SchoolId,
                assignedBy);
        }

        if (assignRoleDto.ExpirationDate.HasValue)
        {
            roleAssignment.SetExpiration(assignRoleDto.ExpirationDate.Value, assignedBy);
        }

        // Validate scope matches role requirements
        if (!await ValidateRoleScopeAsync(roleAssignment))
        {
            return Result.Failure<RoleAssignmentDto>("Role scope validation failed");
        }

        await _roleAssignmentRepository.AddAsync(roleAssignment);
        await _unitOfWork.SaveChangesAsync();

        // Audit the assignment
        await _auditService.LogAuditEventAsync(new CreateAuditRecordDto
        {
            Action = "ASSIGN_ROLE",
            EntityType = "RoleAssignment",
            EntityId = roleAssignment.Id,
            UserId = assignedBy,
            Details = $"Assigned role {roleDefinition.Name} to user {assignRoleDto.UserId}",
            IpAddress = "127.0.0.1"
        });

        return Result.Success(MapToDto(roleAssignment, roleDefinition.Name));
    }

    public async Task<Result<bool>> RevokeRoleAsync(Guid roleAssignmentId, string reason, string revokedBy)
    {
        var roleAssignment = await _roleAssignmentRepository.GetByIdAsync(roleAssignmentId);
        if (roleAssignment == null)
        {
            return Result.Failure<bool>("Role assignment not found");
        }

        roleAssignment.Revoke(reason, revokedBy);
        
        await _roleAssignmentRepository.UpdateAsync(roleAssignment);
        await _unitOfWork.SaveChangesAsync();

        // Audit the revocation
        await _auditService.LogAuditEventAsync(new CreateAuditRecordDto
        {
            Action = "REVOKE_ROLE",
            EntityType = "RoleAssignment",
            EntityId = roleAssignment.Id,
            UserId = revokedBy,
            Details = $"Revoked role assignment: {reason}",
            IpAddress = "127.0.0.1"
        });

        return Result.Success(true);
    }

    public async Task<bool> ValidateRoleScopeAsync(RoleAssignment roleAssignment)
    {
        var roleDefinition = await _roleDefinitionRepository.GetByIdAsync(roleAssignment.RoleDefinitionId);
        if (roleDefinition == null)
        {
            return false;
        }

        return roleDefinition.Scope switch
        {
            RoleScope.District => roleAssignment.SchoolId == null && roleAssignment.ClassId == null,
            RoleScope.School => roleAssignment.SchoolId != null && roleAssignment.ClassId == null,
            RoleScope.Class => roleAssignment.ClassId != null,
            _ => false
        };
    }

    private static RoleAssignmentDto MapToDto(RoleAssignment assignment, string roleName)
    {
        return new RoleAssignmentDto
        {
            Id = assignment.Id,
            UserId = assignment.UserId,
            RoleDefinitionId = assignment.RoleDefinitionId,
            RoleName = roleName,
            SchoolId = assignment.SchoolId,
            ClassId = assignment.ClassId,
            AssignedDate = assignment.AssignedDate,
            ExpirationDate = assignment.ExpirationDate,
            AssignedBy = assignment.AssignedBy,
            IsActive = assignment.IsActive
        };
    }
}