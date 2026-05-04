namespace MLIR.Text;

/// <summary>
/// Provides dialect-specific parsers controlled access to the MLIR parser.
/// </summary>
public sealed class OperationParsingContext : ParsingContext
{
    internal OperationParsingContext(Parser parser)
        : base(parser)
    {
    }
}
