namespace MLIR.Dialects.Builtin;

using System;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Binds and rebuilds the builtin <c>none</c> type.
/// </summary>
/// <remarks>
/// Parsing is handled by the core type parser; this format only provides binding and CST rebuild.
/// </remarks>
public sealed class BuiltinNoneTypeAssemblyFormat : ITypeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<TypeSyntax> TryParse(TypeParsingContext context)
    {
        // Parsing is handled by the core type parser, not by dialect custom syntax.
        return ParseResult<TypeSyntax>.NoMatch();
    }

    /// <inheritdoc/>
    public TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder)
    {
        return new NoneType(syntax);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        return type.Syntax ?? new BuiltinNoneTypeSyntax(TokenFactory.Identifier("none"));
    }
}
