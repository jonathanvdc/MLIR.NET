namespace MLIR.Text;

using System;
using System.Collections.Generic;
using System.Text;
using MLIR.Syntax;

internal static class MlirStructuralPrinter
{
    public static void AppendModule<TOperation>(
        StringBuilder builder,
        IReadOnlyList<TOperation> operations,
        SyntaxToken endOfFileToken,
        Action<StringBuilder, TOperation, int, string> appendOperation)
    {
        for (var i = 0; i < operations.Count; i++)
        {
            appendOperation(builder, operations[i], 0, i > 0 ? "\n" : string.Empty);
        }

        builder.Append(endOfFileToken.LeadingTrivia);
    }

    public static void AppendRegion<TBlock, TOperation>(
        StringBuilder builder,
        RegionSyntax syntax,
        IReadOnlyList<TBlock> blocks,
        int indentLevel,
        Func<TBlock, BlockSyntax> getBlockSyntax,
        Func<TBlock, IReadOnlyList<TOperation>> getOperations,
        Action<StringBuilder, TOperation, int, string> appendOperation)
    {
        MlirPrintWriter.AppendToken(builder, syntax.OpenBraceToken, " ");

        for (var i = 0; i < blocks.Count; i++)
        {
            AppendBlock(builder, blocks[i], indentLevel, getBlockSyntax, getOperations, appendOperation);
        }

        MlirPrintWriter.AppendToken(builder, syntax.CloseBraceToken, "\n", indentLevel);
    }

    private static void AppendBlock<TBlock, TOperation>(
        StringBuilder builder,
        TBlock block,
        int regionIndentLevel,
        Func<TBlock, BlockSyntax> getBlockSyntax,
        Func<TBlock, IReadOnlyList<TOperation>> getOperations,
        Action<StringBuilder, TOperation, int, string> appendOperation)
    {
        var syntax = getBlockSyntax(block);

        // Synthetic entry blocks are a parser implementation detail. Omit their labels when
        // printing unless the block carries arguments that require an explicit header.
        var blockHasExplicitLabel = syntax.Label != "^entry" || syntax.Arguments.Count > 0;
        var blockIndentLevel = regionIndentLevel + 1;

        if (blockHasExplicitLabel)
        {
            MlirPrintWriter.AppendToken(builder, syntax.LabelToken, "\n", blockIndentLevel);

            if (syntax.Arguments.OpenToken != null)
            {
                MlirPrintWriter.AppendToken(builder, syntax.Arguments.OpenToken.Value, string.Empty);
                for (var i = 0; i < syntax.Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        MlirPrintWriter.AppendToken(builder, syntax.Arguments.SeparatorTokens[i - 1], string.Empty);
                    }

                    MlirPrintWriter.AppendBlockArgument(builder, syntax.Arguments[i], i > 0 ? " " : string.Empty);
                }

                MlirPrintWriter.AppendToken(builder, syntax.Arguments.CloseToken!.Value, string.Empty);
            }

            MlirPrintWriter.AppendToken(builder, syntax.ColonToken, string.Empty);
        }

        var operations = getOperations(block);
        var operationIndentLevel = blockHasExplicitLabel ? regionIndentLevel + 2 : regionIndentLevel + 1;
        for (var i = 0; i < operations.Count; i++)
        {
            appendOperation(builder, operations[i], operationIndentLevel, "\n");
        }
    }
}
