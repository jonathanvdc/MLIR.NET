namespace MLIR.Dialects.Builtin;

using System;
using MLIR.Semantics;
using MLIR.Semantics.Types.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Binds and rebuilds a scalar builtin floating-point type such as <c>f32</c> or <c>bf16</c>.
/// </summary>
/// <remarks>
/// <para>
/// Parsing is handled by the core type parser; this format only provides binding and CST rebuild.
/// </para>
/// <para>
/// Each generated scalar float <c>TypeDef</c> class supplies a constructor delegate that produces
/// the concrete <see cref="FloatTypeReference"/> subclass, keeping the assembly format reusable
/// across all float types without a per-type subclass.
/// </para>
/// </remarks>
public sealed class BuiltinScalarFloatTypeAssemblyFormat : ITypeAssemblyFormat
{
    private readonly Func<BuiltinFloatTypeSyntax?, FloatTypeReference> _create;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuiltinScalarFloatTypeAssemblyFormat"/> class.
    /// </summary>
    /// <param name="create">
    /// A delegate that constructs the concrete float type from an optional parsed syntax node.
    /// </param>
    public BuiltinScalarFloatTypeAssemblyFormat(Func<BuiltinFloatTypeSyntax?, FloatTypeReference> create)
    {
        _create = create;
    }

    /// <inheritdoc/>
    public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)
    {
        // Parsing is handled by the core type parser, not by dialect custom syntax.
        return ParseResult<TypeSyntax>.NoMatch();
    }

    /// <inheritdoc/>
    public TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder)
    {
        return _create(syntax as BuiltinFloatTypeSyntax);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type.Syntax is BuiltinFloatTypeSyntax existing)
        {
            return existing;
        }

        var name = type.Name ?? throw new InvalidOperationException(
            $"Cannot rebuild assembly syntax for a float type with no canonical name.");
        return new BuiltinFloatTypeSyntax(TokenFactory.Identifier(name));
    }
}
