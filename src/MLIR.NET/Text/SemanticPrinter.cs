namespace MLIR.Text;

using MLIR.Semantics;

/// <summary>
/// Prints semantic MLIR modules, optionally using dialect-specific custom assembly formats.
/// </summary>
public sealed class SemanticPrinter
{
    /// <summary>
    /// Converts a semantic module to MLIR text.
    /// </summary>
    /// <param name="module">The semantic module to print.</param>
    /// <returns>The printed MLIR text.</returns>
    public static string Print(Module module)
    {
        var printer = new SemanticPrinter();
        var writer = new SyntaxWriter();
        printer.AppendModule(writer, module);
        return writer.ToString();
    }

    internal void AppendModule(SyntaxWriter writer, Module module)
    {
        for (var i = 0; i < module.Operations.Count; i++)
        {
            AppendOperation(writer, module.Operations[i], 0, i > 0 ? "\n" : string.Empty);
        }

        writer.Write(module.Syntax.EndOfFileToken.LeadingTrivia);
    }

    internal void AppendOperation(SyntaxWriter writer, Operation operation, int indentLevel, string defaultLeadingTrivia)
    {
        if (operation.Definition?.AssemblyFormat != null)
        {
            operation.Definition.AssemblyFormat.Print(
                operation,
                new OperationPrintingContext(this, writer, indentLevel, defaultLeadingTrivia));
            return;
        }

        AppendGenericOperation(writer, operation, indentLevel, defaultLeadingTrivia);
    }

    internal void AppendGenericOperation(SyntaxWriter writer, Operation operation, int indentLevel, string defaultLeadingTrivia)
    {
        var regionIndex = 0;
        operation.Syntax.WriteTo(
            writer,
            indentLevel,
            defaultLeadingTrivia,
            (innerWriter, _, innerIndentLevel) =>
            {
                AppendRegion(innerWriter, operation.Regions[regionIndex], innerIndentLevel);
                regionIndex++;
            });
    }

    internal void AppendGenericOperation(SyntaxWriter writer, Syntax.OperationSyntax operation, int indentLevel, string defaultLeadingTrivia)
    {
        writer.WriteOperation(operation, indentLevel, defaultLeadingTrivia);
    }

    internal void AppendRegion(SyntaxWriter writer, Region region, int indentLevel)
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
                AppendOperation(writer, operation, operationIndentLevel, "\n");
            }
        }

        writer.WriteToken(region.Syntax.CloseBraceToken, "\n", indentLevel);
    }
}
