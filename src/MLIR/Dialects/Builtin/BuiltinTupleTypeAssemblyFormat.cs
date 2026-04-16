namespace MLIR.Dialects.Builtin;

using System;
using System.Collections.Generic;
using System.Linq;
using MLIR.Semantics;
using MLIR.Semantics.Types.Collections;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Binds and rebuilds the builtin <c>tuple</c> type, e.g. <c>tuple&lt;i32, f32&gt;</c>.
/// </summary>
/// <remarks>
/// Parsing is handled by the core type parser; this format only provides binding and CST rebuild.
/// <c>BuildCustomAssemblySyntax</c> uses the builder context to recursively synthesize syntax for
/// nested element types so that syntaxless child types are supported.
/// </remarks>
public sealed class BuiltinTupleTypeAssemblyFormat : ITypeAssemblyFormat
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
        if (syntax is not TupleTypeSyntax tupleSyntax)
        {
            throw new InvalidOperationException("Tuple types require tuple type syntax.");
        }

        var elements = tupleSyntax.Elements.Select(binder.BindTypeReference).ToArray();
        return new TupleType(elements, tupleSyntax);
    }

    /// <inheritdoc/>
    public TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context)
    {
        if (type.Syntax is TupleTypeSyntax existing)
        {
            return existing;
        }

        var elements = type switch
        {
            TupleType generated => generated.Types,
            _ => throw new InvalidOperationException(
                $"Cannot rebuild assembly syntax for an unrecognized tuple type reference of type {type.GetType().FullName}.")
        };

        var elementSyntax = elements.Select(context.BuildTypeSyntax).ToArray();
        var commas = new List<Token>(Math.Max(0, elements.Count - 1));
        for (var i = 1; i < elements.Count; i++)
        {
            commas.Add(TokenFactory.Comma());
        }

        return new TupleTypeSyntax(
            TokenFactory.Identifier("tuple"),
            TokenFactory.LessThan(),
            elementSyntax,
            commas,
            TokenFactory.GreaterThan());
    }
}
