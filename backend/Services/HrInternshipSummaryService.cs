using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Evaluations;
using InternshipManagementApi.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Services
{
    public interface IHrInternshipSummaryService
    {
        Task<HrInternshipSummaryResponseDto> GetSummaryAsync(int? programId = null);
    }

    public sealed class HrInternshipSummaryService : IHrInternshipSummaryService
    {
        private readonly AppDbContext _db;

        public HrInternshipSummaryService(AppDbContext db) => _db = db;

        public async Task<HrInternshipSummaryResponseDto> GetSummaryAsync(int? programId = null)
        {
            var selectedProgram = programId.HasValue
                ? await _db.InternshipPrograms.AsNoTracking().SingleOrDefaultAsync(program => program.Id == programId.Value)
                : null;
            if (programId.HasValue && selectedProgram == null)
                throw new NotFoundException("Internship program was not found.");

            var students = await _db.Students.AsNoTracking()
                .Where(student => student.User != null &&
                                  student.User.Role.Name == "ROLE_STUDENT" &&
                                  student.User.Status == UserStatus.ACTIVE &&
                                  (!programId.HasValue || student.ProgramId == programId.Value))
                .OrderBy(student => student.FullName)
                .Select(student => new
                {
                    student.Id,
                    student.ProgramId,
                    ProgramName = student.Program == null ? null : student.Program.Name,
                    student.StudentCode,
                    student.FullName,
                    student.University,
                    student.Major,
                    MentorName = student.Mentor == null ? null : student.Mentor.FullName,
                    MentorDepartment = student.Mentor == null ? null : student.Mentor.Department
                })
                .ToListAsync();

            var evaluations = await _db.InternshipEvaluations.AsNoTracking()
                .ToDictionaryAsync(evaluation => evaluation.StudentId);

            var reportStatistics = await _db.WeeklyReports.AsNoTracking()
                .GroupBy(report => report.StudentId)
                .Select(group => new
                {
                    StudentId = group.Key,
                    ReportCount = group.Count(),
                    LatestReportWeek = (DateOnly?)group.Max(report => report.WeekStartDate)
                })
                .ToDictionaryAsync(statistic => statistic.StudentId);

            var feedbackStatistics = await (
                from feedback in _db.MentorFeedbacks.AsNoTracking()
                join report in _db.WeeklyReports.AsNoTracking()
                    on feedback.WeeklyReportId equals report.Id
                group feedback by report.StudentId into feedbackGroup
                select new { StudentId = feedbackGroup.Key, FeedbackCount = feedbackGroup.Count() }
            ).ToDictionaryAsync(statistic => statistic.StudentId, statistic => statistic.FeedbackCount);

            var items = students.Select(student =>
            {
                evaluations.TryGetValue(student.Id, out var evaluation);
                reportStatistics.TryGetValue(student.Id, out var reports);
                feedbackStatistics.TryGetValue(student.Id, out var feedbackCount);
                var overallScore = evaluation == null
                    ? (decimal?)null
                    : (evaluation.SkillsScore + evaluation.AttitudeScore) / 2m;

                return new HrInternshipSummaryItemDto
                {
                    StudentId = student.Id,
                    ProgramId = student.ProgramId,
                    ProgramName = student.ProgramName,
                    StudentCode = student.StudentCode,
                    StudentName = student.FullName,
                    University = student.University,
                    Major = student.Major,
                    MentorName = student.MentorName,
                    MentorDepartment = student.MentorDepartment,
                    WeeklyReportCount = reports?.ReportCount ?? 0,
                    MentorFeedbackCount = feedbackCount,
                    LatestReportWeek = reports?.LatestReportWeek,
                    EvaluationStatus = evaluation == null ? "PENDING" : "EVALUATED",
                    SkillsScore = evaluation?.SkillsScore,
                    AttitudeScore = evaluation?.AttitudeScore,
                    OverallScore = overallScore,
                    EvaluationComments = evaluation?.Comments,
                    EvaluatedBy = evaluation?.MentorNameSnapshot,
                    EvaluatedAt = evaluation?.EvaluatedAt
                };
            }).ToArray();

            var evaluatedScores = items
                .Where(item => item.OverallScore.HasValue)
                .Select(item => item.OverallScore!.Value)
                .ToArray();

            return new HrInternshipSummaryResponseDto
            {
                ProgramId = selectedProgram?.Id,
                ProgramName = selectedProgram?.Name,
                TotalStudents = items.Length,
                EvaluatedStudents = evaluatedScores.Length,
                PendingEvaluations = items.Length - evaluatedScores.Length,
                AverageOverallScore = evaluatedScores.Length == 0 ? null : evaluatedScores.Average(),
                Items = items
            };
        }
    }
}
