using FluentAssertions;
using MigrationCheck.Analysis;
using MigrationCheck.Models;
using Xunit;

namespace MigrationCheck.Tests.Analysis;

public class ReversibilityAnalyzerTests
{
    private readonly ReversibilityAnalyzer _analyzer = new();

    private const string EmptyDownCode = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class DangerousMigration : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropTable(name: "OldUsers");
            }
            protected override void Down(MigrationBuilder migrationBuilder) { }
        }
        """;

    private const string HasDownCode = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class SafeMigration : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.AddColumn<string>(name: "Nickname", table: "Users", nullable: true);
            }
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropColumn(name: "Nickname", table: "Users");
            }
        }
        """;

    [Fact]
    public void Analyze_EmptyDown_FindsMC006()
    {
        var parsed = MigrationParser.Parse(EmptyDownCode);

        var findings = _analyzer.Analyze("TestMigration", parsed.UpOperations, parsed.DownOperations);

        findings.Should().ContainSingle();
        findings[0].Rule.Id.Should().Be("MC006");
    }

    [Fact]
    public void Analyze_NonEmptyDown_DoesNotFindMC006()
    {
        var parsed = MigrationParser.Parse(HasDownCode);

        var findings = _analyzer.Analyze("TestMigration", parsed.UpOperations, parsed.DownOperations);

        findings.Should().BeEmpty();
    }
}
