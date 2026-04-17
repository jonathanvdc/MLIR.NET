namespace MLIR.Syntax;

/// <summary>
/// Represents an enum attribute value, which consists of a comma-separated list of tokens, possibly enclosed in angle brackets.
/// </summary>
public abstract class EnumAttributeValueSyntax : AttributeValueSyntax
{
    /// <summary>
    /// Gets the list of tokens representing the enum attribute value.
    /// For example, for an enum attribute value like <c>&lt;foo, bar, baz&gt;</c>, this list would contain three tokens: <c>foo</c>, <c>bar</c>, and <c>baz</c>.
    /// The source location of the entire enum attribute value is determined by the location of the elements in this list.
    /// </summary>
    /// <remarks>
    /// The <see cref="Elements"/> list should not be empty for a valid enum attribute value.
    /// </remarks>
    public abstract IReadOnlyList<Token> Elements { get; }
}
