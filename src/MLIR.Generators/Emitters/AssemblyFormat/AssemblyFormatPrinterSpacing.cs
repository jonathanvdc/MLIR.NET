namespace MLIR.Generators.Emitters.AssemblyFormat;

using System;

/// <summary>
/// Mirrors MLIR tablegen's printer spacing state for generated declarative assembly formats.
/// </summary>
internal struct AssemblyFormatPrinterSpacing
{
    private bool shouldEmitSpace;
    private bool lastWasPunctuation;
    private string? explicitTrivia;

    private AssemblyFormatPrinterSpacing(bool shouldEmitSpace, bool lastWasPunctuation)
    {
        this.shouldEmitSpace = shouldEmitSpace;
        this.lastWasPunctuation = lastWasPunctuation;
        explicitTrivia = null;
    }

    public static AssemblyFormatPrinterSpacing Initial => new(shouldEmitSpace: true, lastWasPunctuation: false);

    public void ApplyExplicitTrivia(string trivia)
    {
        explicitTrivia = trivia;
        shouldEmitSpace = false;
        lastWasPunctuation = false;
    }

    public string GetLeadingTrivia(FormatNode node, FormatSubject? subject)
    {
        if (explicitTrivia != null)
        {
            var trivia = explicitTrivia;
            explicitTrivia = null;
            return trivia;
        }

        if (node.LiteralTextForSpacing is { } literalText)
        {
            return ShouldEmitSpaceBefore(literalText, lastWasPunctuation) ? " " : string.Empty;
        }

        return shouldEmitSpace || !lastWasPunctuation ? " " : string.Empty;
    }

    public void MarkEmitted(FormatNode node)
    {
        if (node.LiteralTextForSpacing is { } literalText)
        {
            shouldEmitSpace = literalText != "<" && literalText != "(" && literalText != "{" && literalText != "[" && literalText != "->";
            lastWasPunctuation = IsPunctuation(literalText);
            return;
        }

        shouldEmitSpace = true;
        lastWasPunctuation = false;
    }

    private static bool ShouldEmitSpaceBefore(string value, bool lastWasPunctuation)
    {
        if (value.Length != 1 && !string.Equals(value, "->", StringComparison.Ordinal))
        {
            return true;
        }

        if (lastWasPunctuation)
        {
            return value.Length == 0 || !">)}],".Contains(value[0].ToString());
        }

        return value.Length == 0 || !"<>(){}[],".Contains(value[0].ToString());
    }

    private static bool IsPunctuation(string value)
        => string.Equals(value, "->", StringComparison.Ordinal)
        || value.Length == 1 && "<>(){}[],?:*+-.=@#".Contains(value);
}
