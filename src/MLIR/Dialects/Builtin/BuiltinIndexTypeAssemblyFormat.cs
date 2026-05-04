namespace MLIR.Dialects.Builtin;

using System;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Types.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Binds and rebuilds the builtin <c>index</c> type.
/// </summary>
/// <remarks>
/// This format owns the builtin <c>index</c> spelling as well as binding and CST rebuild.
/// </remarks>
public sealed class BuiltinIndexTypeAssemblyFormat : ITypeAssemblyFormat
{
    /// <inheritdoc/>
    public ParseResult<TypeSyntax> TryParse(ParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Identifier, out var nameToken) || nameToken.Text != "index")
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        return ParseResult<TypeSyntax>.Success(new BuiltinIndexTypeSyntax(nameToken));
    }

    /// <inheritdoc/>
    public TypeReference Bind(TypeSyntax syntax, Binder binder)
    {
        return new IndexType(syntax);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        return type.Syntax ?? new BuiltinIndexTypeSyntax(TokenFactory.Identifier("index"));
    }
}
