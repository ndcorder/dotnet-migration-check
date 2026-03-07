namespace MigrationCheck.Models;

public record MigrationOperation(
    string MethodName,
    Dictionary<string, string> Arguments,
    int LineNumber,
    string RawText
);
