namespace MLIR.Text;

using MLIR.Syntax;

/// <summary>
/// Provides dialect-specific attribute parsers controlled access to the MLIR parser.
/// </summary>
public sealed class AttributeParsingContext : DialectParsingContext
{
    internal AttributeParsingContext(Parser parser)
        : base(parser)
    {
    }
}
