using MigrationCheck.Models;

namespace MigrationCheck.Analysis;

public interface IMigrationAnalyzer
{
    IReadOnlyList<Finding> Analyze(string migrationName, IReadOnlyList<MigrationOperation> upOperations, IReadOnlyList<MigrationOperation> downOperations);
}
