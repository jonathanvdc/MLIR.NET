namespace MLIR.Text;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Provides custom assembly printers controlled access to MLIR printing services.
/// </summary>
public sealed class OperationPrintingContext
{
    private readonly SyntaxWriter writer;

    internal OperationPrintingContext(SyntaxWriter writer, int indentLevel, string defaultLeadingTrivia)
    {
        this.writer = writer;
        IndentLevel = indentLevel;
        DefaultLeadingTrivia = defaultLeadingTrivia;
    }

    /// <summary>
    /// Gets the indentation level of the operation currently being printed.
    /// </summary>
    public int IndentLevel { get; }

    /// <summary>
    /// Gets the default leading trivia to use when tokens do not carry explicit trivia.
    /// </summary>
    public string DefaultLeadingTrivia { get; }

    /// <summary>
    /// Appends the operation's default leading trivia and indentation.
    /// </summary>
    public void WriteOperationPrefix()
    {
        writer.Write(DefaultLeadingTrivia);
        writer.WriteIndent(IndentLevel);
    }

    /// <summary>
    /// Appends raw text to the output.
    /// </summary>
    public void Write(string text)
    {
        writer.Write(text);
    }

    /// <summary>
    /// Appends a token using the generic token-preserving formatting rules.
    /// </summary>
    public void WriteToken(SyntaxToken token, string defaultLeadingTrivia, int? indentLevel = null)
    {
        writer.WriteToken(token, defaultLeadingTrivia, indentLevel);
    }

    /// <summary>
    /// Appends raw syntax text using the generic formatting rules.
    /// </summary>
    public void WriteRaw(RawSyntaxText rawText, string defaultLeadingTrivia)
    {
        writer.WriteRaw(rawText, defaultLeadingTrivia);
    }

    /// <summary>
    /// Prints a nested region using the semantic printer.
    /// </summary>
    public void PrintRegion(Region region)
    {
        Printer.AppendSemanticRegion(writer, region, IndentLevel);
    }

    /// <summary>
    /// Prints the supplied operation using the generic assembly fallback.
    /// </summary>
    public void PrintGenericOperation(Operation operation)
    {
        Printer.AppendGenericSemanticOperation(writer, operation, IndentLevel, DefaultLeadingTrivia);
    }
}
