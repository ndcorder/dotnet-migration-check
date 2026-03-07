using FluentAssertions;
using MigrationCheck.Analysis;
using MigrationCheck.Models;
using Xunit;

namespace MigrationCheck.Tests.Analysis;

public class RenameDetectorTests
{
    private readonly RenameDetector _detector = new();

    private const string PossibleRenameCode = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class PossibleRename : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropColumn(name: "EmailAddress", table: "Users");
                migrationBuilder.AddColumn<string>(name: "Email", table: "Users", nullable: true);
            }
            protected override void Down(MigrationBuilder migrationBuilder) { }
        }
        """;

    private const string DifferentTablesCode = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class DifferentTables : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropColumn(name: "Name", table: "Users");
                migrationBuilder.AddColumn<string>(name: "Name", table: "Products", nullable: true);
            }
            protected override void Down(MigrationBuilder migrationBuilder) { }
        }
        """;

    [Fact]
    public void Analyze_DropAndAddOnSameTable_FindsMC008()
    {
        var parsed = MigrationParser.Parse(PossibleRenameCode);

        var findings = _detector.Analyze("TestMigration", parsed.UpOperations, parsed.DownOperations);

        findings.Should().ContainSingle();
        findings[0].Rule.Id.Should().Be("MC008");
    }

    [Fact]
    public void Analyze_DropAndAddOnDifferentTables_DoesNotFindMC008()
    {
        var parsed = MigrationParser.Parse(DifferentTablesCode);

        var findings = _detector.Analyze("TestMigration", parsed.UpOperations, parsed.DownOperations);

        findings.Where(f => f.Rule.Id == "MC008").Should().BeEmpty();
    }
}
