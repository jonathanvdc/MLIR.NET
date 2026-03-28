namespace MLIR.Text;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Prints MLIR syntax and semantic modules back to text.
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
        var writer = new SyntaxWriter();
        writer.WriteModule(module);
        return writer.ToString();
    }

    /// <summary>
    /// Converts a semantic module to MLIR text, using custom assembly formats when available.
    /// </summary>
    /// <param name="module">The semantic module to print.</param>
    /// <returns>The printed MLIR text.</returns>
    public static string Print(Module module)
    {
        var writer = new SyntaxWriter();
        AppendSemanticModule(writer, module);
        return writer.ToString();
    }

    internal static void AppendSemanticModule(SyntaxWriter writer, Module module)
    {
        for (var i = 0; i < module.Operations.Count; i++)
        {
            AppendSemanticOperation(writer, module.Operations[i], 0, i > 0 ? "\n" : string.Empty);
        }

        writer.Write(module.Syntax.EndOfFileToken.LeadingTrivia);
    }

    internal static void AppendSemanticOperation(SyntaxWriter writer, Operation operation, int indentLevel, string defaultLeadingTrivia)
    {
        if (operation.Definition?.AssemblyFormat != null)
        {
            operation.Definition.AssemblyFormat.Print(
                operation,
                new OperationPrintingContext(writer, indentLevel, defaultLeadingTrivia));
            return;
        }

        AppendGenericSemanticOperation(writer, operation, indentLevel, defaultLeadingTrivia);
    }

    internal static void AppendGenericSemanticOperation(SyntaxWriter writer, Operation operation, int indentLevel, string defaultLeadingTrivia)
    {
        var regionIndex = 0;
        operation.Syntax.WriteTo(
            writer,
            indentLevel,
            defaultLeadingTrivia,
            (innerWriter, _, innerIndentLevel) =>
            {
                AppendSemanticRegion(innerWriter, operation.Regions[regionIndex], innerIndentLevel);
                regionIndex++;
            });
    }

    internal static void AppendSemanticRegion(SyntaxWriter writer, Region region, int indentLevel)
    {
        writer.WriteToken(region.Syntax.OpenBraceToken, " ");

        foreach (var block in region.Blocks)
        {
            var syntax = block.Syntax;
            var blockHasExplicitLabel = syntax.Label != "^entry" || syntax.Arguments.Count > 0;
            var blockIndentLevel = indentLevel + 1;

            if (blockHasExplicitLabel)
            {
                writer.WriteToken(syntax.LabelToken, "\n", blockIndentLevel);

                if (syntax.Arguments.OpenToken != null)
                {
                    writer.WriteToken(syntax.Arguments.OpenToken.Value, string.Empty);
                    for (var i = 0; i < syntax.Arguments.Count; i++)
                    {
                        if (i > 0)
                        {
                            writer.WriteToken(syntax.Arguments.SeparatorTokens[i - 1], string.Empty);
                        }

                        syntax.Arguments[i].WriteTo(writer, i > 0 ? " " : string.Empty);
                    }

                    writer.WriteToken(syntax.Arguments.CloseToken!.Value, string.Empty);
                }

                writer.WriteToken(syntax.ColonToken, string.Empty);
            }

            var operationIndentLevel = blockHasExplicitLabel ? indentLevel + 2 : indentLevel + 1;
            foreach (var operation in block.Operations)
            {
                AppendSemanticOperation(writer, operation, operationIndentLevel, "\n");
            }
        }

        writer.WriteToken(region.Syntax.CloseBraceToken, "\n", indentLevel);
    }
}
