using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

namespace MLIR;

/// <summary>
/// Represents the builtin <c>none</c> type.
/// </summary>
/// <remarks>
/// The source generator contributes the public wrapper constructors and the shared
/// <see cref="TypeDefinition"/> metadata. This handwritten partial keeps the semantic
/// identity and syntax-preservation logic used by the runtime and generated constraint
/// wrappers.
/// </remarks>
public partial class NoneType : TypeReference
{
    /// <summary>
    /// Initializes a new parsed builtin none type.
    /// </summary>
    public NoneType(BuiltinNoneTypeSyntax syntax)
        : this(syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin none type.
    /// </summary>
    public NoneType()
        : this(null, SourceLocation.Unknown)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NoneType"/> class
    /// with an optional preserved syntax node.
    /// </summary>
    protected NoneType(TypeSyntax? syntax, SourceLocation location)
        : base(syntax, location)
    {
    }

    /// <inheritdoc/>
    protected override Type SemanticFamily => typeof(NoneType);
}
