using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs.Workforce;

public sealed class AttendanceCheckInRequestDto
{
    [StringLength(500)]
    public string? Notes { get; set; }
}

public sealed class AttendanceDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateTime CheckInTimeUtc { get; set; }
    public DateTime? CheckOutTimeUtc { get; set; }
    public int? DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class AttendanceReportResponseDto
{
    public IReadOnlyList<AttendanceDto> Items { get; set; } = Array.Empty<AttendanceDto>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);
    public int UnclassifiedSessions { get; set; }
    public int PresentDays { get; set; }
    public int LateDays { get; set; }
    public int AbsentDays { get; set; }
    public int ApprovedLeaveCalendarDays { get; set; }
    public bool AttendanceClassificationConfigured { get; set; }
}

public sealed class WorkScheduleDayRequestDto
{
    [Range(0, 6)]
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakStart { get; set; }
    public TimeSpan? BreakEnd { get; set; }
}

public sealed class WorkScheduleRequestDto
{
    public DateOnly? EffectiveStartDate { get; set; }
    [StringLength(20)]
    public string? RecurrenceType { get; set; }
    [Range(1, 52)]
    public int? RecurrenceWeeks { get; set; }
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string TargetType { get; set; } = string.Empty;
    public int? StudentGroupId { get; set; }
    public int? StudentId { get; set; }
    public bool IsFlexible { get; set; }
    [Required]
    public string Status { get; set; } = "ACTIVE";
    [MinLength(1), MaxLength(7)]
    public List<WorkScheduleDayRequestDto> Days { get; set; } = new();
}

public sealed class WorkScheduleDto
{
    public DateOnly? EffectiveStartDate { get; set; }
    public string? RecurrenceType { get; set; }
    public int? RecurrenceWeeks { get; set; }
    public DateOnly? EffectiveEndDate { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public int? StudentGroupId { get; set; }
    public string? StudentGroupName { get; set; }
    public int? StudentId { get; set; }
    public string? StudentName { get; set; }
    public bool IsFlexible { get; set; }
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<WorkScheduleDayRequestDto> Days { get; set; } = Array.Empty<WorkScheduleDayRequestDto>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class StudentGroupRequestDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    [StringLength(500)]
    public string? Description { get; set; }
}

public sealed class StudentGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<StudentGroupMemberDto> Members { get; set; } = Array.Empty<StudentGroupMemberDto>();
}

public sealed class StudentGroupMemberDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime JoinedAt { get; set; }
}

public sealed class LeaveRequestCreateDto
{
    [Required]
    public DateOnly StartDate { get; set; }
    [Required]
    public DateOnly EndDate { get; set; }
    [Required, StringLength(2000, MinimumLength = 2)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class LeaveRequestReviewDto
{
    public bool Approve { get; set; }
    [StringLength(1000)]
    public string? RejectionReason { get; set; }
}

public sealed class LeaveRequestDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AllowanceRequestDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string AllowanceType { get; set; } = string.Empty;
    [StringLength(1000)]
    public string? Description { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }
    [Required]
    public DateOnly PeriodStart { get; set; }
    [Required]
    public DateOnly PeriodEnd { get; set; }
}

public sealed class AllowancePaymentRequestDto
{
    [Range(typeof(decimal), "0.01", "9999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }
    [StringLength(150)]
    public string? Reference { get; set; }
}

public sealed class AllowanceDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string AllowanceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => Amount - PaidAmount;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public IReadOnlyList<AllowancePaymentDto> Payments { get; set; } = Array.Empty<AllowancePaymentDto>();
}

public sealed class AllowancePaymentDto
{
    public int Id { get; set; }
    public int AllowanceId { get; set; }
    public string AllowanceType { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; }
    public string? Reference { get; set; }
}

public sealed class AllowanceHistoryResponseDto
{
    public IReadOnlyList<AllowancePaymentDto> Items { get; set; } = Array.Empty<AllowancePaymentDto>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);
    public decimal TotalReceived { get; set; }
}

public sealed class SupportRequestCreateDto
{
    [Required, StringLength(160, MinimumLength = 3)]
    public string Subject { get; set; } = string.Empty;
    [Required, StringLength(5000, MinimumLength = 2)]
    public string Description { get; set; } = string.Empty;
    [Required, StringLength(80, MinimumLength = 2)]
    public string Category { get; set; } = string.Empty;
}

public sealed class SupportRequestUpdateDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
    [StringLength(4000)]
    public string? Response { get; set; }
}

public sealed class SupportRequestDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Response { get; set; }
    public int? HandledByUserId { get; set; }
    public DateTime? HandledAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
