namespace MLIR.Text;

using MLIR.Semantics;
using MLIR.Syntax;

/// <summary>
/// Provides custom assembly printers controlled access to the semantic MLIR printer.
/// </summary>
public sealed class OperationPrintingContext
{
    private readonly SemanticPrinter printer;
    private readonly SyntaxWriter writer;

    internal OperationPrintingContext(SemanticPrinter printer, SyntaxWriter writer, int indentLevel, string defaultLeadingTrivia)
    {
        this.printer = printer;
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
        printer.AppendRegion(writer, region, IndentLevel);
    }

    /// <summary>
    /// Prints the supplied operation using the generic assembly fallback.
    /// </summary>
    public void PrintGenericOperation(Operation operation)
    {
        printer.AppendGenericOperation(writer, operation, IndentLevel, DefaultLeadingTrivia);
    }
}
