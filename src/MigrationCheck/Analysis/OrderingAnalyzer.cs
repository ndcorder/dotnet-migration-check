using MigrationCheck.Models;

namespace MigrationCheck.Analysis;

public class OrderingAnalyzer : IMigrationAnalyzer
{
    private static readonly AnalysisRule MC011 = new("MC011", "MigrationOrderingIssue", Severity.Warning, "Migration timestamp ordering is inconsistent, which may cause deployment issues.");

    public IReadOnlyList<Finding> Analyze(string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations)
    {
        return [];
    }

    public static IReadOnlyList<Finding> AnalyzeOrdering(IReadOnlyList<(string FileName, string? TimestampPrefix)> migrations)
    {
        var findings = new List<Finding>();
        var seen = new Dictionary<string, string>();

        string? previousTimestamp = null;
        string? previousFileName = null;

        foreach (var (fileName, timestamp) in migrations)
        {
            if (timestamp is null)
                continue;

            if (seen.TryGetValue(timestamp, out var existing))
            {
                findings.Add(new Finding(MC011, fileName, $"Duplicate timestamp prefix '{timestamp}' shared with '{existing}'", "Regenerate one of the migrations to ensure unique timestamps.", null));
            }
            else
            {
                seen[timestamp] = fileName;
            }

            if (previousTimestamp is not null &&
                string.Compare(timestamp, previousTimestamp, StringComparison.Ordinal) < 0)
            {
                findings.Add(new Finding(MC011, fileName, $"Timestamp '{timestamp}' is earlier than previous migration '{previousFileName}' ({previousTimestamp})", "Reorder or regenerate migrations to maintain chronological order.", null));
            }

            previousTimestamp = timestamp;
            previousFileName = fileName;
        }

        return findings;
    }
}
