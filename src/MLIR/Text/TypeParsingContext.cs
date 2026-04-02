namespace MLIR.Text;

using MLIR.Syntax;

/// <summary>
/// Provides dialect-specific type parsers controlled access to the MLIR parser.
/// </summary>
public sealed class TypeParsingContext : DialectParsingContext
{
    internal TypeParsingContext(Parser parser)
        : base(parser)
    {
    }
}
