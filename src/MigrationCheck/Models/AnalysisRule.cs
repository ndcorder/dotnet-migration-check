namespace MigrationCheck.Models;

public record AnalysisRule(
    string Id,
    string Name,
    Severity DefaultSeverity,
    string Description
);
