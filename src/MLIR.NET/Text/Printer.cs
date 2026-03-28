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

        switch (operation.Body)
        {
            case GenericOperationBodySyntax generic:
                AppendGenericBody(builder, generic, indentLevel, appendRegion);
                break;
            case CustomOperationBodySyntax custom:
                AppendCustomBody(builder, custom, indentLevel, appendRegion);
                break;
        }
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

    private static void AppendGenericBody(
        StringBuilder builder,
        GenericOperationBodySyntax body,
        int indentLevel,
        Action<StringBuilder, RegionSyntax, int, int> appendRegion)
    {
        PrintWriter.AppendToken(builder, body.OperandList.OpenToken!.Value, string.Empty);
        for (var i = 0; i < body.OperandList.Count; i++)
        {
            if (i > 0)
            {
                PrintWriter.AppendToken(builder, body.OperandList.SeparatorTokens[i - 1], string.Empty);
            }

            PrintWriter.AppendToken(builder, body.OperandList[i], i > 0 ? " " : string.Empty);
        }

        PrintWriter.AppendToken(builder, body.OperandList.CloseToken!.Value, string.Empty);

        if (body.SuccessorList.OpenToken != null)
        {
            PrintWriter.AppendToken(builder, body.SuccessorList.OpenToken.Value, " ");
            for (var i = 0; i < body.SuccessorList.Count; i++)
            {
                if (i > 0)
                {
                    PrintWriter.AppendToken(builder, body.SuccessorList.SeparatorTokens[i - 1], string.Empty);
                }

                PrintWriter.AppendToken(builder, body.SuccessorList[i], i > 0 ? " " : string.Empty);
            }

            PrintWriter.AppendToken(builder, body.SuccessorList.CloseToken!.Value, string.Empty);
        }

        for (var i = 0; i < body.Regions.Count; i++)
        {
            appendRegion(builder, body.Regions[i], i, indentLevel);
        }

        AppendAttributeDictionary(builder, body.Attributes, " ");

        if (body.TypeSignatureColonToken != null && body.TypeSignature != null)
        {
            PrintWriter.AppendToken(builder, body.TypeSignatureColonToken.Value, " ");
            PrintWriter.AppendRaw(builder, body.TypeSignature, " ");
        }
    }

    private static void AppendCustomBody(
        StringBuilder builder,
        CustomOperationBodySyntax body,
        int indentLevel,
        Action<StringBuilder, RegionSyntax, int, int> appendRegion)
    {
        var defaultTrivia = " ";
        var regionIndex = 0;
        foreach (var item in body.Items)
        {
            switch (item)
            {
                case CustomTokenSyntax token:
                    PrintWriter.AppendToken(builder, token.Token, defaultTrivia);
                    defaultTrivia = GetNextDefaultLeadingTrivia(token.Token.Text);
                    break;
                case CustomRawSyntax raw:
                    PrintWriter.AppendRaw(builder, raw.Text, defaultTrivia);
                    defaultTrivia = " ";
                    break;
                case CustomRegionSyntax region:
                    appendRegion(builder, region.Region, regionIndex, indentLevel);
                    regionIndex++;
                    defaultTrivia = " ";
                    break;
                case CustomAttributeDictionarySyntax attributes:
                    AppendAttributeDictionary(builder, attributes.Attributes, defaultTrivia);
                    defaultTrivia = " ";
                    break;
            }
        }
    }

    private static void AppendAttributeDictionary(
        StringBuilder builder,
        DelimitedSyntaxList<NamedAttributeSyntax> attributes,
        string defaultLeadingTrivia)
    {
        if (attributes.OpenToken == null)
        {
            return;
        }

        PrintWriter.AppendToken(builder, attributes.OpenToken.Value, defaultLeadingTrivia);
        for (var i = 0; i < attributes.Count; i++)
        {
            if (i > 0)
            {
                PrintWriter.AppendToken(builder, attributes.SeparatorTokens[i - 1], string.Empty);
            }

            PrintWriter.AppendAttribute(builder, attributes[i], i > 0 ? " " : string.Empty);
        }

        PrintWriter.AppendToken(builder, attributes.CloseToken!.Value, string.Empty);
    }

    private static string GetNextDefaultLeadingTrivia(string tokenText)
    {
        return tokenText switch
        {
            "," or ":" or "=" => " ",
            "(" or "[" or "{" => string.Empty,
            _ => string.Empty,
        };
    }
}
