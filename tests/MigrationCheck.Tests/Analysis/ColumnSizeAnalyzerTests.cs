using FluentAssertions;
using MigrationCheck.Analysis;
using MigrationCheck.Models;
using Xunit;

namespace MigrationCheck.Tests.Analysis;

public class ColumnSizeAnalyzerTests
{
    private readonly ColumnSizeAnalyzer _analyzer = new();

    private const string NonNullWithoutDefault = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class AddRequiredColumn : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.AddColumn<string>(name: "Status", table: "Orders", nullable: false);
            }
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropColumn(name: "Status", table: "Orders");
            }
        }
        """;

    private const string NonNullWithDefault = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class AddColumnWithDefault : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.AddColumn<string>(name: "Status", table: "Orders", nullable: false, defaultValue: "Pending");
            }
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropColumn(name: "Status", table: "Orders");
            }
        }
        """;

    private const string StringWithoutMaxLength = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class AddDescriptionColumn : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.AddColumn<string>(name: "Description", table: "Products", nullable: true);
            }
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropColumn(name: "Description", table: "Products");
            }
        }
        """;

    private const string StringWithMaxLength = """
        using Microsoft.EntityFrameworkCore.Migrations;
        public partial class AddNameColumn : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.AddColumn<string>(name: "Name", table: "Products", nullable: true, maxLength: 256);
            }
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropColumn(name: "Name", table: "Products");
            }
        }
        """;

    [Fact]
    public void Analyze_NonNullWithoutDefault_FindsMC004()
    {
        var parsed = MigrationParser.Parse(NonNullWithoutDefault);
        var findings = _analyzer.Analyze("AddRequiredColumn", parsed.UpOperations, parsed.DownOperations);
        findings.Should().Contain(f => f.Rule.Id == "MC004");
    }

    [Fact]
    public void Analyze_NonNullWithDefault_DoesNotFindMC004()
    {
        var parsed = MigrationParser.Parse(NonNullWithDefault);
        var findings = _analyzer.Analyze("AddColumnWithDefault", parsed.UpOperations, parsed.DownOperations);
        findings.Should().NotContain(f => f.Rule.Id == "MC004");
    }

    [Fact]
    public void Analyze_StringWithoutMaxLength_FindsMC009()
    {
        var parsed = MigrationParser.Parse(StringWithoutMaxLength);
        var findings = _analyzer.Analyze("AddDescriptionColumn", parsed.UpOperations, parsed.DownOperations);
        findings.Should().Contain(f => f.Rule.Id == "MC009");
    }

    [Fact]
    public void Analyze_StringWithMaxLength_DoesNotFindMC009()
    {
        var parsed = MigrationParser.Parse(StringWithMaxLength);
        var findings = _analyzer.Analyze("AddNameColumn", parsed.UpOperations, parsed.DownOperations);
        findings.Should().NotContain(f => f.Rule.Id == "MC009");
    }
}
