using System.Text.Json;
using System.Text.Json.Serialization;
using MigrationCheck.Models;

namespace MigrationCheck.Formatters;

public class JsonFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public void Render(IReadOnlyList<Finding> findings, TextWriter output)
    {
        var items = findings.Select(f => new
        {
            Rule = f.Rule.Id,
            Severity = f.Rule.DefaultSeverity.ToString(),
            Migration = f.MigrationName,
            f.Message,
            Line = f.LineNumber,
            f.Remediation
        });

        var json = JsonSerializer.Serialize(items, Options);
        output.Write(json);
        output.WriteLine();
    }
}
