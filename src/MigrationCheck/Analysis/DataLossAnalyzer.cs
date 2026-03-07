using MigrationCheck.Models;

namespace MigrationCheck.Analysis;

public class DataLossAnalyzer : IMigrationAnalyzer
{
    private static readonly AnalysisRule MC001 = new("MC001", "DropTable", Severity.Critical, "A table is being dropped, which causes irreversible data loss.");
    private static readonly AnalysisRule MC002 = new("MC002", "DropColumn", Severity.Error, "A column is being dropped, which causes data loss.");
    private static readonly AnalysisRule MC003 = new("MC003", "ColumnTypeNarrowing", Severity.Error, "A column type change may narrow the data type, risking data truncation or loss.");
    private static readonly AnalysisRule MC012 = new("MC012", "PrimaryKeyAlteration", Severity.Critical, "Primary key is being altered, which can cause data loss and requires careful migration.");

    public IReadOnlyList<Finding> Analyze(string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations)
    {
        var findings = new List<Finding>();

        foreach (var op in upOperations)
        {
            switch (op.MethodName)
            {
                case "DropTable":
                    {
                        op.Arguments.TryGetValue("name", out var table);
                        findings.Add(new Finding(MC001, migrationName, $"Table '{table}' is being dropped", "Ensure data is backed up or migrated before dropping the table.", op.LineNumber));
                        break;
                    }
                case "DropColumn":
                    {
                        op.Arguments.TryGetValue("name", out var name);
                        op.Arguments.TryGetValue("table", out var table);
                        findings.Add(new Finding(MC002, migrationName, $"Column '{name}' dropped from '{table}'", "Back up data before dropping the column, or consider renaming instead.", op.LineNumber));
                        break;
                    }
                case "AlterColumn":
                    {
                        if (op.Arguments.ContainsKey("type"))
                        {
                            op.Arguments.TryGetValue("table", out var table);
                            op.Arguments.TryGetValue("name", out var name);
                            findings.Add(new Finding(MC003, migrationName, $"Column type changed on '{table}.{name}'", "Verify the type change does not truncate existing data.", op.LineNumber));
                        }
                        break;
                    }
                case "DropPrimaryKey":
                case "AddPrimaryKey":
                    {
                        op.Arguments.TryGetValue("table", out var table);
                        findings.Add(new Finding(MC012, migrationName, $"Primary key alteration on '{table}'", "Test primary key changes against production data volumes.", op.LineNumber));
                        break;
                    }
            }
        }

        return findings;
    }
}
