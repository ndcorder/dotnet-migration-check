namespace MigrationCheck.Models;

public record Finding(
    AnalysisRule Rule,
    string MigrationName,
    string Message,
    string? Remediation,
    int? LineNumber
);
