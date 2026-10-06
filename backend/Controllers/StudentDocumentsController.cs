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
    [ApiController]
    [Route("api/student/documents")]
    [Authorize(Roles = "ROLE_STUDENT")]
    [Produces("application/json")]
    public sealed class StudentDocumentsController : ControllerBase
    {
        private const long MaxFileSize = 10 * 1024 * 1024;
        private const long MultipartOverhead = 256 * 1024;
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public StudentDocumentsController(AppDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyDocuments()
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (studentId == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy hồ sơ sinh viên gắn với tài khoản này."));

            var documents = await _db.StudentDocuments.AsNoTracking()
                .Where(document => document.StudentId == studentId.Value)
                .OrderByDescending(document => document.UploadedAt)
                .Select(document => new StudentDocumentResponse(
                    document.Id,
                    document.DocumentType,
                    document.OriginalFileName,
                    document.ContentType,
                    document.SizeBytes,
                    document.UploadedAt))
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<StudentDocumentResponse>>.Ok(documents));
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxFileSize + MultipartOverhead)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize + MultipartOverhead)]
        public async Task<IActionResult> Upload([FromForm] StudentDocumentUploadRequest request)
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (studentId == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy hồ sơ sinh viên gắn với tài khoản này."));

            var documentType = NormalizeDocumentType(request.DocumentType);
            if (documentType == null)
                return BadRequest(ApiResponse.Fail("Loại tài liệu phải là CV hoặc INTERNSHIP_LETTER."));

            var inspected = await InspectFileAsync(request.File);
            if (!inspected.IsValid)
                return BadRequest(ApiResponse.Fail(inspected.Error!));

            var now = DateTime.UtcNow;
            var document = new StudentDocument
            {
                StudentId = studentId.Value,
                DocumentType = documentType,
                OriginalFileName = inspected.FileName!,
                StoredFileName = $"db-{Guid.NewGuid():N}{Path.GetExtension(inspected.FileName)}",
                ContentType = inspected.ContentType!,
                SizeBytes = inspected.Content!.LongLength,
                FileContent = inspected.Content,
                UploadedAt = now
            };

            _db.StudentDocuments.Add(document);
            await _db.SaveChangesAsync();

            var response = ToResponse(document);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<StudentDocumentResponse>.Created(response, "Tải tài liệu lên thành công."));
        }

        [HttpPut("{documentId:int}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxFileSize + MultipartOverhead)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize + MultipartOverhead)]
        public async Task<IActionResult> Replace(int documentId, [FromForm] StudentDocumentReplaceRequest request)
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (studentId == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy hồ sơ sinh viên gắn với tài khoản này."));

            var document = await _db.StudentDocuments.SingleOrDefaultAsync(item =>
                item.Id == documentId && item.StudentId == studentId.Value);
            if (document == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy tài liệu của sinh viên này."));

            var inspected = await InspectFileAsync(request.File);
            if (!inspected.IsValid)
                return BadRequest(ApiResponse.Fail(inspected.Error!));

            document.OriginalFileName = inspected.FileName!;
            document.ContentType = inspected.ContentType!;
            document.SizeBytes = inspected.Content!.LongLength;
            document.FileContent = inspected.Content;
            document.UploadedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<StudentDocumentResponse>.Ok(ToResponse(document), "Đã thay thế tài liệu."));
        }

        [HttpGet("{documentId:int}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Download(int documentId)
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (studentId == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy hồ sơ sinh viên gắn với tài khoản này."));

            var document = await _db.StudentDocuments.AsNoTracking().SingleOrDefaultAsync(item =>
                item.Id == documentId && item.StudentId == studentId.Value);
            if (document == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy tài liệu của sinh viên này."));

            if (document.FileContent is { Length: > 0 })
                return File(document.FileContent, document.ContentType, document.OriginalFileName);

            var legacyPath = GetLegacyDocumentPath(document);
            if (legacyPath != null && System.IO.File.Exists(legacyPath))
                return PhysicalFile(legacyPath, document.ContentType, document.OriginalFileName);

            return NotFound(ApiResponse.Fail("Nội dung tệp cũ không còn trên máy chủ."));
        }

        [HttpDelete("{documentId:int}")]
        public async Task<IActionResult> Delete(int documentId)
        {
            var studentId = await GetCurrentStudentIdAsync();
            if (studentId == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy hồ sơ sinh viên gắn với tài khoản này."));

            var document = await _db.StudentDocuments.SingleOrDefaultAsync(item =>
                item.Id == documentId && item.StudentId == studentId.Value);
            if (document == null)
                return NotFound(ApiResponse.Fail("Không tìm thấy tài liệu của sinh viên này."));

            _db.StudentDocuments.Remove(document);
            await _db.SaveChangesAsync();
            return Ok(ApiResponse.Ok("Đã xóa tài liệu."));
        }

        private async Task<int?> GetCurrentStudentIdAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            if (!int.TryParse(userIdClaim, out var userId))
                return null;

            return await _db.Students.AsNoTracking()
                .Where(student => student.UserId == userId)
                .Select(student => (int?)student.Id)
                .SingleOrDefaultAsync();
        }

        private static string? NormalizeDocumentType(string? value)
        {
            var normalized = value?.Trim().ToUpperInvariant();
            return normalized is "CV" or "INTERNSHIP_LETTER" ? normalized : null;
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

            var isPdf = extension == ".pdf" && StartsWith(content, "%PDF-"u8);
            var isLegacyWord = extension == ".doc" && StartsWith(content, new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });
            var isDocx = extension == ".docx" && StartsWith(content, new byte[] { 0x50, 0x4B, 0x03, 0x04 });

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

        private static bool StartsWith(byte[] content, ReadOnlySpan<byte> signature) =>
            content.AsSpan().StartsWith(signature);

        private static StudentDocumentResponse ToResponse(StudentDocument document) => new(
            document.Id,
            document.DocumentType,
            document.OriginalFileName,
            document.ContentType,
            document.SizeBytes,
            document.UploadedAt);

        private string? GetLegacyDocumentPath(StudentDocument document)
        {
            if (string.IsNullOrWhiteSpace(document.StoredFileName))
                return null;

            var safeStoredName = Path.GetFileName(document.StoredFileName);
            if (!string.Equals(safeStoredName, document.StoredFileName, StringComparison.Ordinal))
                return null;

            return Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "private-uploads",
                document.StudentId.ToString(),
                safeStoredName);
        }

        private sealed record FileInspection(bool IsValid, string? Error, string? FileName, string? ContentType, byte[]? Content)
        {
            public static FileInspection Invalid(string error) => new(false, error, null, null, null);
            public static FileInspection Valid(string fileName, string contentType, byte[] content) =>
                new(true, null, fileName, contentType, content);
        }
    }
}
