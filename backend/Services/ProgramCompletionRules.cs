using InternshipManagementApi.Data.Entities;

namespace InternshipManagementApi.Services;

public sealed record ProgramCompletionCandidate(
    int ProgramId,
    bool ApplicationApproved,
    string ParticipationStatus,
    string? LatestCompletionAction);

public sealed record ProgramCompletionCounts(int EligibleCount, int CompletedCount);

public static class ProgramCompletionRules
{
    public static bool IsApplicationApproved(string? applicationStatus)
    {
        return string.Equals(
            applicationStatus,
            StudentApplicationStatus.Approved,
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsEligible(
        bool applicationApproved,
        int? programId,
        string participationStatus)
    {
        return applicationApproved &&
               programId.HasValue &&
               (participationStatus == ProgramParticipationStatus.Active ||
                participationStatus == ProgramParticipationStatus.Ended);
    }

    public static bool IsCompletionConfirmed(bool eligible, string? latestCompletionAction)
    {
        return eligible && latestCompletionAction == StudentProgramAuditEvent.CompletionConfirmed;
    }

    public static ProgramCompletionCounts Count(IEnumerable<ProgramCompletionCandidate> candidates, int programId)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var eligible = candidates
            .Where(candidate => candidate.ProgramId == programId &&
                                IsEligible(
                                    candidate.ApplicationApproved,
                                    candidate.ProgramId,
                                    candidate.ParticipationStatus))
            .ToArray();

        return new ProgramCompletionCounts(
            eligible.Length,
            eligible.Count(candidate => IsCompletionConfirmed(true, candidate.LatestCompletionAction)));
    }
}
