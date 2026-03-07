using MigrationCheck.Models;

namespace MigrationCheck.Analysis;

public class ReversibilityAnalyzer : IMigrationAnalyzer
{
    private static readonly AnalysisRule MC006 = new("MC006", "IrreversibleMigration", Severity.Warning, "The Down() method is empty, making the migration irreversible.");

    public IReadOnlyList<Finding> Analyze(string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations)
    {
        if (downOperations.Count == 0)
        {
            return [new Finding(MC006, migrationName, "Down() method is empty — migration is not reversible", "Implement the Down() method to allow rolling back this migration.", null)];
        }

        return [];
    }
}
