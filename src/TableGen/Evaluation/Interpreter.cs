namespace TableGen.Evaluation;

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
    /// <returns>The interpreted document.</returns>
    public static InterpretedDocument Evaluate(DocumentSyntax document)
    {
        var context = new EvaluationContext(document);
        var builder = new RecordBuilder(context);
        var result = builder.BuildDocument();
        if (!result.IsSuccess)
        {
            throw result.Diagnostic!.ToException();
        }

        return result.Value;
    }
}
