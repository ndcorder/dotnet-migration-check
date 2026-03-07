using FluentAssertions;
using MigrationCheck.Analysis;
using Xunit;

namespace MigrationCheck.Tests.Analysis;

public class OrderingAnalyzerTests
{
    [Fact]
    public void AnalyzeOrdering_SequentialTimestamps_ReturnsNoFindings()
    {
        var migrations = new List<(string FileName, string? TimestampPrefix)>
        {
            ("20250101120000_First.cs", "20250101120000"),
            ("20250201120000_Second.cs", "20250201120000"),
            ("20250301120000_Third.cs", "20250301120000")
        };

        var findings = OrderingAnalyzer.AnalyzeOrdering(migrations);
        findings.Should().BeEmpty();
    }

    [Fact]
    public void AnalyzeOrdering_DuplicateTimestamp_FindsMC011()
    {
        var migrations = new List<(string FileName, string? TimestampPrefix)>
        {
            ("20250101120000_First.cs", "20250101120000"),
            ("20250101120000_Second.cs", "20250101120000")
        };

        var findings = OrderingAnalyzer.AnalyzeOrdering(migrations);
        findings.Should().ContainSingle(f => f.Rule.Id == "MC011");
    }

    [Fact]
    public void AnalyzeOrdering_OutOfOrder_FindsMC011()
    {
        var migrations = new List<(string FileName, string? TimestampPrefix)>
        {
            ("20250301120000_Third.cs", "20250301120000"),
            ("20250101120000_First.cs", "20250101120000")
        };

        var findings = OrderingAnalyzer.AnalyzeOrdering(migrations);
        findings.Should().ContainSingle(f => f.Rule.Id == "MC011");
    }
}
