using System.Security.Claims;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Controllers
{
    // US10: sinh viên xem và xác nhận hợp đồng thực tập của chính mình.
    [ApiController]
    [Route("api/student/contract")]
    [Authorize(Roles = "ROLE_STUDENT")]
    [Produces("application/json")]
    public sealed class StudentContractController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StudentContractController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyContract()
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (studentId == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy hồ sơ sinh viên gắn với tài khoản này."));

            var contract = await QueryResponse(studentId.Value);
            if (contract == null)
                return NotFound(ApiResponse.Fail("Bạn chưa có hợp đồng thực tập."));

            return Ok(ApiResponse<StudentContractResponse>.Ok(contract));
        }

        [HttpPost("{contractId:int}/confirm")]
        public async Task<IActionResult> Confirm(int contractId)
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (studentId == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy hồ sơ sinh viên gắn với tài khoản này."));

            var userId = GetCurrentUserId();
            var current = await _db.InternshipContracts.AsNoTracking()
                .Where(c => c.Id == contractId && c.StudentId == studentId.Value)
                .Select(c => new { c.Status })
                .SingleOrDefaultAsync();

            // Hợp đồng của người khác được coi như không tồn tại để không lộ thông tin.
            if (current == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy hợp đồng của sinh viên này."));
            if (current.Status == ContractStatus.Confirmed)
                return Conflict(ApiResponse.Fail("Hợp đồng đã được xác nhận trước đó."));
            if (current.Status == ContractStatus.Cancelled)
                return BadRequest(ApiResponse.Fail("Hợp đồng đã bị hủy nên không thể xác nhận."));

            // Cập nhật có điều kiện trạng thái để hai yêu cầu đồng thời không cùng xác nhận.
            var now = DateTime.UtcNow;
            var updated = await _db.InternshipContracts
                .Where(c => c.Id == contractId
                    && c.StudentId == studentId.Value
                    && c.Status == ContractStatus.PendingConfirmation)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Status, ContractStatus.Confirmed)
                    .SetProperty(c => c.ConfirmedAt, now)
                    .SetProperty(c => c.ConfirmedByUserId, userId));

            if (updated == 0)
                return Conflict(ApiResponse.Fail("Trạng thái hợp đồng vừa thay đổi. Vui lòng tải lại và thử lại."));

            var contract = await QueryResponse(studentId.Value);
            return Ok(ApiResponse<StudentContractResponse>.Ok(contract!, "Xác nhận hợp đồng thành công."));
        }

        [HttpGet("{contractId:int}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Download(int contractId)
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (studentId == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy hồ sơ sinh viên gắn với tài khoản này."));

            var file = await _db.InternshipContracts.AsNoTracking()
                .Where(c => c.Id == contractId && c.StudentId == studentId.Value)
                .Select(c => new { c.FileName, c.ContentType, c.FileContent })
                .SingleOrDefaultAsync();

            if (file?.FileContent == null || file.FileContent.Length == 0)
                return NotFound(ApiResponse.Fail("Hợp đồng chưa có tệp đính kèm."));

            return File(
                file.FileContent,
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType,
                file.FileName ?? "hop-dong-thuc-tap.pdf");
        }

        private Task<StudentContractResponse?> QueryResponse(int studentId) =>
            _db.InternshipContracts.AsNoTracking()
                .Where(c => c.StudentId == studentId)
                .Select(c => new StudentContractResponse(
                    c.Id,
                    c.ContractNumber,
                    c.Status,
                    c.Student.Program != null ? c.Student.Program.Name : null,
                    c.Student.Program != null ? c.Student.Program.StartDate : null,
                    c.Student.Program != null ? c.Student.Program.EndDate : null,
                    c.Student.Mentor != null ? c.Student.Mentor.FullName : null,
                    c.FileName,
                    c.FileContent != null,
                    c.IssuedAt,
                    c.ConfirmedAt))
                .SingleOrDefaultAsync();

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(claim, out var userId) ? userId : null;
        }

        private async Task<int?> GetCurrentStudentIdAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return null;

            return await _db.Students.AsNoTracking()
                .Where(student => student.UserId == userId.Value)
                .Select(student => (int?)student.Id)
                .SingleOrDefaultAsync();
        }
    }
}
