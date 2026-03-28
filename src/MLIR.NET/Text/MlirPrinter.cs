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
        MlirStructuralPrinter.AppendModule(builder, module.Operations, module.EndOfFileToken, AppendOperation);
        return builder.ToString();
    }

    internal static void AppendOperation(StringBuilder builder, OperationSyntax operation, int indentLevel, string defaultLeadingTrivia)
    {
        AppendOperation(builder, operation, indentLevel, defaultLeadingTrivia, static (innerBuilder, region, _, innerIndentLevel) =>
            MlirStructuralPrinter.AppendRegion(innerBuilder, region, region.Blocks, innerIndentLevel, static block => block, static block => block.Operations, AppendOperation));
    }

    internal static void AppendOperation(
        StringBuilder builder,
        OperationSyntax operation,
        int indentLevel,
        string defaultLeadingTrivia,
        Action<StringBuilder, RegionSyntax, int, int> appendRegion)
    {
        if (operation.ResultTokens.Count > 0)
        {
            for (var i = 0; i < operation.ResultTokens.Count; i++)
            {
                if (i > 0)
                {
                    MlirPrintWriter.AppendToken(builder, operation.ResultCommaTokens[i - 1], string.Empty);
                }

                MlirPrintWriter.AppendToken(builder, operation.ResultTokens[i], i > 0 ? " " : defaultLeadingTrivia, i == 0 ? indentLevel : null);
            }

            MlirPrintWriter.AppendToken(builder, operation.EqualsToken!.Value, " ");
            MlirPrintWriter.AppendToken(builder, operation.NameToken, " ");
        }
        else
        {
            MlirPrintWriter.AppendToken(builder, operation.NameToken, defaultLeadingTrivia, indentLevel);
        }

        MlirPrintWriter.AppendToken(builder, operation.OperandList.OpenToken!.Value, string.Empty);
        for (var i = 0; i < operation.OperandList.Count; i++)
        {
            if (i > 0)
            {
                MlirPrintWriter.AppendToken(builder, operation.OperandList.SeparatorTokens[i - 1], string.Empty);
            }

            MlirPrintWriter.AppendToken(builder, operation.OperandList[i], i > 0 ? " " : string.Empty);
        }

        MlirPrintWriter.AppendToken(builder, operation.OperandList.CloseToken!.Value, string.Empty);

        if (operation.SuccessorList.OpenToken != null)
        {
            MlirPrintWriter.AppendToken(builder, operation.SuccessorList.OpenToken.Value, " ");
            for (var i = 0; i < operation.SuccessorList.Count; i++)
            {
                if (i > 0)
                {
                    MlirPrintWriter.AppendToken(builder, operation.SuccessorList.SeparatorTokens[i - 1], string.Empty);
                }

                MlirPrintWriter.AppendToken(builder, operation.SuccessorList[i], i > 0 ? " " : string.Empty);
            }

            MlirPrintWriter.AppendToken(builder, operation.SuccessorList.CloseToken!.Value, string.Empty);
        }

        for (var i = 0; i < operation.Regions.Count; i++)
        {
            appendRegion(builder, operation.Regions[i], i, indentLevel);
        }

        if (operation.Attributes.OpenToken != null)
        {
            MlirPrintWriter.AppendToken(builder, operation.Attributes.OpenToken.Value, " ");
            for (var i = 0; i < operation.Attributes.Count; i++)
            {
                if (i > 0)
                {
                    MlirPrintWriter.AppendToken(builder, operation.Attributes.SeparatorTokens[i - 1], string.Empty);
                }

                MlirPrintWriter.AppendAttribute(builder, operation.Attributes[i], i > 0 ? " " : string.Empty);
            }

            MlirPrintWriter.AppendToken(builder, operation.Attributes.CloseToken!.Value, string.Empty);
        }

        if (operation.TypeSignatureColonToken != null && operation.TypeSignature != null)
        {
            MlirPrintWriter.AppendToken(builder, operation.TypeSignatureColonToken.Value, " ");
            MlirPrintWriter.AppendRaw(builder, operation.TypeSignature, " ");
        }
    }
}
