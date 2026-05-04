namespace MLIR.Text;

/// <summary>
/// Provides dialect-specific type parsers controlled access to the MLIR parser.
/// </summary>
public sealed class TypeParsingContext : ParsingContext
{
    internal TypeParsingContext(Parser parser)
        : base(parser)
    {
    }
}
