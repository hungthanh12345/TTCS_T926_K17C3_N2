using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.DTOs.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/hr/student-registrations/{studentId:int}/documents")]
    [Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]
    [Produces("application/json")]
    public sealed class HrStudentDocumentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public HrStudentDocumentsController(AppDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments(int studentId)
        {
            if (!await _db.Students.AnyAsync(student => student.Id == studentId))
                return NotFound(ApiResponse.Fail("Student registration was not found."));

            var documents = await _db.StudentDocuments.AsNoTracking()
                .Where(document => document.StudentId == studentId)
                .OrderBy(document => document.DocumentType)
                .ThenByDescending(document => document.UploadedAt)
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

        [HttpGet("{documentId:int}/download")]
        public async Task<IActionResult> Download(int studentId, int documentId)
        {
            var document = await _db.StudentDocuments.AsNoTracking().SingleOrDefaultAsync(item =>
                item.Id == documentId && item.StudentId == studentId);
            if (document == null)
                return NotFound(ApiResponse.Fail("Document was not found for this student registration."));

            if (document.FileContent is { Length: > 0 })
                return File(document.FileContent, document.ContentType, document.OriginalFileName);

            var safeStoredName = Path.GetFileName(document.StoredFileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeStoredName) || safeStoredName != document.StoredFileName)
                return NotFound(ApiResponse.Fail("Document content is unavailable."));

            var legacyPath = Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "private-uploads",
                studentId.ToString(),
                safeStoredName);
            return System.IO.File.Exists(legacyPath)
                ? PhysicalFile(legacyPath, document.ContentType, document.OriginalFileName)
                : NotFound(ApiResponse.Fail("Document content is unavailable."));
        }
    }
}
