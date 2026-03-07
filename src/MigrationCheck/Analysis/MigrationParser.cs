using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrationCheck.Models;

namespace MigrationCheck.Analysis;

public record ParsedMigration(
    IReadOnlyList<MigrationOperation> UpOperations,
    IReadOnlyList<MigrationOperation> DownOperations
);

public static class MigrationParser
{
    public static ParsedMigration Parse(string fileContent)
    {
        var tree = CSharpSyntaxTree.ParseText(fileContent);
        var root = tree.GetRoot();

        var migrationClass = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.BaseList?.Types
                .Any(t => t.ToString().Contains("Migration")) == true);

        if (migrationClass is null)
            return new ParsedMigration([], []);

        var upOps = ExtractOperations(tree, migrationClass, "Up");
        var downOps = ExtractOperations(tree, migrationClass, "Down");

        return new ParsedMigration(upOps, downOps);
    }

    private static IReadOnlyList<MigrationOperation> ExtractOperations(
        SyntaxTree tree,
        ClassDeclarationSyntax migrationClass,
        string methodName)
    {
        var method = migrationClass.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == methodName);

        if (method is null)
            return [];

        return method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(inv => inv.Expression is MemberAccessExpressionSyntax ma
                && ma.Expression.ToString() == "migrationBuilder")
            .Select(inv => CreateOperation(tree, inv))
            .ToList();
    }

    private static MigrationOperation CreateOperation(SyntaxTree tree, InvocationExpressionSyntax invocation)
    {
        var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
        var methodName = memberAccess.Name.Identifier.Text;
        var arguments = new Dictionary<string, string>();

        if (memberAccess.Name is GenericNameSyntax generic)
        {
            methodName = generic.Identifier.Text;
            arguments["genericType"] = generic.TypeArgumentList.Arguments.ToString();
        }

        var args = invocation.ArgumentList.Arguments;
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            var key = arg.NameColon?.Name.Identifier.Text ?? $"arg{i}";
            var value = StripQuotes(arg.Expression.ToString());
            arguments[key] = value;
        }

        var lineSpan = tree.GetLineSpan(invocation.Span);
        var lineNumber = lineSpan.StartLinePosition.Line + 1;

        return new MigrationOperation(methodName, arguments, lineNumber, invocation.ToString());
    }

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value[1..^1];
        return value;
    }
}
