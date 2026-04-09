using MLIR.Semantics;
using MLIR.Semantics.Types.Primitives;
using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;

namespace MLIR;

/// <summary>
/// Represents a builtin integer type such as <c>i32</c> or <c>si64</c>.
/// </summary>
/// <remarks>
/// The source generator contributes the public wrapper constructors, public parameter
/// properties, and the shared <see cref="TypeDefinition"/> metadata. This handwritten
/// partial keeps the semantic identity, parsing, and syntax-preservation logic used by
/// the runtime and generated constraint wrappers.
/// </remarks>
public partial class IntegerType : TypeReference
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerType"/> class
    /// with an optional preserved syntax node.
    /// </summary>
    protected IntegerType(int width, IntegerTypeSignedness signedness, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(width, signedness), location)
    {
        Width = width;
        Signedness = signedness;
    }

    private static BuiltinIntegerTypeSyntax BuildSyntax(int width, IntegerTypeSignedness signedness)
    {
        var text = signedness switch
        {
            IntegerTypeSignedness.Signed => "si" + width,
            IntegerTypeSignedness.Unsigned => "ui" + width,
            _ => "i" + width,
        };

        return new BuiltinIntegerTypeSyntax(TokenFactory.Identifier(text), signedness, width);
    }

}
