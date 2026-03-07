using Spectre.Console;
using MigrationCheck.Models;

namespace MigrationCheck.Formatters;

public class TableFormatter : IOutputFormatter
{
    public void Render(IReadOnlyList<Finding> findings, TextWriter output)
    {
        if (findings.Count == 0)
        {
            output.WriteLine("No findings.");
            return;
        }

        var sorted = findings
            .OrderByDescending(f => f.Rule.DefaultSeverity)
            .ThenBy(f => f.MigrationName, StringComparer.Ordinal)
            .ToList();

        if (output == Console.Out)
        {
            RenderSpectreTable(sorted);
        }
        else
        {
            RenderPlainText(sorted, output);
        }
    }

    private static void RenderSpectreTable(IReadOnlyList<Finding> findings)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);

        table.AddColumn(new TableColumn("Severity").Centered());
        table.AddColumn(new TableColumn("Rule"));
        table.AddColumn(new TableColumn("Migration"));
        table.AddColumn(new TableColumn("Message"));
        table.AddColumn(new TableColumn("Line").Centered());
        table.AddColumn(new TableColumn("Remediation"));

        foreach (var f in findings)
        {
            var severity = f.Rule.DefaultSeverity;
            var color = severity switch
            {
                Severity.Critical => "red",
                Severity.Error => "red",
                Severity.Warning => "yellow",
                Severity.Info => "blue",
                _ => "white"
            };

            table.AddRow(
                new Markup($"[{color}]{severity}[/]"),
                new Text(f.Rule.Id),
                new Text(f.MigrationName),
                new Text(f.Message),
                new Text(f.LineNumber?.ToString() ?? "-"),
                new Text(f.Remediation ?? "")
            );
        }

        AnsiConsole.Write(table);
    }

    private static void RenderPlainText(IReadOnlyList<Finding> findings, TextWriter output)
    {
        output.WriteLine($"{"Severity",-10} {"Rule",-8} {"Migration",-30} {"Message",-50} {"Line",-6} {"Remediation"}");
        output.WriteLine(new string('-', 130));

        foreach (var f in findings)
        {
            output.WriteLine($"{f.Rule.DefaultSeverity,-10} {f.Rule.Id,-8} {f.MigrationName,-30} {f.Message,-50} {(f.LineNumber?.ToString() ?? "-"),-6} {f.Remediation ?? ""}");
        }
    }
}
