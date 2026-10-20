namespace InternshipManagementApi.Data.Entities;

public sealed class StudentProgramAuditEvent
{
    public const string ParticipationStatusChanged = "PARTICIPATION_STATUS_CHANGED";
    public const string CompletionConfirmed = "COMPLETION_CONFIRMED";
    public const string CompletionRevoked = "COMPLETION_REVOKED";

    public int Id { get; set; }
    public int StudentId { get; set; }
    public int ProgramId { get; set; }
    public int ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? Reason { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
