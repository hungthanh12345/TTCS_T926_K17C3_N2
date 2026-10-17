using System.Data;
using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs;
using InternshipManagementApi.Services.Reports;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Services;

public interface IProgramCompletionService
{
    Task<ProgramCompletionDashboardDto> GetDashboardAsync(int? programId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudentProgramAuditEventDto>> GetHistoryAsync(
        int studentId,
        int programId,
        CancellationToken cancellationToken);
    Task UpdateParticipationAsync(
        int studentId,
        UpdateProgramParticipationRequestDto request,
        int actorUserId,
        CancellationToken cancellationToken);
    Task ConfirmCompletionAsync(
        int studentId,
        int programId,
        string? reason,
        int actorUserId,
        CancellationToken cancellationToken);
    Task RevokeCompletionAsync(
        int studentId,
        int programId,
        string? reason,
        int actorUserId,
        CancellationToken cancellationToken);
    Task ResetParticipationForProgramAssignmentAsync(
        int studentId,
        int programId,
        int actorUserId,
        CancellationToken cancellationToken);
}

public sealed class ProgramCompletionService : IProgramCompletionService
{
    private const string ReassignmentReason = "Chương trình được phân công lại; HR cần xác minh trạng thái tham gia.";

    private readonly AppDbContext _db;

    public ProgramCompletionService(AppDbContext db) => _db = db;

    public async Task<ProgramCompletionDashboardDto> GetDashboardAsync(
        int? programId,
        CancellationToken cancellationToken)
    {
        var programs = await _db.InternshipPrograms
            .AsNoTracking()
            .OrderBy(program => program.Name)
            .Select(program => new ProgramCompletionSummaryDto
            {
                ProgramId = program.Id,
                ProgramName = program.Name
            })
            .ToListAsync(cancellationToken);

        if (programId.HasValue && programs.All(program => program.ProgramId != programId.Value))
            throw new NotFoundException("Chương trình thực tập không tồn tại.");

        var selectedProgramId = programId ?? programs.FirstOrDefault()?.ProgramId;
        var rows = await CurrentStudentStateQuery()
            .ToListAsync(cancellationToken);

        var candidates = rows.Select(row => new ProgramCompletionCandidate(
            row.ProgramId,
            row.ApplicationApproved,
            row.ParticipationStatus,
            row.LatestCompletionAction)).ToArray();

        var summaries = programs.Select(program =>
        {
            var counts = ProgramCompletionRules.Count(candidates, program.ProgramId);
            return new ProgramCompletionSummaryDto
            {
                ProgramId = program.ProgramId,
                ProgramName = program.ProgramName,
                EligibleCount = counts.EligibleCount,
                CompletedCount = counts.CompletedCount,
                CompletionRate = ProgramCompletionRate.CalculatePercent(counts.EligibleCount, counts.CompletedCount)
            };
        }).ToArray();

        var students = selectedProgramId.HasValue
            ? rows.Where(row => row.ProgramId == selectedProgramId.Value)
                .OrderBy(row => row.FullName)
                .Select(row =>
                {
                    var eligible = ProgramCompletionRules.IsEligible(
                        row.ApplicationApproved,
                        row.ProgramId,
                        row.ParticipationStatus);
                    var hasCurrentConfirmation = row.LatestCompletionAction == StudentProgramAuditEvent.CompletionConfirmed;
                    return new ProgramCompletionStudentDto
                    {
                        StudentId = row.StudentId,
                        StudentCode = row.StudentCode,
                        FullName = row.FullName,
                        ApplicationApproved = row.ApplicationApproved,
                        ParticipationStatus = row.ParticipationStatus,
                        IsEligible = eligible,
                        CompletionConfirmed = ProgramCompletionRules.IsCompletionConfirmed(
                            eligible,
                            row.LatestCompletionAction),
                        HasCurrentConfirmation = hasCurrentConfirmation
                    };
                }).ToArray()
            : Array.Empty<ProgramCompletionStudentDto>();

        return new ProgramCompletionDashboardDto
        {
            SelectedProgramId = selectedProgramId,
            Programs = summaries,
            Students = students
        };
    }

    public async Task<IReadOnlyList<StudentProgramAuditEventDto>> GetHistoryAsync(
        int studentId,
        int programId,
        CancellationToken cancellationToken)
    {
        if (!await _db.InternshipPrograms.AnyAsync(program => program.Id == programId, cancellationToken))
            throw new NotFoundException("Chương trình thực tập không tồn tại.");

        var student = await _db.Students.AsNoTracking()
            .Where(item => item.Id == studentId)
            .Select(item => new { item.Id, item.ProgramId })
            .SingleOrDefaultAsync(cancellationToken);
        if (student == null)
            throw new NotFoundException("Không tìm thấy hồ sơ sinh viên.");

        var events = _db.StudentProgramAuditEvents.AsNoTracking()
            .Where(item => item.StudentId == studentId && item.ProgramId == programId);
        if (student.ProgramId != programId && !await events.AnyAsync(cancellationToken))
            throw new NotFoundException("Không tìm thấy lịch sử của sinh viên trong chương trình này.");

        return await events.OrderBy(item => item.Id)
            .Select(item => new StudentProgramAuditEventDto
            {
                Id = item.Id,
                StudentId = item.StudentId,
                ProgramId = item.ProgramId,
                ActorUserId = item.ActorUserId,
                EventType = item.EventType,
                PreviousStatus = item.PreviousStatus,
                NewStatus = item.NewStatus,
                Reason = item.Reason,
                OccurredAtUtc = item.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateParticipationAsync(
        int studentId,
        UpdateProgramParticipationRequestDto request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var status = request.Status.Trim().ToUpperInvariant();
        if (!ProgramParticipationStatus.Allowed.Contains(status))
            throw new BadRequestException("Trạng thái tham gia không hợp lệ.");

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            var student = await LockStudentAsync(studentId, cancellationToken);
            await ValidateCurrentAssignmentAsync(student, request.ProgramId, actorUserId, cancellationToken);

            var (currentStatus, latestCompletionAction) = await GetCurrentStateAsync(
                studentId,
                request.ProgramId,
                cancellationToken);
            if (currentStatus == status)
                throw new ConflictException("Sinh viên đã ở trạng thái tham gia này.");

            if (latestCompletionAction == StudentProgramAuditEvent.CompletionConfirmed &&
                status is not ProgramParticipationStatus.Active and not ProgramParticipationStatus.Ended)
            {
                throw new ConflictException("Hãy thu hồi xác nhận hoàn thành trước khi chuyển sang trạng thái này.");
            }

            _db.StudentProgramAuditEvents.Add(NewEvent(
                studentId,
                request.ProgramId,
                actorUserId,
                StudentProgramAuditEvent.ParticipationStatusChanged,
                currentStatus,
                status,
                NormalizeReason(request.Reason)));
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task ConfirmCompletionAsync(
        int studentId,
        int programId,
        string? reason,
        int actorUserId,
        CancellationToken cancellationToken) =>
        ChangeCompletionAsync(
            studentId,
            programId,
            reason,
            actorUserId,
            StudentProgramAuditEvent.CompletionConfirmed,
            cancellationToken);

    public Task RevokeCompletionAsync(
        int studentId,
        int programId,
        string? reason,
        int actorUserId,
        CancellationToken cancellationToken) =>
        ChangeCompletionAsync(
            studentId,
            programId,
            reason,
            actorUserId,
            StudentProgramAuditEvent.CompletionRevoked,
            cancellationToken);

    public async Task ResetParticipationForProgramAssignmentAsync(
        int studentId,
        int programId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (_db.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Program reassignment audit must share the assignment transaction.");

        var (currentStatus, latestCompletionAction) = await GetCurrentStateAsync(
            studentId,
            programId,
            cancellationToken);

        if (latestCompletionAction == StudentProgramAuditEvent.CompletionConfirmed)
        {
            _db.StudentProgramAuditEvents.Add(NewEvent(
                studentId,
                programId,
                actorUserId,
                StudentProgramAuditEvent.CompletionRevoked,
                null,
                null,
                ReassignmentReason));
        }

        _db.StudentProgramAuditEvents.Add(NewEvent(
            studentId,
            programId,
            actorUserId,
            StudentProgramAuditEvent.ParticipationStatusChanged,
            currentStatus,
            ProgramParticipationStatus.Unverified,
            ReassignmentReason));
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ChangeCompletionAsync(
        int studentId,
        int programId,
        string? reason,
        int actorUserId,
        string action,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            var student = await LockStudentAsync(studentId, cancellationToken);
            await ValidateCurrentAssignmentAsync(student, programId, actorUserId, cancellationToken);
            var (currentStatus, latestCompletionAction) = await GetCurrentStateAsync(
                studentId,
                programId,
                cancellationToken);

            if (action == StudentProgramAuditEvent.CompletionConfirmed)
            {
                if (!ProgramCompletionRules.IsEligible(
                        applicationApproved: true,
                        student.ProgramId,
                        currentStatus))
                {
                    throw new ConflictException("Chỉ hồ sơ đã duyệt, đang tham gia hoặc đã kết thúc mới được xác nhận hoàn thành.");
                }

                if (latestCompletionAction == StudentProgramAuditEvent.CompletionConfirmed)
                    throw new ConflictException("Chương trình đã được xác nhận hoàn thành; hãy thu hồi trước khi xác nhận lại.");
            }
            else if (latestCompletionAction != StudentProgramAuditEvent.CompletionConfirmed)
            {
                throw new ConflictException("Không có xác nhận hoàn thành đang hiệu lực để thu hồi.");
            }

            _db.StudentProgramAuditEvents.Add(NewEvent(
                studentId,
                programId,
                actorUserId,
                action,
                null,
                null,
                NormalizeReason(reason)));
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private IQueryable<StudentStateRow> CurrentStudentStateQuery()
    {
        var statusChangedAction = StudentProgramAuditEvent.ParticipationStatusChanged;
        var unverifiedStatus = ProgramParticipationStatus.Unverified;

        return _db.Students.AsNoTracking()
            .Where(student => student.ProgramId.HasValue && student.Program != null)
            .Select(student => new StudentStateRow
            {
                StudentId = student.Id,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                ProgramId = student.ProgramId!.Value,
                ProgramName = student.Program!.Name,
                ApplicationApproved = student.ApplicationStatus == StudentApplicationStatus.Approved,
                ParticipationStatus = _db.StudentProgramAuditEvents.AsNoTracking()
                    .Where(item => item.StudentId == student.Id &&
                                   item.ProgramId == student.ProgramId &&
                                   item.EventType == statusChangedAction)
                    .OrderByDescending(item => item.Id)
                    .Select(item => item.NewStatus)
                    .FirstOrDefault() ?? unverifiedStatus,
                LatestCompletionAction = _db.StudentProgramAuditEvents.AsNoTracking()
                    .Where(item => item.StudentId == student.Id &&
                                   item.ProgramId == student.ProgramId &&
                                   (item.EventType == StudentProgramAuditEvent.CompletionConfirmed ||
                                    item.EventType == StudentProgramAuditEvent.CompletionRevoked))
                    .OrderByDescending(item => item.Id)
                    .Select(item => item.EventType)
                    .FirstOrDefault()
            });
    }

    private async Task<Student> LockStudentAsync(int studentId, CancellationToken cancellationToken)
    {
        var student = await _db.Students
            .FromSqlInterpolated($"SELECT * FROM students WHERE id = {studentId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        return student ?? throw new NotFoundException("Không tìm thấy hồ sơ sinh viên.");
    }

    private async Task ValidateCurrentAssignmentAsync(
        Student student,
        int programId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (student.ProgramId != programId)
            throw new ConflictException("Chương trình của sinh viên đã thay đổi. Hãy tải lại dữ liệu.");

        if (!await _db.InternshipPrograms.AnyAsync(program => program.Id == programId, cancellationToken))
            throw new ConflictException("Chương trình của sinh viên không còn hợp lệ.");

        var actor = await _db.Users.AsNoTracking()
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == actorUserId, cancellationToken);
        if (actor?.Role?.Name != "ROLE_HR")
            throw new UnauthorizedException("Tài khoản HR không hợp lệ.");

        if (!ProgramCompletionRules.IsApplicationApproved(student.ApplicationStatus))
        {
            throw new ConflictException("Chỉ hồ sơ sinh viên đã được HR duyệt mới được phân loại hoặc xác nhận.");
        }
    }

    private async Task<(string ParticipationStatus, string? LatestCompletionAction)> GetCurrentStateAsync(
        int studentId,
        int programId,
        CancellationToken cancellationToken)
    {
        var currentStatus = await _db.StudentProgramAuditEvents.AsNoTracking()
            .Where(item => item.StudentId == studentId &&
                           item.ProgramId == programId &&
                           item.EventType == StudentProgramAuditEvent.ParticipationStatusChanged)
            .OrderByDescending(item => item.Id)
            .Select(item => item.NewStatus)
            .FirstOrDefaultAsync(cancellationToken);

        var latestCompletionAction = await _db.StudentProgramAuditEvents.AsNoTracking()
            .Where(item => item.StudentId == studentId &&
                           item.ProgramId == programId &&
                           (item.EventType == StudentProgramAuditEvent.CompletionConfirmed ||
                            item.EventType == StudentProgramAuditEvent.CompletionRevoked))
            .OrderByDescending(item => item.Id)
            .Select(item => item.EventType)
            .FirstOrDefaultAsync(cancellationToken);

        return (currentStatus ?? ProgramParticipationStatus.Unverified, latestCompletionAction);
    }

    private static StudentProgramAuditEvent NewEvent(
        int studentId,
        int programId,
        int actorUserId,
        string eventType,
        string? previousStatus,
        string? newStatus,
        string? reason) => new()
    {
        StudentId = studentId,
        ProgramId = programId,
        ActorUserId = actorUserId,
        EventType = eventType,
        PreviousStatus = previousStatus,
        NewStatus = newStatus,
        Reason = reason,
        OccurredAtUtc = DateTime.UtcNow
    };

    private static string? NormalizeReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

    private sealed class StudentStateRow
    {
        public int StudentId { get; init; }
        public string StudentCode { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public int ProgramId { get; init; }
        public string ProgramName { get; init; } = string.Empty;
        public bool ApplicationApproved { get; init; }
        public string ParticipationStatus { get; init; } = string.Empty;
        public string? LatestCompletionAction { get; init; }
    }
}
