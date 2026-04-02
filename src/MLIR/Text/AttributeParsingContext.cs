namespace MLIR.Text;

using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Provides dialect-specific attribute parsers controlled access to the MLIR parser.
/// </summary>
public sealed class AttributeParsingContext : DialectParsingContext
{
    internal AttributeParsingContext(Parser parser, DialectRegistry? dialectRegistry, AttributeDefinition? expectedDefinition)
        : base(parser)
    {
        DialectRegistry = dialectRegistry;
        ExpectedDefinition = expectedDefinition;
    }

    /// <summary>
    /// Gets the dialect registry used for parsing, if one is available.
    /// </summary>
    public DialectRegistry? DialectRegistry { get; }

    /// <summary>
    /// Gets the attribute definition expected by the caller, if one is known.
    /// </summary>
    public AttributeDefinition? ExpectedDefinition { get; }
}
