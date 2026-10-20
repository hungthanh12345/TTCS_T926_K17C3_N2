using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Workforce;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers;

public abstract class WorkforceControllerBase : ControllerBase
{
    protected int? CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}

[ApiController]
[Route("api/student/attendance")]
[Authorize(Roles = "ROLE_STUDENT")]
public sealed class StudentAttendanceController : WorkforceControllerBase
{
    private readonly IAttendanceService _attendance;
    public StudentAttendanceController(IAttendanceService attendance) => _attendance = attendance;

    [HttpGet("today")]
    public async Task<IActionResult> Today(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        return Ok(ApiResponse<AttendanceDto?>.Ok(await _attendance.GetTodayAsync(userId, cancellationToken)));
    }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] AttendanceCheckInRequestDto request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        var result = await _attendance.CheckInAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<AttendanceDto>.Created(result, "Check-in recorded."));
    }

    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        return Ok(ApiResponse<AttendanceDto>.Ok(await _attendance.CheckOutAsync(userId, cancellationToken), "Check-out recorded."));
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        return Ok(ApiResponse<PagedResult<AttendanceDto>>.Ok(await _attendance.GetHistoryAsync(
            userId, fromDate, toDate, AttendanceService.NormalizePage(page), AttendanceService.NormalizePageSize(pageSize), cancellationToken)));
    }
}

[ApiController]
[Route("api/hr/attendance-reports")]
[Authorize(Roles = "ROLE_HR")]
public sealed class HrAttendanceReportsController : ControllerBase
{
    private readonly IAttendanceReportService _reports;
    public HrAttendanceReportsController(IAttendanceReportService reports) => _reports = reports;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] int? studentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (studentId is <= 0) return BadRequest(ApiResponse.Fail("studentId must be positive."));
        var result = await _reports.GetAsync(fromDate, toDate, studentId,
            AttendanceService.NormalizePage(page), AttendanceService.NormalizePageSize(pageSize), cancellationToken);
        return Ok(ApiResponse<AttendanceReportResponseDto>.Ok(result));
    }
}

[ApiController]
[Route("api/hr/work-schedules")]
[Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]
public sealed class HrWorkSchedulesController : ControllerBase
{
    private readonly IWorkScheduleService _schedules;
    public HrWorkSchedulesController(IWorkScheduleService schedules) => _schedules = schedules;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<WorkScheduleDto>>.Ok(await _schedules.GetAllAsync(cancellationToken)));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WorkScheduleDto>.Ok(await _schedules.GetAsync(id, cancellationToken)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkScheduleRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _schedules.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<WorkScheduleDto>.Created(result));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] WorkScheduleRequestDto request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<WorkScheduleDto>.Ok(await _schedules.UpdateAsync(id, request, cancellationToken)));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _schedules.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Work schedule deleted."));
    }
}

[ApiController]
[Route("api/hr/student-groups")]
[Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]
public sealed class HrStudentGroupsController : ControllerBase
{
    private readonly IWorkScheduleService _schedules;
    public HrStudentGroupsController(IWorkScheduleService schedules) => _schedules = schedules;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<StudentGroupDto>>.Ok(await _schedules.GetGroupsAsync(cancellationToken)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StudentGroupRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _schedules.CreateGroupAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<StudentGroupDto>.Created(result));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StudentGroupRequestDto request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<StudentGroupDto>.Ok(await _schedules.UpdateGroupAsync(id, request, cancellationToken)));

    [HttpPut("{groupId:int}/members/{studentId:int}")]
    public async Task<IActionResult> Assign(int groupId, int studentId, CancellationToken cancellationToken)
    {
        await _schedules.AssignStudentAsync(groupId, studentId, cancellationToken);
        return Ok(ApiResponse.Ok("Student assigned to group."));
    }

    [HttpDelete("{groupId:int}/members/{studentId:int}")]
    public async Task<IActionResult> Remove(int groupId, int studentId, CancellationToken cancellationToken)
    {
        await _schedules.RemoveStudentAsync(groupId, studentId, cancellationToken);
        return Ok(ApiResponse.Ok("Student removed from group."));
    }
}

[ApiController]
[Route("api/student/leave-requests")]
[Authorize(Roles = "ROLE_STUDENT")]
public sealed class StudentLeaveRequestsController : WorkforceControllerBase
{
    private readonly ILeaveRequestService _leave;
    public StudentLeaveRequestsController(ILeaveRequestService leave) => _leave = leave;

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        return Ok(ApiResponse<IReadOnlyList<LeaveRequestDto>>.Ok(await _leave.GetMineAsync(userId, cancellationToken)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LeaveRequestCreateDto request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        var result = await _leave.CreateAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<LeaveRequestDto>.Created(result));
    }
}

[ApiController]
[Route("api/hr/leave-requests")]
[Authorize(Roles = "ROLE_HR")]
public sealed class HrLeaveRequestsController : WorkforceControllerBase
{
    private readonly ILeaveRequestService _leave;
    public HrLeaveRequestsController(ILeaveRequestService leave) => _leave = leave;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? studentId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (studentId is <= 0) return BadRequest(ApiResponse.Fail("studentId must be positive."));
        return Ok(ApiResponse<PagedResult<LeaveRequestDto>>.Ok(await _leave.GetAllAsync(status, studentId,
            AttendanceService.NormalizePage(page), AttendanceService.NormalizePageSize(pageSize), cancellationToken)));
    }

    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> Review(int id, [FromBody] LeaveRequestReviewDto request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        return Ok(ApiResponse<LeaveRequestDto>.Ok(await _leave.ReviewAsync(userId, id, request, cancellationToken)));
    }
}

[ApiController]
[Route("api/hr/allowances")]
[Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]
public sealed class HrAllowancesController : WorkforceControllerBase
{
    private readonly IAllowanceService _allowances;
    public HrAllowancesController(IAllowanceService allowances) => _allowances = allowances;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? studentId, [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (studentId is <= 0) return BadRequest(ApiResponse.Fail("studentId must be positive."));
        return Ok(ApiResponse<PagedResult<AllowanceDto>>.Ok(await _allowances.GetAllAsync(studentId, fromDate, toDate,
            AttendanceService.NormalizePage(page), AttendanceService.NormalizePageSize(pageSize), cancellationToken)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AllowanceDto>.Ok(await _allowances.GetAsync(id, cancellationToken)));

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] int studentId, [FromBody] AllowanceRequestDto request, CancellationToken cancellationToken)
    {
        if (studentId <= 0) return BadRequest(ApiResponse.Fail("studentId is required."));
        var result = await _allowances.CreateAsync(studentId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<AllowanceDto>.Created(result));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AllowanceRequestDto request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<AllowanceDto>.Ok(await _allowances.UpdateAsync(id, request, cancellationToken)));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        await _allowances.CancelAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Allowance cancelled; payment history was retained."));
    }

    [HttpPost("{id:int}/payments")]
    public async Task<IActionResult> RecordPayment(int id, [FromBody] AllowancePaymentRequestDto request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        var result = await _allowances.RecordPaymentAsync(userId, id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<AllowancePaymentDto>.Created(result));
    }
}

[ApiController]
[Route("api/student/allowances/history")]
[Authorize(Roles = "ROLE_STUDENT")]
public sealed class StudentAllowanceHistoryController : WorkforceControllerBase
{
    private readonly IAllowanceService _allowances;
    public StudentAllowanceHistoryController(IAllowanceService allowances) => _allowances = allowances;

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        return Ok(ApiResponse<AllowanceHistoryResponseDto>.Ok(await _allowances.GetMineAsync(userId, fromDate, toDate,
            AttendanceService.NormalizePage(page), AttendanceService.NormalizePageSize(pageSize), cancellationToken)));
    }
}

[ApiController]
[Route("api/student/support-requests")]
[Authorize(Roles = "ROLE_STUDENT")]
public sealed class StudentSupportRequestsController : WorkforceControllerBase
{
    private readonly ISupportRequestService _support;
    public StudentSupportRequestsController(ISupportRequestService support) => _support = support;

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        return Ok(ApiResponse<IReadOnlyList<SupportRequestDto>>.Ok(await _support.GetMineAsync(userId, cancellationToken)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetMineById(int id, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        return Ok(ApiResponse<SupportRequestDto>.Ok(await _support.GetMineByIdAsync(userId, id, cancellationToken)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SupportRequestCreateDto request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        var result = await _support.CreateAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<SupportRequestDto>.Created(result));
    }
}

[ApiController]
[Route("api/hr/support-requests")]
[Authorize(Roles = "ROLE_HR")]
public sealed class HrSupportRequestsController : WorkforceControllerBase
{
    private readonly ISupportRequestService _support;
    public HrSupportRequestsController(ISupportRequestService support) => _support = support;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(ApiResponse<PagedResult<SupportRequestDto>>.Ok(await _support.GetAllAsync(status,
            AttendanceService.NormalizePage(page), AttendanceService.NormalizePageSize(pageSize), cancellationToken)));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<SupportRequestDto>.Ok(await _support.GetByIdAsync(id, cancellationToken)));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SupportRequestUpdateDto request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized(ApiResponse.Fail("Invalid user identity."));
        return Ok(ApiResponse<SupportRequestDto>.Ok(await _support.UpdateAsync(userId, id, request, cancellationToken)));
    }
}
