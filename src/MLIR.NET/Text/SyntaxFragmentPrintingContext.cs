namespace MLIR.Text;

using System.Text;
using MLIR.Syntax;

/// <summary>
/// Provides syntax fragments controlled access to the MLIR printer.
/// </summary>
public sealed class SyntaxFragmentPrintingContext
{
    private readonly StringBuilder builder;

    internal SyntaxFragmentPrintingContext(StringBuilder builder, string defaultLeadingTrivia)
    {
        this.builder = builder;
        DefaultLeadingTrivia = defaultLeadingTrivia;
    }

    /// <summary>
    /// Gets the default leading trivia to use when syntax does not carry explicit trivia.
    /// </summary>
    public string DefaultLeadingTrivia { get; }

    /// <summary>
    /// Appends a token using the generic token-preserving formatting rules.
    /// </summary>
    public void WriteToken(SyntaxToken token, string defaultLeadingTrivia)
    {
        PrintWriter.AppendToken(builder, token, defaultLeadingTrivia);
    }

    /// <summary>
    /// Appends raw syntax text using the generic formatting rules.
    /// </summary>
    public void WriteRaw(RawSyntaxText rawText, string defaultLeadingTrivia)
    {
        PrintWriter.AppendRaw(builder, rawText, defaultLeadingTrivia);
    }
}
