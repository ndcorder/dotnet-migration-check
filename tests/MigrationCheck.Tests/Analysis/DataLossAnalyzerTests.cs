using FluentAssertions;
using MigrationCheck.Analysis;
using MigrationCheck.Models;
using Xunit;

namespace MigrationCheck.Tests.Analysis;

public class DataLossAnalyzerTests
{
    private readonly DataLossAnalyzer _analyzer = new();

    private const string DangerousCode = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class DangerousMigration : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropTable(name: "OldUsers");
                migrationBuilder.DropColumn(name: "LegacyEmail", table: "Users");
                migrationBuilder.DropPrimaryKey(name: "PK_Users", table: "Users");
                migrationBuilder.AddPrimaryKey(name: "PK_Users_New", table: "Users", column: "NewId");
            }
            protected override void Down(MigrationBuilder migrationBuilder) { }
        }
        """;

    private const string SafeCode = """
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
    public void Analyze_DangerousMigration_FindsFourIssues()
    {
        var parsed = MigrationParser.Parse(DangerousCode);

        var findings = _analyzer.Analyze("TestMigration", parsed.UpOperations, parsed.DownOperations);

        findings.Should().HaveCount(4);
        findings.Select(f => f.Rule.Id).Should().BeEquivalentTo(["MC001", "MC002", "MC012", "MC012"]);
    }

    [Fact]
    public void Analyze_DangerousMigration_HasCorrectSeverities()
    {
        var parsed = MigrationParser.Parse(DangerousCode);

        var findings = _analyzer.Analyze("TestMigration", parsed.UpOperations, parsed.DownOperations);

        findings.Where(f => f.Rule.Id == "MC001").Should().AllSatisfy(f => f.Rule.DefaultSeverity.Should().Be(Severity.Critical));
        findings.Where(f => f.Rule.Id == "MC002").Should().AllSatisfy(f => f.Rule.DefaultSeverity.Should().Be(Severity.Error));
        findings.Where(f => f.Rule.Id == "MC012").Should().AllSatisfy(f => f.Rule.DefaultSeverity.Should().Be(Severity.Critical));
    }

    [Fact]
    public void Analyze_SafeMigration_ReturnsNoFindings()
    {
        var parsed = MigrationParser.Parse(SafeCode);

        var findings = _analyzer.Analyze("TestMigration", parsed.UpOperations, parsed.DownOperations);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_AlterColumnWithType_FindsMC003()
    {
        var code = """
            using Microsoft.EntityFrameworkCore.Migrations;
            public partial class NarrowColumn : Migration
            {
                protected override void Up(MigrationBuilder migrationBuilder)
                {
                    migrationBuilder.AlterColumn<string>(name: "Description", table: "Products", type: "nvarchar(100)", nullable: false);
                }
                protected override void Down(MigrationBuilder migrationBuilder) { }
            }
            """;
        var parsed = MigrationParser.Parse(code);
        var findings = _analyzer.Analyze("NarrowColumn", parsed.UpOperations, parsed.DownOperations);
        findings.Should().ContainSingle(f => f.Rule.Id == "MC003");
        findings.First().Rule.DefaultSeverity.Should().Be(Severity.Error);
    }
}
