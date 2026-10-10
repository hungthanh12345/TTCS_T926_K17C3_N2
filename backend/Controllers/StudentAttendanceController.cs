using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.DTOs.Attendance;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/student/attendance")]
    [Authorize(Roles = "ROLE_STUDENT")]
    [Produces("application/json")]
    public sealed class StudentAttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public StudentAttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        /// <summary>
        /// Thực tập sinh thực hiện Check-in trong ngày làm việc
        /// </summary>
        [HttpPost("check-in")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequestDto? request)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse.Fail("Xác thực người dùng không hợp lệ."));
            }

            var result = await _attendanceService.CheckInAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<AttendanceResponseDto>.Created(result, "Check-in thành công. Chúc bạn một ngày làm việc hiệu quả!"));
        }

        /// <summary>
        /// Thực tập sinh thực hiện Check-out kết thúc buổi làm việc
        /// </summary>
        [HttpPost("check-out")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CheckOut([FromBody] CheckOutRequestDto? request)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse.Fail("Xác thực người dùng không hợp lệ."));
            }

            var result = await _attendanceService.CheckOutAsync(userId, request);
            return Ok(ApiResponse<AttendanceResponseDto>.Ok(result, "Check-out thành công. Thời gian làm việc đã được ghi nhận!"));
        }

        /// <summary>
        /// Lấy trạng thái chấm công của ngày hôm nay
        /// </summary>
        [HttpGet("today")]
        [ProducesResponseType(typeof(ApiResponse<TodayAttendanceStatusDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTodayStatus()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse.Fail("Xác thực người dùng không hợp lệ."));
            }

            var result = await _attendanceService.GetTodayStatusAsync(userId);
            return Ok(ApiResponse<TodayAttendanceStatusDto>.Ok(result));
        }

        /// <summary>
        /// Lấy toàn bộ lịch sử và tổng quan chấm công của sinh viên
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory(
            [FromQuery] DateOnly? startDate,
            [FromQuery] DateOnly? endDate,
            [FromQuery] string? status)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(ApiResponse.Fail("Xác thực người dùng không hợp lệ."));
            }

            var result = await _attendanceService.GetOwnHistoryAsync(userId, startDate, endDate, status);
            return Ok(ApiResponse<AttendanceSummaryDto>.Ok(result));
        }

        private bool TryGetUserId(out int userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(value, out userId);
        }
    }
}
