using System.ComponentModel.DataAnnotations;

namespace InternshipManagementApi.DTOs;

public sealed class ProgramCompletionDashboardDto
{
    public int? SelectedProgramId { get; init; }
    public IReadOnlyList<ProgramCompletionSummaryDto> Programs { get; init; }
        = Array.Empty<ProgramCompletionSummaryDto>();
    public IReadOnlyList<ProgramCompletionStudentDto> Students { get; init; }
        = Array.Empty<ProgramCompletionStudentDto>();
}

public sealed class ProgramCompletionSummaryDto
{
    public int ProgramId { get; init; }
    public string ProgramName { get; init; } = string.Empty;
    public int EligibleCount { get; init; }
    public int CompletedCount { get; init; }
    public decimal? CompletionRate { get; init; }
}

public sealed class ProgramCompletionStudentDto
{
    public int StudentId { get; init; }
    public string StudentCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool ApplicationApproved { get; init; }
    public string ParticipationStatus { get; init; } = string.Empty;
    public bool IsEligible { get; init; }
    public bool CompletionConfirmed { get; init; }
    public bool HasCurrentConfirmation { get; init; }
}

public sealed class StudentProgramAuditEventDto
{
    public int Id { get; init; }
    public int StudentId { get; init; }
    public int ProgramId { get; init; }
    public int ActorUserId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? PreviousStatus { get; init; }
    public string? NewStatus { get; init; }
    public string? Reason { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}

public sealed class UpdateProgramParticipationRequestDto
{
    [Range(1, int.MaxValue)]
    public int ProgramId { get; init; }

    [Required]
    [StringLength(16)]
    public string Status { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Reason { get; init; }
}

public sealed class ProgramCompletionActionRequestDto
{
    [StringLength(500)]
    public string? Reason { get; init; }
}
