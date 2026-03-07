using MigrationCheck.Models;

namespace MigrationCheck.Formatters;

public interface IOutputFormatter
{
    void Render(IReadOnlyList<Finding> findings, TextWriter output);
}
