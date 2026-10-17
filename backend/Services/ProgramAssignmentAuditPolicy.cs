namespace InternshipManagementApi.Services;

public static class ProgramAssignmentAuditPolicy
{
    public static IReadOnlyList<int> GetAffectedProgramIds(int? previousProgramId, int? requestedProgramId)
    {
        if (previousProgramId == requestedProgramId)
            return Array.Empty<int>();

        var affectedProgramIds = new List<int>(capacity: 2);
        if (previousProgramId.HasValue)
            affectedProgramIds.Add(previousProgramId.Value);
        if (requestedProgramId.HasValue)
            affectedProgramIds.Add(requestedProgramId.Value);

        return affectedProgramIds;
    }
}
