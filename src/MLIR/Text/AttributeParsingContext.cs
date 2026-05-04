namespace MLIR.Text;

/// <summary>
/// Provides dialect-specific attribute parsers controlled access to the MLIR parser.
/// </summary>
public sealed class AttributeParsingContext : ParsingContext
{
    internal AttributeParsingContext(Parser parser)
        : base(parser)
    {
    }
}
