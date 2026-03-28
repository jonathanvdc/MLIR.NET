namespace MLIR.Text;

using System.Text;
using MLIR.Syntax;

/// <summary>
/// Prints generic MLIR syntax back to its textual representation.
/// </summary>
public sealed class MlirPrinter
{
    /// <summary>
    /// Converts a module syntax tree to MLIR text.
    /// </summary>
    /// <param name="module">The module to print.</param>
    /// <returns>The printed MLIR text.</returns>
    public static string Print(ModuleSyntax module)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < module.Operations.Count; i++)
        {
            AppendOperation(builder, module.Operations[i], 0, i > 0 ? "\n" : string.Empty);
        }

        builder.Append(module.EndOfFileToken.LeadingTrivia);
        return builder.ToString();
    }

    private static void AppendOperation(StringBuilder builder, OperationSyntax operation, int indentLevel, string defaultLeadingTrivia)
    {
        if (operation.ResultTokens.Count > 0)
        {
            for (var i = 0; i < operation.ResultTokens.Count; i++)
            {
                if (i > 0)
                {
                    AppendToken(builder, operation.ResultCommaTokens[i - 1], string.Empty);
                }

                AppendToken(builder, operation.ResultTokens[i], i > 0 ? " " : defaultLeadingTrivia, i == 0 ? indentLevel : null);
            }

            AppendToken(builder, operation.EqualsToken!.Value, " ");
            AppendToken(builder, operation.NameToken, " ");
        }
        else
        {
            AppendToken(builder, operation.NameToken, defaultLeadingTrivia, indentLevel);
        }

        AppendToken(builder, operation.OperandList.OpenToken!.Value, string.Empty);
        for (var i = 0; i < operation.OperandList.Count; i++)
        {
            if (i > 0)
            {
                AppendToken(builder, operation.OperandList.SeparatorTokens[i - 1], string.Empty);
            }

            AppendToken(builder, operation.OperandList[i], i > 0 ? " " : string.Empty);
        }

        AppendToken(builder, operation.OperandList.CloseToken!.Value, string.Empty);

        if (operation.SuccessorList.OpenToken != null)
        {
            AppendToken(builder, operation.SuccessorList.OpenToken.Value, " ");
            for (var i = 0; i < operation.SuccessorList.Count; i++)
            {
                if (i > 0)
                {
                    AppendToken(builder, operation.SuccessorList.SeparatorTokens[i - 1], string.Empty);
                }

                AppendToken(builder, operation.SuccessorList[i], i > 0 ? " " : string.Empty);
            }

            AppendToken(builder, operation.SuccessorList.CloseToken!.Value, string.Empty);
        }

        foreach (var region in operation.Regions)
        {
            AppendRegion(builder, region, indentLevel);
        }

        if (operation.Attributes.OpenToken != null)
        {
            AppendToken(builder, operation.Attributes.OpenToken.Value, " ");
            for (var i = 0; i < operation.Attributes.Count; i++)
            {
                if (i > 0)
                {
                    AppendToken(builder, operation.Attributes.SeparatorTokens[i - 1], string.Empty);
                }

                AppendAttribute(builder, operation.Attributes[i], i > 0 ? " " : string.Empty);
            }

            AppendToken(builder, operation.Attributes.CloseToken!.Value, string.Empty);
        }

        if (operation.TypeSignatureColonToken != null && operation.TypeSignature != null)
        {
            AppendToken(builder, operation.TypeSignatureColonToken.Value, " ");
            AppendRaw(builder, operation.TypeSignature, " ");
        }
    }

    private static void AppendRegion(StringBuilder builder, RegionSyntax region, int indentLevel)
    {
        AppendToken(builder, region.OpenBraceToken, " ");

        for (var i = 0; i < region.Blocks.Count; i++)
        {
            AppendBlock(builder, region.Blocks[i], indentLevel);
        }

        AppendToken(builder, region.CloseBraceToken, "\n", indentLevel);
    }

    private static void AppendBlock(StringBuilder builder, BlockSyntax block, int regionIndentLevel)
    {
        // Synthetic entry blocks are a parser implementation detail. Omit their labels when
        // printing unless the block carries arguments that require an explicit header.
        var blockHasExplicitLabel = block.Label != "^entry" || block.Arguments.Count > 0;
        var blockIndentLevel = regionIndentLevel + 1;

        if (blockHasExplicitLabel)
        {
            AppendToken(builder, block.LabelToken, "\n", blockIndentLevel);

            if (block.Arguments.OpenToken != null)
            {
                AppendToken(builder, block.Arguments.OpenToken.Value, string.Empty);
                for (var i = 0; i < block.Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        AppendToken(builder, block.Arguments.SeparatorTokens[i - 1], string.Empty);
                    }

                    AppendBlockArgument(builder, block.Arguments[i], i > 0 ? " " : string.Empty);
                }

                AppendToken(builder, block.Arguments.CloseToken!.Value, string.Empty);
            }

            AppendToken(builder, block.ColonToken, string.Empty);
        }

        var operationIndentLevel = blockHasExplicitLabel ? regionIndentLevel + 2 : regionIndentLevel + 1;
        for (var i = 0; i < block.Operations.Count; i++)
        {
            AppendOperation(builder, block.Operations[i], operationIndentLevel, "\n");
        }
    }

    private static void AppendBlockArgument(StringBuilder builder, BlockArgumentSyntax argument, string defaultLeadingTrivia)
    {
        AppendToken(builder, argument.NameToken, defaultLeadingTrivia);
        AppendToken(builder, argument.ColonToken, string.Empty);
        AppendRaw(builder, argument.Type, " ");
    }

    private static void AppendAttribute(StringBuilder builder, NamedAttributeSyntax attribute, string defaultLeadingTrivia)
    {
        AppendToken(builder, attribute.NameToken, defaultLeadingTrivia);
        AppendToken(builder, attribute.EqualsToken, " ");
        AppendRaw(builder, attribute.Value, " ");
    }

    private static void AppendToken(StringBuilder builder, SyntaxToken token, string defaultLeadingTrivia, int? indentLevel = null)
    {
        if (token.LeadingTrivia.Length > 0)
        {
            builder.Append(token.LeadingTrivia);
            builder.Append(token.Text);
            return;
        }

        builder.Append(defaultLeadingTrivia);
        if (indentLevel.HasValue)
        {
            AppendIndent(builder, indentLevel.Value);
        }

        builder.Append(token.Text);
    }

    private static void AppendRaw(StringBuilder builder, RawSyntaxText rawText, string defaultLeadingTrivia)
    {
        builder.Append(rawText.HasLeadingTrivia ? rawText.LeadingTrivia : defaultLeadingTrivia);
        builder.Append(rawText.Text);
    }

    private static void AppendIndent(StringBuilder builder, int indentLevel)
    {
        builder.Append(' ', indentLevel * 2);
    }
}
