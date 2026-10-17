namespace DatabaseSchemaChecker;

internal static class MigrationClassifier
{
    public static IReadOnlyList<MigrationStatus> Classify(
        IEnumerable<string> sourceIds,
        IEnumerable<string> historyIds,
        IEnumerable<string> knownMissingSourceIds)
    {
        var source = sourceIds.ToHashSet(StringComparer.Ordinal);
        var history = historyIds.ToHashSet(StringComparer.Ordinal);
        var knownMissing = knownMissingSourceIds.ToHashSet(StringComparer.Ordinal);
        var all = source.Union(history).Union(knownMissing).OrderBy(id => id, StringComparer.Ordinal);

        return all.Select(id =>
        {
            var hasSource = source.Contains(id);
            var inHistory = history.Contains(id);
            var isKnownGap = knownMissing.Contains(id);
            var missingSource = !hasSource && (inHistory || isKnownGap);
            var status = missingSource
                ? "SOURCE_MISSING"
                : inHistory ? "APPLIED" : "PENDING";
            var blocksUpgrade = !hasSource && inHistory;

            string? note = null;
            if (isKnownGap)
            {
                note = inHistory
                    ? "Known source gap is present in this database history; upgrade is blocked until reconciled."
                    : "Known source gap is not in this database history; visible for audit and does not block this fresh schema.";
            }
            else if (blocksUpgrade)
            {
                note = "History contains a migration ID unavailable in the current assembly.";
            }

            return new MigrationStatus(id, status, hasSource, inHistory, blocksUpgrade, note);
        }).ToArray();
    }
}