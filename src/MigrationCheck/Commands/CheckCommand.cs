using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MigrationCheck.Analysis;
using MigrationCheck.Discovery;
using MigrationCheck.Formatters;
using MigrationCheck.Models;

namespace MigrationCheck.Commands;

public sealed class CheckCommand : Command<CheckCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[PATH]")]
        [Description("Path to the project or solution directory")]
        public string? Path { get; init; }

        [CommandOption("--format")]
        [DefaultValue("table")]
        [Description("Output format: table or json")]
        public string Format { get; init; } = "table";

        [CommandOption("--severity")]
        [DefaultValue("warning")]
        [Description("Minimum severity to report: info, warning, error, or critical")]
        public string Severity { get; init; } = "warning";

        [CommandOption("--last")]
        [Description("Only analyze the last N migrations")]
        public int? Last { get; init; }

        [CommandOption("--no-reversibility")]
        [Description("Skip reversibility checks")]
        public bool NoReversibility { get; init; }

        [CommandOption("--ci")]
        [Description("CI mode: exit with code 1 if any finding has severity >= Error")]
        public bool Ci { get; init; }
    }

    protected override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken)
    {
        if (settings.Ci)
            AnsiConsole.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;

        if (settings.Last is <= 0)
        {
            AnsiConsole.MarkupLine("[red]--last must be a positive number.[/]");
            return 1;
        }

        var minSeverity = ParseSeverity(settings.Severity);
        if (minSeverity is null)
        {
            AnsiConsole.MarkupLine($"[red]Unknown severity:[/] {Markup.Escape(settings.Severity)}. Use info, warning, error, or critical.");
            return 1;
        }

        var path = settings.Path ?? Directory.GetCurrentDirectory();

        if (!Directory.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]Directory not found:[/] {Markup.Escape(path)}");
            return 1;
        }

        var migrations = MigrationDiscovery.Discover(path, settings.Last);

        if (migrations.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No migration files found.[/]");
            return 0;
        }

        var analyzers = BuildAnalyzers(settings);
        var allFindings = new List<Finding>();

        foreach (var migration in migrations)
        {
            try
            {
                var content = File.ReadAllText(migration.FilePath);
                var parsed = MigrationParser.Parse(content);
                var migrationName = System.IO.Path.GetFileNameWithoutExtension(migration.FileName);

                foreach (var analyzer in analyzers)
                {
                    var findings = analyzer.Analyze(migrationName, parsed.UpOperations, parsed.DownOperations);
                    allFindings.AddRange(findings);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Could not read {Markup.Escape(migration.FileName)}: {Markup.Escape(ex.Message)}");
            }
        }

        var orderingFindings = OrderingAnalyzer.AnalyzeOrdering(
            migrations.Select(m => (m.FileName, m.TimestampPrefix)).ToList());
        allFindings.AddRange(orderingFindings);

        var filtered = allFindings
            .Where(f => f.Rule.DefaultSeverity >= minSeverity.Value)
            .ToList();

        var formatter = CreateFormatter(settings.Format);
        formatter.Render(filtered, Console.Out);

        if (settings.Ci && filtered.Any(f => f.Rule.DefaultSeverity >= Models.Severity.Error))
            return 1;

        return 0;
    }

    private static List<IMigrationAnalyzer> BuildAnalyzers(Settings settings)
    {
        var analyzers = new List<IMigrationAnalyzer>
        {
            new DataLossAnalyzer(),
            new IndexAnalyzer(),
            new RawSqlAnalyzer(),
            new RenameDetector(),
            new ColumnSizeAnalyzer()
        };

        if (!settings.NoReversibility)
            analyzers.Add(new ReversibilityAnalyzer());

        return analyzers;
    }

    private static Severity? ParseSeverity(string value) =>
        value.ToLowerInvariant() switch
        {
            "info" => Models.Severity.Info,
            "warning" => Models.Severity.Warning,
            "error" => Models.Severity.Error,
            "critical" => Models.Severity.Critical,
            _ => null
        };

    private static IOutputFormatter CreateFormatter(string format) =>
        format.ToLowerInvariant() switch
        {
            "json" => new JsonFormatter(),
            _ => new TableFormatter()
        };
}
