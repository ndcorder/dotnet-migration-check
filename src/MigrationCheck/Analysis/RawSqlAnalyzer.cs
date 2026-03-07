using MigrationCheck.Models;

namespace MigrationCheck.Analysis;

public class RawSqlAnalyzer : IMigrationAnalyzer
{
    private static readonly AnalysisRule MC007 = new("MC007", "RawSqlDetected", Severity.Info, "Raw SQL is used in the migration, which may not be portable across database providers.");

    public IReadOnlyList<Finding> Analyze(string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations)
    {
        var findings = new List<Finding>();

        foreach (var op in upOperations)
        {
            if (op.MethodName == "Sql")
            {
                var preview = op.RawText.Length > 80 ? op.RawText[..80] + "..." : op.RawText;
                findings.Add(new Finding(MC007, migrationName, $"Raw SQL detected: {preview}", "Consider using EF Core migration operations instead of raw SQL for portability.", op.LineNumber));
            }
        }

        return findings;
    }
}
