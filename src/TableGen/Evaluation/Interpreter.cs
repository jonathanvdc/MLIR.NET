namespace TableGen.Evaluation;

using MLIR.Text;
using TableGen.Syntax;

/// <summary>
/// Evaluates TableGen syntax into expanded records.
/// </summary>
public static class Interpreter
{
    /// <summary>
    /// Evaluates a parsed TableGen document.
    /// </summary>
    /// <param name="document">The parsed syntax tree.</param>
    /// <returns>The interpreted document or an evaluation diagnostic.</returns>
    public static ParseResult<InterpretedDocument> Evaluate(DocumentSyntax document)
    {
        var context = new EvaluationContext(document);
        var builder = new RecordBuilder(context);
        return builder.BuildDocument();
    }
}
