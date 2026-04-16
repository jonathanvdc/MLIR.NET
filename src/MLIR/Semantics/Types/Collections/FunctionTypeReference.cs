using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;

namespace MLIR.Semantics.Types.Collections;

/// <summary>
/// Represents a builtin function type.
/// </summary>
public class FunctionTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("function", new MLIR.Dialects.Builtin.BuiltinFunctionTypeAssemblyFormat());

    /// <summary>
    /// Initializes a new parsed builtin function type reference.
    /// </summary>
    public FunctionTypeReference(FunctionTypeSyntax syntax, IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results)
        : this(inputs, results, syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin function type reference.
    /// </summary>
    public FunctionTypeReference(IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results)
        : this(inputs, results, null, SourceLocation.Unknown)
    {
    }

    /// <summary>
    /// Gets the input types.
    /// </summary>
    public IReadOnlyList<TypeReference> Inputs { get; }

    /// <summary>
    /// Gets the result types.
    /// </summary>
    public IReadOnlyList<TypeReference> Results { get; }

    /// <inheritdoc/>
    public override string? Name => "function";

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    private FunctionTypeReference(IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results, TypeSyntax? syntax, SourceLocation location)
        : base(syntax, location)
    {
        Inputs = inputs;
        Results = results;
    }

    /// <inheritdoc/>
    protected override Type SemanticFamily => typeof(FunctionTypeReference);

    /// <inheritdoc/>
    protected override bool SemanticEqualsValue(TypeReference other)
    {
        var otherFunction = (FunctionTypeReference)other;
        return HaveSameTypes(Inputs, otherFunction.Inputs)
            && HaveSameTypes(Results, otherFunction.Results);
    }

    /// <inheritdoc/>
    protected override int GetSemanticHashCodeValue()
    {
        unchecked
        {
            return (GetSequenceHashCode(Inputs) * 397) ^ GetSequenceHashCode(Results);
        }
    }

    private static bool HaveSameTypes(IReadOnlyList<TypeReference> left, IReadOnlyList<TypeReference> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }
}
