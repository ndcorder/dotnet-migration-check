using FluentAssertions;
using MigrationCheck.Analysis;
using MigrationCheck.Models;
using Xunit;

namespace MigrationCheck.Tests.Analysis;

public class IndexAnalyzerTests
{
    private readonly IndexAnalyzer _analyzer = new();

    private const string FkWithoutIndexCode = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class FkWithoutIndex : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.AddForeignKey(name: "FK_Orders_Users", table: "Orders", column: "UserId", principalTable: "Users", principalColumn: "Id");
                migrationBuilder.DropIndex(name: "IX_Orders_OldColumn", table: "Orders");
            }
            protected override void Down(MigrationBuilder migrationBuilder) { }
        }
        """;

    private const string FkWithIndexCode = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class FkWithIndex : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.CreateIndex(name: "IX_Orders_UserId", table: "Orders", column: "UserId");
                migrationBuilder.AddForeignKey(name: "FK_Orders_Users", table: "Orders", column: "UserId", principalTable: "Users", principalColumn: "Id");
            }
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropForeignKey(name: "FK_Orders_Users", table: "Orders");
                migrationBuilder.DropIndex(name: "IX_Orders_UserId", table: "Orders");
            }
        }
        """;

    [Fact]
    public void Analyze_FkWithoutIndex_FindsMC005AndMC010()
    {
        var parsed = MigrationParser.Parse(FkWithoutIndexCode);

        var findings = _analyzer.Analyze("TestMigration", parsed.UpOperations, parsed.DownOperations);

        findings.Should().HaveCount(2);
        findings.Select(f => f.Rule.Id).Should().Contain("MC005");
        findings.Select(f => f.Rule.Id).Should().Contain("MC010");
    }

    [Fact]
    public void Analyze_FkWithMatchingIndex_DoesNotFindMC005()
    {
        var parsed = MigrationParser.Parse(FkWithIndexCode);

        var findings = _analyzer.Analyze("TestMigration", parsed.UpOperations, parsed.DownOperations);

        findings.Select(f => f.Rule.Id).Should().NotContain("MC005");
    }
}
