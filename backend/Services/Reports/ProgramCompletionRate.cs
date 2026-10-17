namespace InternshipManagementApi.Services.Reports;

public static class ProgramCompletionRate
{
    public static decimal? CalculatePercent(int eligibleCount, int completedCount)
    {
        if (eligibleCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eligibleCount));
        }

        if (completedCount < 0 || completedCount > eligibleCount)
        {
            throw new ArgumentOutOfRangeException(nameof(completedCount));
        }

        return eligibleCount == 0
            ? null
            : completedCount * 100m / eligibleCount;
    }
}
