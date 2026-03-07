using MigrationCheck.Models;

namespace MigrationCheck.Analysis;

public class ColumnSizeAnalyzer : IMigrationAnalyzer
{
    private static readonly AnalysisRule MC004 = new("MC004", "NonNullableWithoutDefault", Severity.Warning, "A non-nullable column is added without a default value, which will fail on non-empty tables.");
    private static readonly AnalysisRule MC009 = new("MC009", "UnboundedStringColumn", Severity.Info, "A string column has no max length, which may use nvarchar(max) and affect performance.");

    public IReadOnlyList<Finding> Analyze(string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations)
    {
        var findings = new List<Finding>();

        foreach (var op in upOperations)
        {
            if (op.MethodName != "AddColumn")
                continue;

            op.Arguments.TryGetValue("name", out var name);
            op.Arguments.TryGetValue("table", out var table);

            if (op.Arguments.TryGetValue("nullable", out var nullable) &&
                string.Equals(nullable, "false", StringComparison.OrdinalIgnoreCase) &&
                !op.Arguments.ContainsKey("defaultValue") &&
                !op.Arguments.ContainsKey("defaultValueSql"))
            {
                findings.Add(new Finding(MC004, migrationName, $"Non-nullable column '{name}' added to '{table}' without default value", "Add a default value or make the column nullable to avoid failures on existing rows.", op.LineNumber));
            }

            if (IsStringLike(op) && !op.Arguments.ContainsKey("maxLength"))
            {
                findings.Add(new Finding(MC009, migrationName, $"Column '{name}' on '{table}' has no max length specified", "Consider setting a maxLength to avoid unbounded nvarchar(max) columns.", op.LineNumber));
            }
        }

        return findings;
    }

    private static bool IsStringLike(MigrationOperation op)
    {
        if (op.Arguments.TryGetValue("genericType", out var genericType) &&
            genericType.Contains("string", StringComparison.OrdinalIgnoreCase))
            return true;

        if (op.Arguments.TryGetValue("type", out var type))
        {
            if (type.Contains("varchar", StringComparison.OrdinalIgnoreCase) ||
                type.Contains("text", StringComparison.OrdinalIgnoreCase) ||
                type.Contains("nvarchar", StringComparison.OrdinalIgnoreCase) ||
                type.Contains("(max)", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
