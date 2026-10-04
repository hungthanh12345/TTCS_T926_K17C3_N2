using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.WeeklyReports;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace InternshipManagementApi.Services
{
    public interface IWeeklyReportService
    {
        Task<WeeklyReportResponseDto> CreateOwnAsync(int userId, WeeklyReportRequestDto request);
        Task<IReadOnlyList<WeeklyReportResponseDto>> GetOwnAsync(int userId);
        Task<WeeklyReportResponseDto> GetOwnByIdAsync(int userId, int reportId);
        Task<WeeklyReportResponseDto> UpdateOwnAsync(int userId, int reportId, WeeklyReportRequestDto request);
        Task<IReadOnlyList<WeeklyReportResponseDto>> GetAssignedToMentorAsync(int userId);
        Task<WeeklyReportResponseDto> GetAssignedToMentorByIdAsync(int userId, int reportId);
    }

    public sealed class WeeklyReportService : IWeeklyReportService
    {
        private const string SubmittedStatus = "SUBMITTED";
        private readonly AppDbContext _db;

        public WeeklyReportService(AppDbContext db) => _db = db;

        public async Task<WeeklyReportResponseDto> CreateOwnAsync(int userId, WeeklyReportRequestDto request)
        {
            ValidateRequest(request);
            var student = await GetStudentAsync(userId);
            if (await _db.WeeklyReports.AnyAsync(report =>
                    report.StudentId == student.Id && report.WeekStartDate == request.WeekStartDate))
            {
                throw new ConflictException("A weekly report already exists for this week.");
            }

            var now = DateTime.UtcNow;
            var report = new WeeklyReport
            {
                StudentId = student.Id,
                Student = student,
                WeekStartDate = request.WeekStartDate,
                WorkSummary = request.WorkSummary.Trim(),
                Results = NormalizeOptional(request.Results),
                Challenges = NormalizeOptional(request.Challenges),
                NextWeekPlan = NormalizeOptional(request.NextWeekPlan),
                AttachmentUrl = NormalizeAttachmentUrl(request.AttachmentUrl),
                Status = SubmittedStatus,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.WeeklyReports.Add(report);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsDuplicateKey(exception))
            {
                throw new ConflictException("A weekly report already exists for this week.");
            }

            return ToResponse(report);
        }

        public async Task<IReadOnlyList<WeeklyReportResponseDto>> GetOwnAsync(int userId)
        {
            var student = await GetStudentAsync(userId);
            var reports = await WithDetails(_db.WeeklyReports.AsNoTracking())
                .Where(report => report.StudentId == student.Id)
                .OrderByDescending(report => report.WeekStartDate)
                .ToListAsync();

            return reports.Select(report => ToResponse(report)).ToArray();
        }

        public async Task<WeeklyReportResponseDto> GetOwnByIdAsync(int userId, int reportId)
        {
            var student = await GetStudentAsync(userId);
            var report = await WithDetails(_db.WeeklyReports.AsNoTracking())
                .SingleOrDefaultAsync(item => item.Id == reportId && item.StudentId == student.Id);
            return report == null
                ? throw new NotFoundException("Weekly report was not found.")
                : ToResponse(report);
        }

        public async Task<WeeklyReportResponseDto> UpdateOwnAsync(
            int userId,
            int reportId,
            WeeklyReportRequestDto request)
        {
            ValidateRequest(request);
            var student = await GetStudentAsync(userId);
            var report = await WithDetails(_db.WeeklyReports)
                .SingleOrDefaultAsync(item => item.Id == reportId && item.StudentId == student.Id);

            if (report == null)
                throw new NotFoundException("Weekly report was not found.");
            if (report.Status != SubmittedStatus || report.MentorFeedback != null)
                throw new ConflictException("A reviewed weekly report can no longer be edited.");

            if (await _db.WeeklyReports.AnyAsync(item =>
                    item.StudentId == student.Id &&
                    item.WeekStartDate == request.WeekStartDate &&
                    item.Id != report.Id))
            {
                throw new ConflictException("A weekly report already exists for this week.");
            }

            ApplyRequest(report, request);
            report.UpdatedAt = DateTime.UtcNow;
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsDuplicateKey(exception))
            {
                throw new ConflictException("A weekly report already exists for this week.");
            }

            return ToResponse(report);
        }

        public async Task<IReadOnlyList<WeeklyReportResponseDto>> GetAssignedToMentorAsync(int userId)
        {
            var mentor = await GetMentorAsync(userId);
            var reports = await WithDetails(_db.WeeklyReports.AsNoTracking())
                .Where(report => report.Student.MentorId == mentor.Id)
                .OrderByDescending(report => report.WeekStartDate)
                .ToListAsync();

            return reports.Select(report => ToResponse(report, mentor.Id)).ToArray();
        }

        public async Task<WeeklyReportResponseDto> GetAssignedToMentorByIdAsync(int userId, int reportId)
        {
            var mentor = await GetMentorAsync(userId);
            var report = await WithDetails(_db.WeeklyReports.AsNoTracking())
                .SingleOrDefaultAsync(item => item.Id == reportId && item.Student.MentorId == mentor.Id);
            return report == null
                ? throw new NotFoundException("Weekly report was not found in this mentor's assignments.")
                : ToResponse(report, mentor.Id);
        }

        private async Task<Student> GetStudentAsync(int userId)
        {
            var student = await _db.Students.SingleOrDefaultAsync(item => item.UserId == userId);
            return student ?? throw new NotFoundException("No student profile is linked to this account.");
        }

        private async Task<Mentor> GetMentorAsync(int userId)
        {
            var mentor = await _db.Mentors.SingleOrDefaultAsync(item => item.UserId == userId);
            return mentor ?? throw new NotFoundException("No mentor profile is linked to this account.");
        }

        private static IQueryable<WeeklyReport> WithDetails(IQueryable<WeeklyReport> query) => query
            .Include(report => report.Student)
            .Include(report => report.MentorFeedback)
                .ThenInclude(feedback => feedback!.Mentor);

        private static WeeklyReportResponseDto ToResponse(WeeklyReport report, int? viewerMentorId = null) => new()
        {
            Id = report.Id,
            StudentName = report.Student.FullName,
            StudentCode = report.Student.StudentCode,
            WeekStartDate = report.WeekStartDate,
            WorkSummary = report.WorkSummary,
            Results = report.Results,
            Challenges = report.Challenges,
            NextWeekPlan = report.NextWeekPlan,
            AttachmentUrl = report.AttachmentUrl,
            Status = report.Status,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt,
            CanEditFeedback = viewerMentorId.HasValue && report.MentorFeedback?.MentorId == viewerMentorId.Value,
            MentorFeedback = report.MentorFeedback == null ? null : new MentorFeedbackResponseDto
            {
                Id = report.MentorFeedback.Id,
                Content = report.MentorFeedback.Content,
                MentorName = report.MentorFeedback.Mentor.FullName,
                CreatedAt = report.MentorFeedback.CreatedAt,
                UpdatedAt = report.MentorFeedback.UpdatedAt
            }
        };

        private static void ValidateRequest(WeeklyReportRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.WorkSummary) || request.WorkSummary.Trim().Length < 5)
                throw new BadRequestException("Work summary must contain at least 5 non-whitespace characters.");

            if (request.WeekStartDate == default || request.WeekStartDate.DayOfWeek != DayOfWeek.Monday)
                throw new BadRequestException("Week start date must be a valid Monday.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentWeekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            if (request.WeekStartDate > currentWeekStart)
                throw new BadRequestException("A report cannot be submitted for a future week.");

            if (!string.IsNullOrWhiteSpace(request.AttachmentUrl) && !IsAllowedHttpUrl(request.AttachmentUrl.Trim()))
                throw new BadRequestException("Attachment URL must be an absolute HTTP or HTTPS URL.");
        }

        private static bool IsAllowedHttpUrl(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            !string.IsNullOrWhiteSpace(uri.Host) &&
            string.IsNullOrEmpty(uri.UserInfo);

        private static string? NormalizeAttachmentUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return new Uri(value.Trim(), UriKind.Absolute).AbsoluteUri;
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static void ApplyRequest(WeeklyReport report, WeeklyReportRequestDto request)
        {
            report.WeekStartDate = request.WeekStartDate;
            report.WorkSummary = request.WorkSummary.Trim();
            report.Results = NormalizeOptional(request.Results);
            report.Challenges = NormalizeOptional(request.Challenges);
            report.NextWeekPlan = NormalizeOptional(request.NextWeekPlan);
            report.AttachmentUrl = NormalizeAttachmentUrl(request.AttachmentUrl);
        }

        private static bool IsDuplicateKey(DbUpdateException exception) =>
            exception.GetBaseException() is MySqlException { Number: 1062 };
    }
}
