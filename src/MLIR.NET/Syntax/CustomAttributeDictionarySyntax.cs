namespace MLIR.Syntax;

/// <summary>
/// Represents a preserved attribute dictionary in custom operation assembly.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CustomAttributeDictionarySyntax"/> class.
/// </remarks>
/// <param name="attributes">The preserved attribute dictionary.</param>
public sealed class CustomAttributeDictionarySyntax(DelimitedSyntaxList<NamedAttributeSyntax> attributes) : CustomAssemblyItemSyntax
{
    /// <summary>
    /// Gets the preserved attribute dictionary.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> Attributes { get; } = attributes;
}
