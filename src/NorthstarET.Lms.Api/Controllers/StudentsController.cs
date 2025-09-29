using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthstarET.Lms.Application.Commands;
using NorthstarET.Lms.Application.Queries;
using NorthstarET.Lms.Application.DTOs;
using NorthstarET.Lms.Application.Interfaces;
using NorthstarET.Lms.Application.Services;

namespace NorthstarET.Lms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Always require authentication
public class StudentsController : ControllerBase
{
    private readonly StudentService _studentService;
    private readonly EnrollmentService _enrollmentService;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(
        StudentService studentService,
        EnrollmentService enrollmentService,
        ILogger<StudentsController> logger)
    {
        _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
        _enrollmentService = enrollmentService ?? throw new ArgumentNullException(nameof(enrollmentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new student
    /// </summary>
    /// <param name="request">Student creation request</param>
    /// <returns>Created student information</returns>
    [HttpPost]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<StudentDto>> CreateStudent(
        [FromBody] CreateStudentCommand request)
    {
        try
        {
            _logger.LogInformation("Creating student with number: {StudentNumber}", request.Student.StudentNumber);
            
            var result = await _studentService.CreateStudentAsync(request.Student, request.CreatedBy);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Student creation failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Student created successfully: {StudentId}", result.Value.UserId);
            return Created($"/api/v1/students/{result.Value.UserId}", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student with number: {StudentNumber}", request.Student.StudentNumber);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get a specific student by ID
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <returns>Student information with academic history</returns>
    [HttpGet("{userId:guid}")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser,Staff")]
    public async Task<ActionResult<StudentDetailDto>> GetStudent(Guid userId)
    {
        try
        {
            _logger.LogInformation("Retrieving student: {StudentId}", userId);
            
            var query = new GetStudentQuery { UserId = userId };
            var result = await _studentService.GetStudentAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Student not found: {StudentId}", userId);
                return NotFound(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student: {StudentId}", userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// List students with pagination and filtering
    /// </summary>
    /// <param name="query">List students query parameters</param>
    /// <returns>Paginated list of students</returns>
    [HttpGet]
    [Authorize(Roles = "DistrictAdmin,SchoolUser,Staff")]
    public async Task<ActionResult<PagedResult<StudentSummaryDto>>> ListStudents(
        [FromQuery] ListStudentsQuery query)
    {
        try
        {
            _logger.LogInformation("Listing students - Page: {Page}, Size: {Size}", query.Page, query.Size);
            
            var result = await _studentService.ListStudentsAsync(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error listing students: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing students");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Update student information
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="request">Update student request</param>
    /// <returns>Updated student information</returns>
    [HttpPut("{userId:guid}")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<StudentDto>> UpdateStudent(
        Guid userId,
        [FromBody] UpdateStudentDto studentUpdate)
    {
        try
        {
            _logger.LogInformation("Updating student: {StudentId}", userId);
            
            // TODO: Get current user for updatedBy parameter
            var command = new UpdateStudentCommand(userId, studentUpdate, "system");
            var result = await _studentService.UpdateStudentAsync(command);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Student update failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student: {StudentId}", userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Enroll student in a class
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="request">Enrollment request</param>
    /// <returns>Created enrollment information</returns>
    [HttpPost("{userId:guid}/enrollments")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<EnrollmentDto>> EnrollStudent(
        Guid userId,
        [FromBody] CreateEnrollmentCommand request)
    {
        try
        {
            request.StudentId = userId;
            _logger.LogInformation("Enrolling student {StudentId} in class {ClassId}", userId, request.ClassId);
            
            var result = await _enrollmentService.CreateEnrollmentAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Student enrollment failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Student enrolled successfully: {EnrollmentId}", result.Value.Id);
            return Created($"/api/v1/students/{userId}/enrollments/{result.Value.Id}", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling student {StudentId} in class {ClassId}", userId, request.ClassId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Withdraw student from a class
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="enrollmentId">Enrollment ID</param>
    /// <param name="request">Withdrawal request</param>
    /// <returns>Success result</returns>
    [HttpDelete("{userId:guid}/enrollments/{enrollmentId:guid}")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult> WithdrawStudent(
        Guid userId,
        Guid enrollmentId,
        [FromBody] WithdrawEnrollmentCommand request)
    {
        try
        {
            request.EnrollmentId = enrollmentId;
            request.StudentId = userId;
            _logger.LogInformation("Withdrawing student {StudentId} from enrollment {EnrollmentId}", userId, enrollmentId);
            
            var result = await _enrollmentService.WithdrawEnrollmentAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Student withdrawal failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Student withdrawn successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error withdrawing student {StudentId} from enrollment {EnrollmentId}", userId, enrollmentId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Transfer student between schools
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="request">Transfer request</param>
    /// <returns>Transfer result</returns>
    [HttpPost("{userId:guid}/transfer")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult> TransferStudent(
        Guid userId,
        [FromBody] TransferStudentCommand request)
    {
        try
        {
            request.StudentId = userId;
            _logger.LogInformation("Transferring student {StudentId} from school {FromSchoolId} to {ToSchoolId}", 
                userId, request.FromSchoolId, request.ToSchoolId);
            
            var result = await _enrollmentService.TransferStudentAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Student transfer failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Student transferred successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring student {StudentId}", userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Promote student to next grade level
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="request">Promotion request</param>
    /// <returns>Promotion result</returns>
    [HttpPost("{userId:guid}/promote")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult> PromoteStudent(
        Guid userId,
        [FromBody] PromoteStudentCommand request)
    {
        try
        {
            request.StudentId = userId;
            _logger.LogInformation("Promoting student {StudentId} from {FromGrade} to {ToGrade}", 
                userId, request.FromGradeLevel, request.ToGradeLevel);
            
            var result = await _studentService.PromoteStudentAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Student promotion failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Student promoted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting student {StudentId}", userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Preview bulk rollover for grade progression
    /// </summary>
    /// <param name="request">Bulk rollover preview request</param>
    /// <returns>Preview results with affected students</returns>
    [HttpPost("bulk-rollover/preview")]
    [Authorize(Roles = "DistrictAdmin")]
    public async Task<ActionResult<BulkRolloverPreviewDto>> PreviewBulkRollover(
        [FromBody] PreviewBulkRolloverCommand request)
    {
        try
        {
            _logger.LogInformation("Previewing bulk rollover from {FromYear} to {ToYear}", 
                request.FromSchoolYear, request.ToSchoolYear);
            
            var result = await _studentService.PreviewBulkRolloverAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Bulk rollover preview failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing bulk rollover");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Execute bulk rollover for grade progression
    /// </summary>
    /// <param name="request">Bulk rollover execution request</param>
    /// <returns>Execution job information</returns>
    [HttpPost("bulk-rollover/execute")]
    [Authorize(Roles = "DistrictAdmin")]
    public async Task<ActionResult<BulkOperationJobDto>> ExecuteBulkRollover(
        [FromBody] ExecuteBulkRolloverCommand request)
    {
        try
        {
            _logger.LogInformation("Executing bulk rollover with preview {PreviewId}", request.PreviewId);
            
            var result = await _studentService.ExecuteBulkRolloverAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Bulk rollover execution failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Accepted(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing bulk rollover");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Add guardian relationship to student
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="request">Guardian relationship request</param>
    /// <returns>Created guardian relationship</returns>
    [HttpPost("{userId:guid}/guardians")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<GuardianRelationshipDto>> AddGuardianRelationship(
        Guid userId,
        [FromBody] CreateGuardianRelationshipCommand request)
    {
        try
        {
            request.StudentId = userId;
            _logger.LogInformation("Adding guardian relationship for student {StudentId}", userId);
            
            var result = await _studentService.AddGuardianRelationshipAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Guardian relationship creation failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Created($"/api/v1/students/{userId}/guardians/{result.Value.Id}", result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding guardian relationship for student {StudentId}", userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Update guardian relationship
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="relationshipId">Guardian relationship ID</param>
    /// <param name="request">Update guardian relationship request</param>
    /// <returns>Updated guardian relationship</returns>
    [HttpPut("{userId:guid}/guardians/{relationshipId:guid}")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<GuardianRelationshipDto>> UpdateGuardianRelationship(
        Guid userId,
        Guid relationshipId,
        [FromBody] UpdateGuardianRelationshipCommand request)
    {
        try
        {
            request.StudentId = userId;
            request.RelationshipId = relationshipId;
            _logger.LogInformation("Updating guardian relationship {RelationshipId} for student {StudentId}", 
                relationshipId, userId);
            
            var result = await _studentService.UpdateGuardianRelationshipAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Guardian relationship update failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating guardian relationship {RelationshipId} for student {StudentId}", 
                relationshipId, userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Bulk import students from CSV
    /// </summary>
    /// <param name="request">Bulk import request with file</param>
    /// <returns>Import job information</returns>
    [HttpPost("bulk-import")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult<BulkOperationJobDto>> BulkImportStudents(
        [FromForm] BulkImportStudentsCommand request)
    {
        try
        {
            _logger.LogInformation("Starting bulk import of students");
            
            var result = await _studentService.BulkImportStudentsAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Bulk import failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Accepted(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk student import");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Map student to external identity
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="request">Identity mapping request</param>
    /// <returns>Success result</returns>
    [HttpPost("{userId:guid}/identity-mapping")]
    [Authorize(Roles = "DistrictAdmin")]
    public async Task<ActionResult> MapIdentity(
        Guid userId,
        [FromBody] MapStudentIdentityCommand request)
    {
        try
        {
            request.UserId = userId;
            _logger.LogInformation("Mapping identity for student {StudentId}", userId);
            
            var result = await _studentService.MapIdentityAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Identity mapping failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = "Identity mapped successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping identity for student {StudentId}", userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Process student lifecycle event
    /// </summary>
    /// <param name="userId">Student user ID</param>
    /// <param name="request">Lifecycle event request</param>
    /// <returns>Success result</returns>
    [HttpPost("{userId:guid}/lifecycle")]
    [Authorize(Roles = "DistrictAdmin,SchoolUser")]
    public async Task<ActionResult> ProcessLifecycleEvent(
        Guid userId,
        [FromBody] StudentLifecycleEventCommand request)
    {
        try
        {
            request.StudentId = userId;
            _logger.LogInformation("Processing lifecycle event {EventType} for student {StudentId}", 
                request.EventType, userId);
            
            var result = await _studentService.ProcessLifecycleEventAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Lifecycle event processing failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { message = $"Lifecycle event {request.EventType} processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing lifecycle event {EventType} for student {StudentId}", 
                request.EventType, userId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}