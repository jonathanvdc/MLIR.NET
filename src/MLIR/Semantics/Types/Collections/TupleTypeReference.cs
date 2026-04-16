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
    public static TypeDefinition TypeDefinition { get; } = new("tuple", new MLIR.Dialects.Builtin.BuiltinTupleTypeAssemblyFormat());

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
        : base(syntax, location)
    {
        Elements = elements;
    }

    /// <inheritdoc/>
    protected override Type SemanticFamily => typeof(TupleTypeReference);

    /// <inheritdoc/>
    protected override bool SemanticEqualsValue(TypeReference other)
    {
        var otherTuple = (TupleTypeReference)other;
        if (Elements.Count != otherTuple.Elements.Count)
        {
            return false;
        }

        for (var i = 0; i < Elements.Count; i++)
        {
            if (Elements[i] != otherTuple.Elements[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    protected override int GetSemanticHashCodeValue()
    {
        return GetSequenceHashCode(Elements);
    }
}
