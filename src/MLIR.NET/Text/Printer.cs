namespace MLIR.Text;

using System.Text;
using MLIR.Syntax;

/// <summary>
/// Prints generic MLIR syntax back to its textual representation.
/// </summary>
public sealed class Printer
{
    /// <summary>
    /// Converts a module syntax tree to MLIR text.
    /// </summary>
    /// <param name="module">The module to print.</param>
    /// <returns>The printed MLIR text.</returns>
    public static string Print(ModuleSyntax module)
    {
        var builder = new StringBuilder();
        StructuralPrinter.AppendModule(builder, module.Operations, module.EndOfFileToken, AppendOperation);
        return builder.ToString();
    }

    internal static void AppendOperation(StringBuilder builder, OperationSyntax operation, int indentLevel, string defaultLeadingTrivia)
    {
        AppendOperation(builder, operation, indentLevel, defaultLeadingTrivia, static (innerBuilder, region, _, innerIndentLevel) =>
            StructuralPrinter.AppendRegion(innerBuilder, region, region.Blocks, innerIndentLevel, static block => block, static block => block.Operations, AppendOperation));
    }

    internal static void AppendOperation(
        StringBuilder builder,
        OperationSyntax operation,
        int indentLevel,
        string defaultLeadingTrivia,
        Action<StringBuilder, RegionSyntax, int, int> appendRegion)
    {
        AppendOperationPrefix(builder, operation, indentLevel, defaultLeadingTrivia);
        operation.Body.Print(new OperationBodyPrintingContext(builder, indentLevel, " ", appendRegion));
    }

    private static void AppendOperationPrefix(StringBuilder builder, OperationSyntax operation, int indentLevel, string defaultLeadingTrivia)
    {
        if (operation.ResultTokens.Count > 0)
        {
            for (var i = 0; i < operation.ResultTokens.Count; i++)
            {
                if (i > 0)
                {
                    PrintWriter.AppendToken(builder, operation.ResultCommaTokens[i - 1], string.Empty);
                }

                PrintWriter.AppendToken(builder, operation.ResultTokens[i], i > 0 ? " " : defaultLeadingTrivia, i == 0 ? indentLevel : null);
            }

            PrintWriter.AppendToken(builder, operation.EqualsToken!.Value, " ");
            PrintWriter.AppendToken(builder, operation.NameToken, " ");
            return;
        }

        PrintWriter.AppendToken(builder, operation.NameToken, defaultLeadingTrivia, indentLevel);
    }
}
