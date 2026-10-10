using System.Security.Claims;
using System.Security.Cryptography;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Controllers
{
    // US09: HR tải lên, cập nhật và xem hợp đồng thực tập (bản giấy đã số hóa) của sinh viên.
    [ApiController]
    [Route("api/hr/contracts")]
    [Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]
    [Produces("application/json")]
    public sealed class HrContractsController : ControllerBase
    {
        private const long MaxFileSize = 10 * 1024 * 1024;
        private const long MultipartOverhead = 256 * 1024;
        private readonly AppDbContext _db;

        public HrContractsController(AppDbContext db)
        {
            _db = db;
        }

        // Danh sách sinh viên kèm thông tin hợp đồng (nếu có).
        [HttpGet]
        public async Task<IActionResult> GetContracts([FromQuery] string? search)
        {
            var students = _db.Students.AsNoTracking().AsQueryable();
            var keyword = search?.Trim();
            if (!string.IsNullOrEmpty(keyword))
            {
                students = students.Where(s => s.FullName.Contains(keyword) || s.StudentCode.Contains(keyword));
            }

            var rows = await students
                .OrderBy(s => s.FullName)
                .Select(s => new HrContractResponse(
                    s.Id,
                    s.StudentCode,
                    s.FullName,
                    s.Program != null ? s.Program.Name : null,
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => (int?)c.Id).FirstOrDefault(),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.ContractNumber).FirstOrDefault(),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.Status).FirstOrDefault(),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.FileName).FirstOrDefault(),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.SizeBytes).FirstOrDefault(),
                    _db.InternshipContracts.Any(c => c.StudentId == s.Id && c.FileContent != null),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.FileUploadedAt).FirstOrDefault(),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.ConfirmedAt).FirstOrDefault()))
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<HrContractResponse>>.Ok(rows));
        }

        // Tải lên hợp đồng lần đầu hoặc thay thế tệp hợp đồng chưa được sinh viên xác nhận.
        [HttpPut("students/{studentId:int}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxFileSize + MultipartOverhead)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize + MultipartOverhead)]
        public async Task<IActionResult> Upload(int studentId, [FromForm] HrContractUploadRequest request)
        {
            if (!await _db.Students.AnyAsync(s => s.Id == studentId))
                return NotFound(ApiResponse.Fail("Không tìm thấy sinh viên."));

            var inspected = await InspectFileAsync(request.File);
            if (!inspected.IsValid)
                return BadRequest(ApiResponse.Fail(inspected.Error!));

            var hash = Convert.ToHexString(SHA256.HashData(inspected.Content!)).ToLowerInvariant();
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var existing = await _db.InternshipContracts.AsNoTracking()
                .Where(c => c.StudentId == studentId)
                .Select(c => new { c.Id, c.Status, c.ContentHash })
                .SingleOrDefaultAsync();

            if (existing == null)
            {
                var contract = new InternshipContract
                {
                    StudentId = studentId,
                    ContractNumber = $"HD-{now.Year}-{studentId:D4}",
                    FileName = inspected.FileName,
                    ContentType = inspected.ContentType,
                    SizeBytes = inspected.Content!.LongLength,
                    FileContent = inspected.Content,
                    ContentHash = hash,
                    UploadedByUserId = userId,
                    FileUploadedAt = now,
                    Status = ContractStatus.PendingConfirmation,
                    IssuedAt = now
                };
                _db.InternshipContracts.Add(contract);
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Conflict(ApiResponse.Fail("Hợp đồng của sinh viên vừa được tạo bởi người khác. Vui lòng tải lại."));
                }

                return StatusCode(StatusCodes.Status201Created,
                    ApiResponse<HrContractResponse>.Created(await QueryRow(studentId), "Tải hợp đồng lên thành công."));
            }

            if (existing.Status == ContractStatus.Confirmed)
                return Conflict(ApiResponse.Fail("Hợp đồng đã được sinh viên xác nhận nên không thể thay thế."));
            if (string.Equals(existing.ContentHash, hash, StringComparison.OrdinalIgnoreCase))
                return Conflict(ApiResponse.Fail("Tệp trùng với hợp đồng hiện tại của sinh viên."));

            // Cập nhật có điều kiện: nếu sinh viên vừa xác nhận thì không ghi đè.
            var updated = await _db.InternshipContracts
                .Where(c => c.Id == existing.Id && c.Status != ContractStatus.Confirmed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.FileName, inspected.FileName)
                    .SetProperty(c => c.ContentType, inspected.ContentType)
                    .SetProperty(c => c.SizeBytes, inspected.Content!.LongLength)
                    .SetProperty(c => c.FileContent, inspected.Content)
                    .SetProperty(c => c.ContentHash, hash)
                    .SetProperty(c => c.UploadedByUserId, userId)
                    .SetProperty(c => c.FileUploadedAt, now)
                    .SetProperty(c => c.Status, ContractStatus.PendingConfirmation));

            if (updated == 0)
                return Conflict(ApiResponse.Fail("Hợp đồng vừa được sinh viên xác nhận nên không thể thay thế."));

            return Ok(ApiResponse<HrContractResponse>.Ok(await QueryRow(studentId), "Đã cập nhật hợp đồng."));
        }

        [HttpGet("students/{studentId:int}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Download(int studentId)
        {
            var file = await _db.InternshipContracts.AsNoTracking()
                .Where(c => c.StudentId == studentId)
                .Select(c => new { c.FileName, c.ContentType, c.FileContent })
                .SingleOrDefaultAsync();

            if (file?.FileContent == null || file.FileContent.Length == 0)
                return NotFound(ApiResponse.Fail("Sinh viên này chưa có tệp hợp đồng."));

            return File(
                file.FileContent,
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType,
                file.FileName ?? "hop-dong-thuc-tap.pdf");
        }

        private async Task<HrContractResponse> QueryRow(int studentId) =>
            await _db.Students.AsNoTracking()
                .Where(s => s.Id == studentId)
                .Select(s => new HrContractResponse(
                    s.Id,
                    s.StudentCode,
                    s.FullName,
                    s.Program != null ? s.Program.Name : null,
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => (int?)c.Id).FirstOrDefault(),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.ContractNumber).FirstOrDefault(),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.Status).FirstOrDefault(),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.FileName).FirstOrDefault(),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.SizeBytes).FirstOrDefault(),
                    _db.InternshipContracts.Any(c => c.StudentId == s.Id && c.FileContent != null),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.FileUploadedAt).FirstOrDefault(),
                    _db.InternshipContracts.Where(c => c.StudentId == s.Id).Select(c => c.ConfirmedAt).FirstOrDefault()))
                .SingleAsync();

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            return int.TryParse(claim, out var userId) ? userId : null;
        }

        private static async Task<FileInspection> InspectFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return FileInspection.Invalid("Vui lòng chọn tệp không rỗng.");
            if (file.Length > MaxFileSize)
                return FileInspection.Invalid("Dung lượng tệp tối đa là 10 MB.");

            var safeName = Path.GetFileName(file.FileName.Replace('\\', '/'));
            safeName = new string(safeName.Where(character => !char.IsControl(character)).ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 255)
                return FileInspection.Invalid("Tên tệp không hợp lệ hoặc dài hơn 255 ký tự.");

            var extension = Path.GetExtension(safeName).ToLowerInvariant();
            var content = new byte[(int)file.Length];
            await using (var stream = file.OpenReadStream())
            {
                var offset = 0;
                while (offset < content.Length)
                {
                    var read = await stream.ReadAsync(content.AsMemory(offset));
                    if (read == 0) break;
                    offset += read;
                }
                if (offset != content.Length)
                    return FileInspection.Invalid("Không thể đọc đầy đủ nội dung tệp.");
            }

            var isPdf = extension == ".pdf" && content.AsSpan().StartsWith("%PDF-"u8);
            var isLegacyWord = extension == ".doc"
                && content.AsSpan().StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });
            var isDocx = extension == ".docx"
                && content.AsSpan().StartsWith(new byte[] { 0x50, 0x4B, 0x03, 0x04 });

            if (!isPdf && !isLegacyWord && !isDocx)
                return FileInspection.Invalid("Chỉ chấp nhận tệp PDF, DOC hoặc DOCX đúng định dạng.");

            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                _ => "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };

            return FileInspection.Valid(safeName, contentType, content);
        }

        private sealed record FileInspection(bool IsValid, string? Error, string? FileName, string? ContentType, byte[]? Content)
        {
            public static FileInspection Invalid(string error) => new(false, error, null, null, null);
            public static FileInspection Valid(string fileName, string contentType, byte[] content) =>
                new(true, null, fileName, contentType, content);
        }
    }
}
