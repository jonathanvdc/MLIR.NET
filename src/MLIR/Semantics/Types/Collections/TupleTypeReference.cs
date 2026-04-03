using System.Linq;
using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;

namespace MLIR.Semantics.Types.Collections;

/// <summary>
/// Represents a builtin tuple type.
/// </summary>
public class TupleTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("tuple");

    /// <summary>
    /// Initializes a new parsed builtin tuple type reference.
    /// </summary>
    public TupleTypeReference(TupleTypeSyntax syntax, IReadOnlyList<TypeReference> elements)
        : this(elements, syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin tuple type reference.
    /// </summary>
    public TupleTypeReference(IReadOnlyList<TypeReference> elements)
        : this(elements, null, SourceLocation.Unknown)
    {
    }

    /// <summary>
    /// Gets the tuple element types.
    /// </summary>
    public IReadOnlyList<TypeReference> Elements { get; }

    /// <inheritdoc/>
    public override string? Name => "tuple";

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    private TupleTypeReference(IReadOnlyList<TypeReference> elements, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(elements), location)
    {
        Elements = elements;
    }

    private static TupleTypeSyntax BuildSyntax(IReadOnlyList<TypeReference> elements)
    {
        var commas = new List<SyntaxToken>(Math.Max(0, elements.Count - 1));
        for (var i = 1; i < elements.Count; i++)
        {
            commas.Add(new SyntaxToken(","));
        }

        return new TupleTypeSyntax(
            new SyntaxToken("tuple"),
            new SyntaxToken("<"),
            elements.Select(static element => element.Syntax ?? throw new InvalidOperationException("Tuple elements must carry syntax.")).ToArray(),
            commas,
            new SyntaxToken(">"));
    }
}
