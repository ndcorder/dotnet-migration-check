using MigrationCheck.Models;

namespace MigrationCheck.Analysis;

public class IndexAnalyzer : IMigrationAnalyzer
{
    private static readonly AnalysisRule MC005 = new("MC005", "MissingForeignKeyIndex", Severity.Warning, "A foreign key was added without a corresponding index, which may cause performance issues.");
    private static readonly AnalysisRule MC010 = new("MC010", "DropIndex", Severity.Warning, "An index is being dropped, which may affect query performance.");

    public IReadOnlyList<Finding> Analyze(string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations)
    {
        var findings = new List<Finding>();

        var indexedTableColumns = new HashSet<(string Table, string Column)>(StringTupleComparer.Instance);

        foreach (var op in upOperations)
        {
            if (op.MethodName == "CreateIndex")
            {
                op.Arguments.TryGetValue("table", out var table);
                if (table is null) continue;

                if (op.Arguments.TryGetValue("column", out var col))
                    indexedTableColumns.Add((table, col));

                if (op.Arguments.TryGetValue("columns", out var cols))
                {
                    foreach (var c in cols.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                        indexedTableColumns.Add((table, c));
                }
            }
        }

        foreach (var op in upOperations)
        {
            if (op.MethodName == "AddForeignKey")
            {
                op.Arguments.TryGetValue("table", out var table);
                if (table is null) continue;

                var fkColumns = new List<string>();
                if (op.Arguments.TryGetValue("column", out var col))
                    fkColumns.Add(col);
                if (op.Arguments.TryGetValue("columns", out var cols))
                    fkColumns.AddRange(cols.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

                foreach (var fkCol in fkColumns)
                {
                    if (!indexedTableColumns.Contains((table, fkCol)))
                    {
                        findings.Add(new Finding(MC005, migrationName, $"Foreign key on '{table}' column '{fkCol}' has no index", "Add an index on the foreign key column to improve join and lookup performance.", op.LineNumber));
                    }
                }
            }

            if (op.MethodName == "DropIndex")
            {
                op.Arguments.TryGetValue("name", out var name);
                findings.Add(new Finding(MC010, migrationName, $"Index '{name}' dropped", "Verify that the index is no longer needed for query performance.", op.LineNumber));
            }
        }

        return findings;
    }

    private class StringTupleComparer : IEqualityComparer<(string, string)>
    {
        public static readonly StringTupleComparer Instance = new();

        public bool Equals((string, string) x, (string, string) y) =>
            string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string, string) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2));
    }
}
