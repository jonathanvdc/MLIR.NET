namespace TableGen;

using TableGen.Evaluation;
using TableGen.Syntax;
using TableGen.Text;

/// <summary>
/// Represents a parsed TableGen document.
/// </summary>
public sealed class TableGenDocument(TableGenDocumentSyntax syntax)
{
    /// <summary>
    /// Gets the parsed syntax tree.
    /// </summary>
    public TableGenDocumentSyntax Syntax { get; } = syntax;

    /// <summary>
    /// Parses a TableGen document from source text.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>The parsed document.</returns>
    public static TableGenDocument Parse(string source)
    {
        return new TableGenDocument(TableGenParser.ParseDocument(source));
    }

    /// <summary>
    /// Evaluates the document into expanded records.
    /// </summary>
    /// <returns>The interpreted document.</returns>
    public InterpretedDocument Evaluate()
    {
        return TableGenInterpreter.Evaluate(Syntax);
    }
}
