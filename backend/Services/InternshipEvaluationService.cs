using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Evaluations;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace InternshipManagementApi.Services
{
    public interface IInternshipEvaluationService
    {
        Task<IReadOnlyList<MentorEvaluationStudentDto>> GetAssignedStudentsAsync(int mentorUserId);
        Task<InternshipEvaluationResponseDto> GetAssignedEvaluationAsync(int mentorUserId, int evaluationId);
        Task<InternshipEvaluationResponseDto> CreateAsync(int mentorUserId, int studentId, InternshipEvaluationRequestDto request);
        Task<InternshipEvaluationResponseDto> UpdateAsync(int mentorUserId, int evaluationId, InternshipEvaluationRequestDto request);
        Task<IReadOnlyList<InternshipEvaluationResponseDto>> GetOwnAsync(int studentUserId);
        Task<InternshipEvaluationResponseDto> GetOwnByIdAsync(int studentUserId, int evaluationId);
    }

    public sealed class InternshipEvaluationService : IInternshipEvaluationService
    {
        private readonly AppDbContext _db;

        public InternshipEvaluationService(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<MentorEvaluationStudentDto>> GetAssignedStudentsAsync(int mentorUserId)
        {
            var mentor = await GetMentorAsync(mentorUserId);
            var students = await _db.Students.AsNoTracking()
                .Where(student => student.MentorId == mentor.Id &&
                                  student.User != null &&
                                  student.User.Role.Name == "ROLE_STUDENT" &&
                                  student.User.Status == UserStatus.ACTIVE)
                .OrderBy(student => student.FullName)
                .Select(student => new
                {
                    student.Id,
                    student.StudentCode,
                    student.FullName,
                    student.University,
                    student.Major
                })
                .ToListAsync();

            if (students.Count == 0) return Array.Empty<MentorEvaluationStudentDto>();

            var studentIds = students.Select(student => student.Id).ToArray();
            var evaluations = await _db.InternshipEvaluations.AsNoTracking()
                .Include(evaluation => evaluation.Student)
                .Where(evaluation => studentIds.Contains(evaluation.StudentId))
                .ToListAsync();
            var evaluationByStudent = evaluations.ToDictionary(
                evaluation => evaluation.StudentId,
                evaluation => ToResponse(evaluation, evaluation.MentorId == mentor.Id));

            return students.Select(student => new MentorEvaluationStudentDto
            {
                StudentId = student.Id,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                University = student.University,
                Major = student.Major,
                Evaluation = evaluationByStudent.GetValueOrDefault(student.Id)
            }).ToArray();
        }

        public async Task<InternshipEvaluationResponseDto> GetAssignedEvaluationAsync(int mentorUserId, int evaluationId)
        {
            var mentor = await GetMentorAsync(mentorUserId);
            var evaluation = await GetActiveAssignedEvaluationQuery(mentor.Id)
                .SingleOrDefaultAsync(item => item.Id == evaluationId);
            return evaluation == null
                ? throw new NotFoundException("Evaluation was not found in this mentor's assignments.")
                : ToResponse(evaluation, evaluation.MentorId == mentor.Id);
        }

        public async Task<InternshipEvaluationResponseDto> CreateAsync(
            int mentorUserId,
            int studentId,
            InternshipEvaluationRequestDto request)
        {
            ValidateRequest(request);
            var mentor = await GetMentorAsync(mentorUserId);
            var student = await GetActiveAssignedStudentAsync(mentor.Id, studentId);
            if (await _db.InternshipEvaluations.AnyAsync(evaluation => evaluation.StudentId == student.Id))
                throw new ConflictException("An evaluation already exists for this student.");

            var now = DateTime.UtcNow;
            var evaluation = new InternshipEvaluation
            {
                StudentId = student.Id,
                Student = student,
                MentorId = mentor.Id,
                Mentor = mentor,
                MentorNameSnapshot = mentor.FullName,
                SkillsScore = request.SkillsScore,
                AttitudeScore = request.AttitudeScore,
                Comments = request.Comments.Trim(),
                EvaluatedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.InternshipEvaluations.Add(evaluation);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsDuplicateKey(exception))
            {
                throw new ConflictException("An evaluation already exists for this student.");
            }

            return ToResponse(evaluation, canEdit: true);
        }

        public async Task<InternshipEvaluationResponseDto> UpdateAsync(
            int mentorUserId,
            int evaluationId,
            InternshipEvaluationRequestDto request)
        {
            ValidateRequest(request);
            var mentor = await GetMentorAsync(mentorUserId);
            var evaluation = await GetActiveAssignedEvaluationQuery(mentor.Id)
                .SingleOrDefaultAsync(item => item.Id == evaluationId);
            if (evaluation == null)
                throw new NotFoundException("Evaluation was not found in this mentor's assignments.");
            if (evaluation.MentorId != mentor.Id)
                throw new ForbiddenException("A mentor can only update evaluations they authored.");

            evaluation.SkillsScore = request.SkillsScore;
            evaluation.AttitudeScore = request.AttitudeScore;
            evaluation.Comments = request.Comments.Trim();
            evaluation.EvaluatedAt = DateTime.UtcNow;
            evaluation.UpdatedAt = evaluation.EvaluatedAt;
            await _db.SaveChangesAsync();
            return ToResponse(evaluation, canEdit: true);
        }

        public async Task<IReadOnlyList<InternshipEvaluationResponseDto>> GetOwnAsync(int studentUserId)
        {
            var student = await GetStudentAsync(studentUserId);
            var evaluations = await _db.InternshipEvaluations.AsNoTracking()
                .Include(evaluation => evaluation.Student)
                .Where(evaluation => evaluation.StudentId == student.Id)
                .OrderByDescending(evaluation => evaluation.EvaluatedAt)
                .ToListAsync();
            return evaluations.Select(evaluation => ToResponse(evaluation)).ToArray();
        }

        public async Task<InternshipEvaluationResponseDto> GetOwnByIdAsync(int studentUserId, int evaluationId)
        {
            var student = await GetStudentAsync(studentUserId);
            var evaluation = await _db.InternshipEvaluations.AsNoTracking()
                .Include(item => item.Student)
                .SingleOrDefaultAsync(item => item.Id == evaluationId && item.StudentId == student.Id);
            return evaluation == null
                ? throw new NotFoundException("Evaluation was not found for this student.")
                : ToResponse(evaluation);
        }

        private IQueryable<InternshipEvaluation> GetActiveAssignedEvaluationQuery(int mentorId) =>
            _db.InternshipEvaluations
                .Include(evaluation => evaluation.Student)
                .Where(evaluation => evaluation.Student.MentorId == mentorId &&
                                     evaluation.Student.User != null &&
                                     evaluation.Student.User.Role.Name == "ROLE_STUDENT" &&
                                     evaluation.Student.User.Status == UserStatus.ACTIVE);

        private async Task<Student> GetActiveAssignedStudentAsync(int mentorId, int studentId)
        {
            var student = await _db.Students
                .Include(item => item.User)
                    .ThenInclude(user => user!.Role)
                .SingleOrDefaultAsync(item => item.Id == studentId && item.MentorId == mentorId);
            if (student == null)
                throw new NotFoundException("The student is not assigned to this mentor.");
            if (student.User == null || student.User.Status != UserStatus.ACTIVE ||
                !string.Equals(student.User.Role?.Name, "ROLE_STUDENT", StringComparison.OrdinalIgnoreCase))
                throw new ConflictException("Only active student accounts can be evaluated.");
            return student;
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

        private static InternshipEvaluationResponseDto ToResponse(InternshipEvaluation evaluation, bool canEdit = false) => new()
        {
            Id = evaluation.Id,
            StudentId = evaluation.StudentId,
            StudentCode = evaluation.Student.StudentCode,
            StudentName = evaluation.Student.FullName,
            University = evaluation.Student.University,
            Major = evaluation.Student.Major,
            SkillsScore = evaluation.SkillsScore,
            AttitudeScore = evaluation.AttitudeScore,
            OverallScore = (evaluation.SkillsScore + evaluation.AttitudeScore) / 2m,
            Comments = evaluation.Comments,
            MentorName = evaluation.MentorNameSnapshot,
            EvaluatedAt = evaluation.EvaluatedAt,
            UpdatedAt = evaluation.UpdatedAt,
            CanEdit = canEdit
        };

        private static void ValidateRequest(InternshipEvaluationRequestDto request)
        {
            if (request.SkillsScore is < 1 or > 10 || request.AttitudeScore is < 1 or > 10)
                throw new BadRequestException("Evaluation scores must be between 1 and 10.");
            if (string.IsNullOrWhiteSpace(request.Comments) || request.Comments.Trim().Length < 2)
                throw new BadRequestException("Evaluation comments must contain at least 2 non-whitespace characters.");
            if (request.Comments.Length > 4000)
                throw new BadRequestException("Evaluation comments cannot exceed 4000 characters.");
        }

        private static bool IsDuplicateKey(DbUpdateException exception) =>
            exception.GetBaseException() is MySqlException { Number: 1062 };
    }
}
