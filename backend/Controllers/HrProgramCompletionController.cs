using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers;

[ApiController]
[Route("api/hr/program-completion")]
[Authorize(Roles = "ROLE_HR")]
public sealed class HrProgramCompletionController : ControllerBase
{
    private readonly IProgramCompletionService _service;

    public HrProgramCompletionController(IProgramCompletionService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ProgramCompletionDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int? programId,
        CancellationToken cancellationToken)
    {
        var dashboard = await _service.GetDashboardAsync(programId, cancellationToken);
        return Ok(ApiResponse<ProgramCompletionDashboardDto>.Ok(dashboard));
    }

    [HttpGet("students/{studentId:int}/programs/{programId:int}/history")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StudentProgramAuditEventDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        int studentId,
        int programId,
        CancellationToken cancellationToken)
    {
        var history = await _service.GetHistoryAsync(studentId, programId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StudentProgramAuditEventDto>>.Ok(history));
    }

    [HttpPut("students/{studentId:int}/participation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateParticipation(
        int studentId,
        [FromBody] UpdateProgramParticipationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized(ApiResponse.Fail("Không xác định được tài khoản HR hiện tại."));

        await _service.UpdateParticipationAsync(studentId, request, actorUserId, cancellationToken);
        return Ok(ApiResponse.Ok("Đã cập nhật trạng thái tham gia và lưu lịch sử."));
    }

    [HttpPost("students/{studentId:int}/programs/{programId:int}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmCompletion(
        int studentId,
        int programId,
        [FromBody] ProgramCompletionActionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized(ApiResponse.Fail("Không xác định được tài khoản HR hiện tại."));

        await _service.ConfirmCompletionAsync(
            studentId,
            programId,
            request.Reason,
            actorUserId,
            cancellationToken);
        return Ok(ApiResponse.Ok("Đã xác nhận hoàn thành chương trình."));
    }

    [HttpPost("students/{studentId:int}/programs/{programId:int}/revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeCompletion(
        int studentId,
        int programId,
        [FromBody] ProgramCompletionActionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized(ApiResponse.Fail("Không xác định được tài khoản HR hiện tại."));

        await _service.RevokeCompletionAsync(
            studentId,
            programId,
            request.Reason,
            actorUserId,
            cancellationToken);
        return Ok(ApiResponse.Ok("Đã thu hồi xác nhận hoàn thành."));
    }

    private bool TryGetActorUserId(out int actorUserId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
        return int.TryParse(value, out actorUserId);
    }
}
