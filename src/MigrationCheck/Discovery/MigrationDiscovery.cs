using System.Text.RegularExpressions;

namespace MigrationCheck.Discovery;

public static partial class MigrationDiscovery
{
    [GeneratedRegex(@"^(\d{14})_")]
    private static partial Regex TimestampRegex();

    public static IReadOnlyList<MigrationFile> Discover(string basePath, int? lastN)
    {
        var migrationsDir = FindMigrationsFolder(basePath);
        if (migrationsDir is null)
            return [];

        var files = Directory.EnumerateFiles(migrationsDir, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                return !name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                    && !name.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
            .Select(f =>
            {
                var fileName = Path.GetFileName(f);
                var match = TimestampRegex().Match(fileName);
                var timestamp = match.Success ? match.Groups[1].Value : null;
                return new MigrationFile(f, fileName, timestamp);
            })
            .ToList();

        if (lastN.HasValue)
            files = files.TakeLast(lastN.Value).ToList();

        return files;
    }

    private static readonly HashSet<string> SkipDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", ".git", ".vs", ".idea"
    };

    private static string? FindMigrationsFolder(string basePath)
    {
        if (Path.GetFileName(basePath).Equals("Migrations", StringComparison.OrdinalIgnoreCase))
            return basePath;

        var queue = new Queue<string>();
        queue.Enqueue(basePath);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var dir in Directory.EnumerateDirectories(current))
            {
                var dirName = Path.GetFileName(dir);
                if (dirName.Equals("Migrations", StringComparison.OrdinalIgnoreCase))
                    return dir;

                if (!SkipDirs.Contains(dirName))
                    queue.Enqueue(dir);
            }
        }

        return null;
    }
}
