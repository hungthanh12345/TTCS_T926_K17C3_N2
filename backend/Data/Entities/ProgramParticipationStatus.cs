namespace InternshipManagementApi.Data.Entities;

public static class ProgramParticipationStatus
{
    public const string Unverified = "UNVERIFIED";
    public const string Active = "ACTIVE";
    public const string Ended = "ENDED";
    public const string Cancelled = "CANCELLED";

    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        Unverified,
        Active,
        Ended,
        Cancelled
    };
}
