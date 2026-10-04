using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.WeeklyReports;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace InternshipManagementApi.Services
{
    public interface IMentorFeedbackService
    {
        Task<MentorFeedbackResponseDto> CreateAsync(int userId, int reportId, MentorFeedbackRequestDto request);
        Task<MentorFeedbackResponseDto> UpdateAsync(int userId, int reportId, MentorFeedbackRequestDto request);
    }

    public sealed class MentorFeedbackService : IMentorFeedbackService
    {
        private readonly AppDbContext _db;

        public MentorFeedbackService(AppDbContext db) => _db = db;

        public async Task<MentorFeedbackResponseDto> CreateAsync(
            int userId,
            int reportId,
            MentorFeedbackRequestDto request)
        {
            ValidateContent(request.Content);
            var (mentor, report) = await GetAssignedReportAsync(userId, reportId);
            if (report.MentorFeedback != null)
                throw new ConflictException("Feedback has already been submitted for this report.");

            var now = DateTime.UtcNow;
            var feedback = new MentorFeedback
            {
                WeeklyReportId = report.Id,
                MentorId = mentor.Id,
                Mentor = mentor,
                Content = request.Content.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            report.MentorFeedback = feedback;
            report.Status = "REVIEWED";
            report.UpdatedAt = now;
            _db.MentorFeedbacks.Add(feedback);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsDuplicateKey(exception))
            {
                throw new ConflictException("Feedback has already been submitted for this report.");
            }

            return ToResponse(feedback);
        }

        public async Task<MentorFeedbackResponseDto> UpdateAsync(
            int userId,
            int reportId,
            MentorFeedbackRequestDto request)
        {
            ValidateContent(request.Content);
            var (mentor, report) = await GetAssignedReportAsync(userId, reportId);
            var feedback = report.MentorFeedback;
            if (feedback == null)
                throw new NotFoundException("Feedback was not found for this weekly report.");
            if (feedback.MentorId != mentor.Id)
                throw new ForbiddenException("A mentor can only edit feedback they authored.");

            feedback.Content = request.Content.Trim();
            feedback.UpdatedAt = DateTime.UtcNow;
            report.UpdatedAt = feedback.UpdatedAt;
            await _db.SaveChangesAsync();
            return ToResponse(feedback);
        }

        private async Task<(Mentor Mentor, WeeklyReport Report)> GetAssignedReportAsync(int userId, int reportId)
        {
            var mentor = await _db.Mentors.SingleOrDefaultAsync(item => item.UserId == userId);
            if (mentor == null)
                throw new NotFoundException("No mentor profile is linked to this account.");

            var report = await _db.WeeklyReports
                .Include(item => item.Student)
                .Include(item => item.MentorFeedback)
                    .ThenInclude(feedback => feedback!.Mentor)
                .SingleOrDefaultAsync(item => item.Id == reportId && item.Student.MentorId == mentor.Id);

            if (report == null)
                throw new NotFoundException("Weekly report was not found in this mentor's assignments.");

            return (mentor, report);
        }

        private static void ValidateContent(string? content)
        {
            if (string.IsNullOrWhiteSpace(content) || content.Trim().Length < 2)
                throw new BadRequestException("Feedback must contain at least 2 non-whitespace characters.");
        }

        private static MentorFeedbackResponseDto ToResponse(MentorFeedback feedback) => new()
        {
            Id = feedback.Id,
            Content = feedback.Content,
            MentorName = feedback.Mentor.FullName,
            CreatedAt = feedback.CreatedAt,
            UpdatedAt = feedback.UpdatedAt
        };

        private static bool IsDuplicateKey(DbUpdateException exception) =>
            exception.GetBaseException() is MySqlException { Number: 1062 };
    }
}
