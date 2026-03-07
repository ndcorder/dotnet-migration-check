namespace MigrationCheck.Discovery;

public record MigrationFile(
    string FilePath,
    string FileName,
    string? TimestampPrefix
);
