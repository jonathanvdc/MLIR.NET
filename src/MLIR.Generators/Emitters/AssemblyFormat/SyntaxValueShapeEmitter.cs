namespace MLIR.Generators.Emitters.AssemblyFormat;

using System;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

/// <summary>
/// Emits syntax-rewriter expressions from pre-resolved syntax value shapes.
/// </summary>
internal static class SyntaxValueShapeEmitter
{
    /// <summary>
    /// Builds the expression used to rewrite a generated syntax property.
    /// </summary>
    public static string GetRewriteExpression(string variableName, string syntaxType, SyntaxValueShape shape)
    {
        var propertyExpr = EmitterHelpers.CapitalizeFirst(variableName) + "Syntax";

        return shape switch
        {
            SyntaxValueShape.Token or SyntaxValueShape.OptionalToken =>
                "rewriter.VisitToken(" + propertyExpr + ")",

            SyntaxValueShape.RawText =>
                "rewriter.VisitRawText(" + propertyExpr + ")",

            SyntaxValueShape.SyntaxNode =>
                "(" + syntaxType + ")rewriter.Visit(" + propertyExpr + ")",

            SyntaxValueShape.OptionalSyntaxNode =>
                propertyExpr + " != null ? (" + GetRequiredSyntaxType(syntaxType) + ")rewriter.Visit(" + propertyExpr + ") : null",

            SyntaxValueShape.DelimitedList =>
                "rewriter.VisitDelimitedList(" + propertyExpr + ")",

            SyntaxValueShape.DelimitedTokenList =>
                "rewriter.VisitDelimitedTokenList(" + propertyExpr + ")",

            SyntaxValueShape.SeparatedList =>
                "rewriter.VisitSeparatedList(" + propertyExpr + ")",

            SyntaxValueShape.SeparatedTokenList =>
                "rewriter.VisitSeparatedTokenList(" + propertyExpr + ")",

            SyntaxValueShape.TokenList =>
                "rewriter.VisitTokenList(" + propertyExpr + ")",

            SyntaxValueShape.RawTextList =>
                "rewriter.VisitRawTextList(" + propertyExpr + ")",

            SyntaxValueShape.SyntaxList =>
                "rewriter.VisitList(" + propertyExpr + ")",

            SyntaxValueShape.PlainValue =>
                propertyExpr,

            _ => throw new InvalidOperationException("Unsupported syntax value shape: " + shape),
        };
    }

    /// <summary>
    /// Removes the nullable marker used by generated optional syntax-node properties.
    /// </summary>
    private static string GetRequiredSyntaxType(string syntaxType)
    {
        return syntaxType.EndsWith("?", StringComparison.Ordinal)
            ? syntaxType.Substring(0, syntaxType.Length - 1)
            : syntaxType;
    }
}
