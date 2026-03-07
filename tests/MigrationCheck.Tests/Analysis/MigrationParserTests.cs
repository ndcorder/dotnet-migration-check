using FluentAssertions;
using MigrationCheck.Analysis;
using MigrationCheck.Models;
using Xunit;

namespace MigrationCheck.Tests.Analysis;

public class MigrationParserTests
{
    private const string Code = """
        using Microsoft.EntityFrameworkCore.Migrations;

        public partial class AddUsersTable : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.CreateTable(
                    name: "Users",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false),
                        Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                    });
                migrationBuilder.DropColumn(name: "OldField", table: "Users");
                migrationBuilder.AddColumn<string>(name: "FullName", table: "Users", nullable: true);
            }

            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropTable(name: "Users");
            }
        }
        """;

    [Fact]
    public void Parse_ReturnsCorrectNumberOfUpOperations()
    {
        var parsed = MigrationParser.Parse(Code);

        parsed.UpOperations.Should().HaveCount(3);
        parsed.UpOperations.Select(o => o.MethodName)
            .Should().ContainInOrder("CreateTable", "DropColumn", "AddColumn");
    }

    [Fact]
    public void Parse_ReturnsCorrectNumberOfDownOperations()
    {
        var parsed = MigrationParser.Parse(Code);

        parsed.DownOperations.Should().HaveCount(1);
        parsed.DownOperations[0].MethodName.Should().Be("DropTable");
    }

    [Fact]
    public void Parse_DropColumnHasCorrectArguments()
    {
        var parsed = MigrationParser.Parse(Code);

        var dropColumn = parsed.UpOperations.First(o => o.MethodName == "DropColumn");
        dropColumn.Arguments["name"].Should().Be("OldField");
        dropColumn.Arguments["table"].Should().Be("Users");
    }

    [Fact]
    public void Parse_AddColumnHasGenericTypeArgument()
    {
        var parsed = MigrationParser.Parse(Code);

        var addColumn = parsed.UpOperations.First(o => o.MethodName == "AddColumn");
        addColumn.Arguments.Should().ContainKey("genericType");
        addColumn.Arguments["genericType"].Should().Contain("string");
    }

    [Fact]
    public void Parse_OperationsHavePositiveLineNumbers()
    {
        var parsed = MigrationParser.Parse(Code);

        parsed.UpOperations.Should().AllSatisfy(op => op.LineNumber.Should().BeGreaterThan(0));
        parsed.DownOperations.Should().AllSatisfy(op => op.LineNumber.Should().BeGreaterThan(0));
    }
}
