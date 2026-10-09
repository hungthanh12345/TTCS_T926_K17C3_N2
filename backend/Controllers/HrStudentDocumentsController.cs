using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.DTOs.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InternshipManagementApi.Controllers
{
    [ApiController]
    [Route("api/hr/student-registrations/{studentId:int}/documents")]
    [Authorize(Roles = "ROLE_HR,ROLE_ADMIN")]
    [Produces("application/json")]
    public sealed class HrStudentDocumentsController : ControllerBase
    {
        private const string PendingStatus = "PENDING";
        private const string ApprovedStatus = "APPROVED";
        private const string RejectedStatus = "REJECTED";
        private const int MaxReviewAttempts = 3;

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

            var documents = await QueryDocumentResponses(studentId)
                .OrderBy(document => document.DocumentType)
                .ThenByDescending(document => document.UploadedAt)
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<HrStudentDocumentResponse>>.Ok(documents));
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

        [HttpPost("{documentId:int}/approve")]
        [Authorize(Roles = "ROLE_HR")]
        [ProducesResponseType(typeof(ApiResponse<HrStudentDocumentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public Task<IActionResult> ApproveDocument(int studentId, int documentId)
        {
            return ReviewDocumentAsync(studentId, documentId, ApprovedStatus, null,
                "Document approved.");
        }

        [HttpPost("{documentId:int}/reject")]
        [Authorize(Roles = "ROLE_HR")]
        [ProducesResponseType(typeof(ApiResponse<HrStudentDocumentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public Task<IActionResult> RejectDocument(
            int studentId,
            int documentId,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReviewStudentDocumentRequest? request)
        {
            return ReviewDocumentAsync(studentId, documentId, RejectedStatus,
                request?.RejectionReason, "Document rejected.");
        }

        private async Task<IActionResult> ReviewDocumentAsync(
            int studentId,
            int documentId,
            string nextStatus,
            string? suppliedReason,
            string successMessage)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId");
            if (!int.TryParse(userIdValue, out var reviewerId))
                return Unauthorized(ApiResponse.Fail("Invalid reviewer identity."));

            var normalizedReason = nextStatus == RejectedStatus
                ? NormalizeRejectionReason(suppliedReason)
                : null;

            for (var attempt = 0; attempt < MaxReviewAttempts; attempt++)
            {
                var current = await _db.StudentDocuments.AsNoTracking()
                    .Where(document => document.Id == documentId && document.StudentId == studentId)
                    .Select(document => new DocumentReviewSnapshot(
                        document.ReviewStatus,
                        document.ReviewedByUserId,
                        document.ReviewedAt,
                        document.RejectionReason))
                    .SingleOrDefaultAsync();

                if (current == null)
                    return NotFound(ApiResponse.Fail("Document was not found for this student registration."));

                if (current.ReviewStatus != PendingStatus &&
                    current.ReviewStatus != ApprovedStatus &&
                    current.ReviewStatus != RejectedStatus)
                {
                    return Conflict(ApiResponse.Fail("The document has an unsupported review status."));
                }

                var sameDecision = current.ReviewStatus == nextStatus &&
                    string.Equals(current.RejectionReason, normalizedReason, StringComparison.Ordinal);
                if (sameDecision && current.ReviewedByUserId.HasValue && current.ReviewedAt.HasValue)
                {
                    var unchangedResponse = await FindDocumentResponseAsync(studentId, documentId);
                    return unchangedResponse == null
                        ? NotFound(ApiResponse.Fail("Document was not found for this student registration."))
                        : Ok(ApiResponse<HrStudentDocumentResponse>.Ok(unchangedResponse, successMessage));
                }

                var reviewedAtUtc = DateTime.UtcNow;
                var affectedRows = await _db.StudentDocuments
                    .Where(document => document.Id == documentId &&
                        document.StudentId == studentId &&
                        document.ReviewStatus == current.ReviewStatus &&
                        document.ReviewedByUserId == current.ReviewedByUserId &&
                        document.ReviewedAt == current.ReviewedAt &&
                        document.RejectionReason == current.RejectionReason)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(document => document.ReviewStatus, nextStatus)
                        .SetProperty(document => document.ReviewedByUserId, (int?)reviewerId)
                        .SetProperty(document => document.ReviewedAt, (DateTime?)reviewedAtUtc)
                        .SetProperty(document => document.RejectionReason, normalizedReason));

                if (affectedRows == 1)
                {
                    var updatedResponse = await FindDocumentResponseAsync(studentId, documentId);
                    return updatedResponse == null
                        ? NotFound(ApiResponse.Fail("Document was not found for this student registration."))
                        : Ok(ApiResponse<HrStudentDocumentResponse>.Ok(updatedResponse, successMessage));
                }
            }

            return Conflict(ApiResponse.Fail("The document changed during review. Refresh and try again."));
        }

        private IQueryable<HrStudentDocumentResponse> QueryDocumentResponses(int studentId)
        {
            return from document in _db.StudentDocuments.AsNoTracking()
                   join reviewer in _db.Users.AsNoTracking()
                       on document.ReviewedByUserId equals (int?)reviewer.Id into reviewers
                   from reviewer in reviewers.DefaultIfEmpty()
                   where document.StudentId == studentId
                   select new HrStudentDocumentResponse(
                       document.Id,
                       document.DocumentType,
                       document.OriginalFileName,
                       document.ContentType,
                       document.SizeBytes,
                       document.UploadedAt,
                       document.ReviewStatus,
                       document.ReviewedByUserId,
                       reviewer == null ? null : reviewer.Email,
                       document.ReviewedAt,
                       document.RejectionReason);
        }

        private Task<HrStudentDocumentResponse?> FindDocumentResponseAsync(int studentId, int documentId)
        {
            return QueryDocumentResponses(studentId)
                .SingleOrDefaultAsync(document => document.Id == documentId);
        }

        private static string? NormalizeRejectionReason(string? reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        }

        private sealed record DocumentReviewSnapshot(
            string ReviewStatus,
            int? ReviewedByUserId,
            DateTime? ReviewedAt,
            string? RejectionReason);
    }
}
