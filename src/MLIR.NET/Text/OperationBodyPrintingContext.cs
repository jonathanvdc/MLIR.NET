namespace MLIR.Text;

using System;
using System.Text;
using MLIR.Syntax;

/// <summary>
/// Provides operation-body syntax nodes controlled access to the MLIR printer.
/// </summary>
public sealed class OperationBodyPrintingContext
{
    private readonly StringBuilder builder;
    private readonly Action<StringBuilder, RegionSyntax, int, int> appendRegion;
    private int printedRegionCount;

    internal OperationBodyPrintingContext(
        StringBuilder builder,
        int indentLevel,
        string defaultLeadingTrivia,
        Action<StringBuilder, RegionSyntax, int, int> appendRegion)
    {
        this.builder = builder;
        this.appendRegion = appendRegion;
        IndentLevel = indentLevel;
        DefaultLeadingTrivia = defaultLeadingTrivia;
    }

    /// <summary>
    /// Gets the indentation level of the operation currently being printed.
    /// </summary>
    public int IndentLevel { get; }

    /// <summary>
    /// Gets the default leading trivia to use when syntax does not carry explicit trivia.
    /// </summary>
    public string DefaultLeadingTrivia { get; }

    /// <summary>
    /// Appends a token using the generic token-preserving formatting rules.
    /// </summary>
    public void WriteToken(SyntaxToken token, string defaultLeadingTrivia, int? indentLevel = null)
    {
        PrintWriter.AppendToken(builder, token, defaultLeadingTrivia, indentLevel);
    }

    /// <summary>
    /// Appends raw syntax text using the generic formatting rules.
    /// </summary>
    public void WriteRaw(RawSyntaxText rawText, string defaultLeadingTrivia)
    {
        PrintWriter.AppendRaw(builder, rawText, defaultLeadingTrivia);
    }

    /// <summary>
    /// Appends a type syntax node using the generic formatting rules.
    /// </summary>
    public void WriteType(TypeSyntax typeSyntax, string defaultLeadingTrivia)
    {
        typeSyntax.Print(new SyntaxFragmentPrintingContext(builder, defaultLeadingTrivia));
    }

    /// <summary>
    /// Appends a named attribute using the generic formatting rules.
    /// </summary>
    public void WriteAttribute(NamedAttributeSyntax attribute, string defaultLeadingTrivia)
    {
        PrintWriter.AppendAttribute(builder, attribute, defaultLeadingTrivia);
    }

    /// <summary>
    /// Appends a nested region using the printer's region formatting rules.
    /// </summary>
    public void WriteRegion(RegionSyntax region)
    {
        appendRegion(builder, region, printedRegionCount, IndentLevel);
        printedRegionCount++;
    }
}
