using FluentAssertions;
using MigrationCheck.Analysis;
using MigrationCheck.Models;
using Xunit;

namespace MigrationCheck.Tests.Analysis;

public class RawSqlAnalyzerTests
{
    private readonly RawSqlAnalyzer _analyzer = new();

    private const string MigrationWithSql = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class SeedData : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.Sql("INSERT INTO Settings (Key, Value) VALUES ('Version', '1.0')");
            }
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.Sql("DELETE FROM Settings WHERE Key = 'Version'");
            }
        }
        """;

    private const string MigrationWithoutSql = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class SafeMigration : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.AddColumn<string>(name: "Name", table: "Users", nullable: true);
            }
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropColumn(name: "Name", table: "Users");
            }
        }
        """;

    [Fact]
    public void Analyze_MigrationWithSql_FindsMC007()
    {
        var parsed = MigrationParser.Parse(MigrationWithSql);
        var findings = _analyzer.Analyze("SeedData", parsed.UpOperations, parsed.DownOperations);
        findings.Should().ContainSingle(f => f.Rule.Id == "MC007");
    }

    [Fact]
    public void Analyze_MigrationWithoutSql_DoesNotFindMC007()
    {
        var parsed = MigrationParser.Parse(MigrationWithoutSql);
        var findings = _analyzer.Analyze("SafeMigration", parsed.UpOperations, parsed.DownOperations);
        findings.Should().NotContain(f => f.Rule.Id == "MC007");
    }
}
