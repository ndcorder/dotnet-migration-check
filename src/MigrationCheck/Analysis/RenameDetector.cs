using MigrationCheck.Models;

namespace MigrationCheck.Analysis;

public class RenameDetector : IMigrationAnalyzer
{
    private static readonly AnalysisRule MC008 = new("MC008", "PossibleRename", Severity.Warning, "A drop and add on the same table may indicate a rename that should use RenameColumn.");

    public IReadOnlyList<Finding> Analyze(string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations)
    {
        var findings = new List<Finding>();

        var drops = upOperations
            .Where(op => op.MethodName == "DropColumn")
            .Select(op => (Op: op, Table: op.Arguments.GetValueOrDefault("table"), Name: op.Arguments.GetValueOrDefault("name")))
            .Where(x => x.Table is not null && x.Name is not null)
            .ToList();

        var adds = upOperations
            .Where(op => op.MethodName == "AddColumn")
            .Select(op => (Op: op, Table: op.Arguments.GetValueOrDefault("table"), Name: op.Arguments.GetValueOrDefault("name")))
            .Where(x => x.Table is not null && x.Name is not null)
            .ToList();

        foreach (var drop in drops)
        {
            foreach (var add in adds)
            {
                if (!string.Equals(drop.Table, add.Table, StringComparison.OrdinalIgnoreCase))
                    continue;

                var dropType = drop.Op.Arguments.GetValueOrDefault("type") ?? drop.Op.Arguments.GetValueOrDefault("genericType");
                var addType = add.Op.Arguments.GetValueOrDefault("type") ?? add.Op.Arguments.GetValueOrDefault("genericType");

                var typesCompatible = dropType is null || addType is null ||
                                     string.Equals(dropType, addType, StringComparison.OrdinalIgnoreCase);

                if (typesCompatible)
                {
                    findings.Add(new Finding(MC008, migrationName, $"Possible rename: '{drop.Name}' → '{add.Name}' on '{drop.Table}'. Consider using RenameColumn instead.", "Use migrationBuilder.RenameColumn() to preserve data.", drop.Op.LineNumber));
                }
            }
        }

        return findings;
    }
}
