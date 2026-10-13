using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Data;
using InternshipManagementApi.Data.Entities;
using InternshipManagementApi.DTOs.Workforce;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace InternshipManagementApi.Services;

public interface IWorkScheduleService
{
    Task<IReadOnlyList<WorkScheduleDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<WorkScheduleDto> GetAsync(int id, CancellationToken cancellationToken);
    Task<WorkScheduleDto> CreateAsync(WorkScheduleRequestDto request, CancellationToken cancellationToken);
    Task<WorkScheduleDto> UpdateAsync(int id, WorkScheduleRequestDto request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<StudentGroupDto>> GetGroupsAsync(CancellationToken cancellationToken);
    Task<StudentGroupDto> CreateGroupAsync(StudentGroupRequestDto request, CancellationToken cancellationToken);
    Task<StudentGroupDto> UpdateGroupAsync(int id, StudentGroupRequestDto request, CancellationToken cancellationToken);
    Task AssignStudentAsync(int groupId, int studentId, CancellationToken cancellationToken);
    Task RemoveStudentAsync(int groupId, int studentId, CancellationToken cancellationToken);
}

public sealed class WorkScheduleService : IWorkScheduleService
{
    private readonly AppDbContext _db;
    public WorkScheduleService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<WorkScheduleDto>> GetAllAsync(CancellationToken cancellationToken) =>
        (await QuerySchedules().OrderBy(item => item.Name).ToListAsync(cancellationToken)).Select(ToDto).ToArray();

    public async Task<WorkScheduleDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var schedule = await QuerySchedules().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return schedule == null ? throw new NotFoundException("Work schedule was not found.") : ToDto(schedule);
    }

    public async Task<WorkScheduleDto> CreateAsync(WorkScheduleRequestDto request, CancellationToken cancellationToken)
    {
        Validate(request);
        await EnsureTargetExistsAsync(request, cancellationToken);
        var now = DateTime.UtcNow;
        var schedule = new WorkSchedule
        {
            Name = request.Name.Trim(),
            TargetType = request.TargetType,
            StudentGroupId = request.StudentGroupId,
            StudentId = request.StudentId,
            IsFlexible = request.IsFlexible,
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now,
            Days = request.Days.Select(ToEntity).ToList()
        };
        _db.WorkSchedules.Add(schedule);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetAsync(schedule.Id, cancellationToken);
    }

    public async Task<WorkScheduleDto> UpdateAsync(int id, WorkScheduleRequestDto request, CancellationToken cancellationToken)
    {
        Validate(request);
        await EnsureTargetExistsAsync(request, cancellationToken);
        var schedule = await _db.WorkSchedules.Include(item => item.Days)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Work schedule was not found.");
        schedule.Name = request.Name.Trim();
        schedule.TargetType = request.TargetType;
        schedule.StudentGroupId = request.StudentGroupId;
        schedule.StudentId = request.StudentId;
        schedule.IsFlexible = request.IsFlexible;
        schedule.Status = request.Status;
        schedule.UpdatedAt = DateTime.UtcNow;
        _db.WorkScheduleDays.RemoveRange(schedule.Days);
        schedule.Days = request.Days.Select(ToEntity).ToList();
        await _db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var schedule = await _db.WorkSchedules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Work schedule was not found.");
        _db.WorkSchedules.Remove(schedule); // only owned day rows cascade; student/group history is retained.
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentGroupDto>> GetGroupsAsync(CancellationToken cancellationToken)
    {
        var groups = await _db.StudentGroups.AsNoTracking().Include(group => group.Memberships)
            .ThenInclude(membership => membership.Student).ThenInclude(student => student.User)
            .OrderBy(group => group.Name).ToListAsync(cancellationToken);
        return groups.Select(group => new StudentGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Status = group.Status,
            Members = group.Memberships.OrderBy(member => member.Student.FullName).Select(member => new StudentGroupMemberDto
            {
                StudentId = member.StudentId,
                StudentCode = member.Student.StudentCode,
                FullName = member.Student.FullName,
                Email = member.Student.User?.Email,
                JoinedAt = DateTime.SpecifyKind(member.JoinedAt, DateTimeKind.Utc)
            }).ToArray()
        }).ToArray();
    }

    public async Task<StudentGroupDto> CreateGroupAsync(StudentGroupRequestDto request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await _db.StudentGroups.AnyAsync(group => group.Name == name, cancellationToken))
            throw new ConflictException("A group with this name already exists.");
        var now = DateTime.UtcNow;
        var group = new StudentGroup { Name = name, Description = Clean(request.Description), CreatedAt = now, UpdatedAt = now };
        _db.StudentGroups.Add(group);
        try { await _db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.GetBaseException() is MySqlException { Number: 1062 })
        { throw new ConflictException("A group with this name already exists."); }
        return new StudentGroupDto { Id = group.Id, Name = group.Name, Description = group.Description, Status = group.Status };
    }

    public async Task<StudentGroupDto> UpdateGroupAsync(int id, StudentGroupRequestDto request, CancellationToken cancellationToken)
    {
        var group = await _db.StudentGroups.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Student group was not found.");
        var name = request.Name.Trim();
        if (await _db.StudentGroups.AnyAsync(item => item.Id != id && item.Name == name, cancellationToken))
            throw new ConflictException("A group with this name already exists.");
        group.Name = name;
        group.Description = Clean(request.Description);
        group.UpdatedAt = DateTime.UtcNow;
        try { await _db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.GetBaseException() is MySqlException { Number: 1062 })
        { throw new ConflictException("A group with this name already exists."); }
        return new StudentGroupDto { Id = group.Id, Name = group.Name, Description = group.Description, Status = group.Status };
    }

    public async Task AssignStudentAsync(int groupId, int studentId, CancellationToken cancellationToken)
    {
        if (!await _db.StudentGroups.AnyAsync(group => group.Id == groupId && group.Status == "ACTIVE", cancellationToken))
            throw new NotFoundException("Active student group was not found.");
        if (!await _db.Students.AnyAsync(student => student.Id == studentId, cancellationToken))
            throw new NotFoundException("Student was not found.");
        var membership = await _db.StudentGroupMemberships.SingleOrDefaultAsync(
            item => item.StudentId == studentId && item.StudentGroupId == groupId, cancellationToken);
        if (membership != null)
            return;
        _db.StudentGroupMemberships.Add(new StudentGroupMembership { StudentId = studentId, StudentGroupId = groupId, JoinedAt = DateTime.UtcNow });
        try { await _db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.GetBaseException() is MySqlException { Number: 1062 })
        { throw new ConflictException("This student was already assigned to the group. Reload the membership list."); }
    }

    public async Task RemoveStudentAsync(int groupId, int studentId, CancellationToken cancellationToken)
    {
        var membership = await _db.StudentGroupMemberships.SingleOrDefaultAsync(
            item => item.StudentId == studentId && item.StudentGroupId == groupId, cancellationToken);
        if (membership == null) return;
        _db.StudentGroupMemberships.Remove(membership);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<WorkSchedule> QuerySchedules() => _db.WorkSchedules.AsNoTracking()
        .Include(item => item.Group).Include(item => item.Student).Include(item => item.Days);

    private async Task EnsureTargetExistsAsync(WorkScheduleRequestDto request, CancellationToken cancellationToken)
    {
        if (request.TargetType == WorkScheduleTargetType.Student &&
            !await _db.Students.AnyAsync(student => student.Id == request.StudentId, cancellationToken))
            throw new NotFoundException("Student was not found.");
        if (request.TargetType == WorkScheduleTargetType.Group &&
            !await _db.StudentGroups.AnyAsync(group => group.Id == request.StudentGroupId && group.Status == "ACTIVE", cancellationToken))
            throw new NotFoundException("Active student group was not found.");
    }

    private static void Validate(WorkScheduleRequestDto request)
    {
        if (!string.Equals(request.TargetType, WorkScheduleTargetType.Student, StringComparison.Ordinal) &&
            !string.Equals(request.TargetType, WorkScheduleTargetType.Group, StringComparison.Ordinal))
            throw new BadRequestException("TargetType must be STUDENT or GROUP.");
        if (!new[] { "ACTIVE", "INACTIVE" }.Contains(request.Status, StringComparer.Ordinal))
            throw new BadRequestException("Status must be ACTIVE or INACTIVE.");
        if (request.TargetType == WorkScheduleTargetType.Student && (!request.StudentId.HasValue || request.StudentGroupId.HasValue) ||
            request.TargetType == WorkScheduleTargetType.Group && (!request.StudentGroupId.HasValue || request.StudentId.HasValue))
            throw new BadRequestException("Set exactly one target matching TargetType.");
        if (request.Days.Count == 0 || request.Days.Select(day => day.DayOfWeek).Distinct().Count() != request.Days.Count)
            throw new BadRequestException("A schedule must have unique weekdays and at least one day.");
        foreach (var day in request.Days)
        {
            if (day.DayOfWeek is < 0 or > 6 || day.StartTime < TimeSpan.Zero || day.StartTime >= TimeSpan.FromDays(1) ||
                day.EndTime <= day.StartTime || day.EndTime >= TimeSpan.FromDays(1))
                throw new BadRequestException("Schedule times must be valid within one day and end after start.");
            if (day.BreakStart.HasValue != day.BreakEnd.HasValue ||
                day.BreakStart.HasValue && (day.BreakStart.Value < day.StartTime || day.BreakEnd!.Value > day.EndTime || day.BreakStart.Value >= day.BreakEnd.Value))
                throw new BadRequestException("Break time must be a complete interval inside the work period.");
        }
    }

    private static WorkScheduleDay ToEntity(WorkScheduleDayRequestDto day) => new()
    {
        DayOfWeek = day.DayOfWeek,
        StartTime = day.StartTime,
        EndTime = day.EndTime,
        BreakStart = day.BreakStart,
        BreakEnd = day.BreakEnd
    };

    private static WorkScheduleDto ToDto(WorkSchedule schedule) => new()
    {
        Id = schedule.Id,
        Name = schedule.Name,
        TargetType = schedule.TargetType,
        StudentGroupId = schedule.StudentGroupId,
        StudentGroupName = schedule.Group?.Name,
        StudentId = schedule.StudentId,
        StudentName = schedule.Student?.FullName,
        IsFlexible = schedule.IsFlexible,
        Status = schedule.Status,
        CreatedAt = DateTime.SpecifyKind(schedule.CreatedAt, DateTimeKind.Utc),
        UpdatedAt = DateTime.SpecifyKind(schedule.UpdatedAt, DateTimeKind.Utc),
        Days = schedule.Days.OrderBy(day => day.DayOfWeek).Select(day => new WorkScheduleDayRequestDto
        {
            DayOfWeek = day.DayOfWeek, StartTime = day.StartTime, EndTime = day.EndTime,
            BreakStart = day.BreakStart, BreakEnd = day.BreakEnd
        }).ToArray()
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
