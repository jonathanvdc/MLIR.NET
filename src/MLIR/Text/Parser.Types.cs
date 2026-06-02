namespace MLIR.Text;

using System.Collections.Generic;
using MLIR.Dialects;
using MLIR.Dialects.Builtin;
using MLIR.Syntax;

public sealed partial class Parser
{
    /// <summary>
    /// Ordered fallback parsers for type syntax with no expected dialect-specific assembly format.
    /// Bare dialect types without an assembly format are handled separately through the registry.
    /// </summary>
    private static readonly ITypeAssemblyFormat[] DefaultTypeAssemblyFormats = [
        new BuiltinFunctionTypeAssemblyFormat(),
        new BuiltinTupleTypeAssemblyFormat(),
        new BuiltinTensorTypeAssemblyFormat(),
        new BuiltinVectorTypeAssemblyFormat(),
        new BuiltinMemRefTypeAssemblyFormat(),
        new BuiltinIntegerTypeAssemblyFormat(),
        new BuiltinScalarFloatTypeAssemblyFormat(static _ => throw new System.InvalidOperationException("The default parser-only float assembly format should never be used for binding.")),
        new BuiltinIndexTypeAssemblyFormat(),
        new BuiltinNoneTypeAssemblyFormat()
    ];

    /// <summary>
    /// Attempts to parse a type using a dialect-registered custom assembly format.
    /// Peeks at the current position to determine the type name, looks it up in the dialect registry,
    /// and invokes the registered format handler. Resets the position on <c>NoMatch</c>.
    /// Returns <see cref="ParseOutcome.NoMatch"/> when no registry is available or the name is not registered.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseCustomTypeSyntax()
    {
        if (dialectRegistry == null)
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var canonicalName = TryPeekTypeDefinitionName();
        if (canonicalName == null || !dialectRegistry.TryGetType(canonicalName, out var definition))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        if (definition.AssemblyFormat == null)
        {
            var bareTypeCheckpoint = Mark();
            if (!TryMatch(TokenKind.Bang, out var bangToken) || !TryMatch(TokenKind.Identifier, out var nameToken))
            {
                return ParseResult<TypeSyntax>.NoMatch();
            }

            if (Is(TokenKind.LessThan))
            {
                Reset(bareTypeCheckpoint);
                return ParseResult<TypeSyntax>.NoMatch();
            }

            return ParseResult<TypeSyntax>.Success(new BareDialectTypeSyntax(new DialectTypePrefix(bangToken, nameToken)));
        }

        var checkpoint = Mark();
        var result = definition.AssemblyFormat.TryParse(new ParsingContext(this));
        if (result.IsSuccess)
        {
            return result;
        }

        Reset(checkpoint);
        return result.IsError ? result : ParseResult<TypeSyntax>.NoMatch();
    }

    /// <summary>
    /// Invokes a specific <see cref="ITypeAssemblyFormat"/> handler against the current parser position.
    /// Saves a checkpoint before calling and restores it when the handler returns <c>NoMatch</c>.
    /// Propagates <c>Success</c> and <c>Error</c> unchanged.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTypeAssemblyFormat(ITypeAssemblyFormat assemblyFormat)
    {
        var checkpoint = Mark();
        var result = assemblyFormat.TryParse(new ParsingContext(this));
        if (result.IsSuccess)
        {
            return result;
        }

        Reset(checkpoint);
        return result.IsError ? result : ParseResult<TypeSyntax>.NoMatch();
    }

    /// <summary>
    /// Parses a standalone type that must consume the entire input.
    /// Returns a failure when tokens remain after the type.
    /// Used by the public <see cref="ParseType"/> and <c>TryParseType</c> entry points.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseStandaloneType()
    {
        var parsed = TryParseTypeSyntax();
        if (!parsed.IsSuccess)
        {
            return parsed;
        }

        return !Is(TokenKind.EndOfFile)
            ? ParseResult<TypeSyntax>.Failure(CreateDiagnostic("Expected the type to consume the entire input."))
            : parsed;
    }

    /// <summary>
    /// Parses a type syntax node, trying builtin assembly formats first and then registered custom formats.
    /// This is the main entry point for nested type parsing from assembly format handlers.
    /// </summary>
    internal ParseResult<TypeSyntax> TryParseTypeSyntax()
    {
        foreach (var assemblyFormat in DefaultTypeAssemblyFormats)
        {
            var builtinTypeResult = TryParseTypeAssemblyFormat(assemblyFormat);
            if (!builtinTypeResult.IsNoMatch)
            {
                return builtinTypeResult;
            }
        }

        var customTypeResult = TryParseCustomTypeSyntax();
        if (!customTypeResult.IsNoMatch)
        {
            return customTypeResult;
        }

        return CreateUnrecognizedTypeFailure();
    }

    /// <summary>
    /// Parses a comma-separated list of types, consuming items until an operation boundary is reached.
    /// This is used by the <c>type($variadic)</c> custom assembly format, where the list is not enclosed in parentheses but still needs depth-aware parsing.
    /// </summary>
    /// <returns>A successful result with the list of parsed types, or a failure if any item fails to parse.</returns>
    internal ParseResult<IReadOnlyList<TypeSyntax>> TryParseTypeSyntaxList()
    {
        var result = TryParseTypeSyntaxSeparatedList();
        return result.IsSuccess
            ? ParseResult<IReadOnlyList<TypeSyntax>>.Success(result.Value.Items)
            : ParseResult<IReadOnlyList<TypeSyntax>>.Failure(result.Diagnostic!);
    }

    /// <summary>
    /// Parses a comma-separated list of types while preserving the comma tokens.
    /// </summary>
    /// <returns>A successful result with the separated type syntax list, or a failure if any item fails to parse.</returns>
    internal ParseResult<SeparatedSyntaxList<TypeSyntax>> TryParseTypeSyntaxSeparatedList()
    {
        var items = new List<TypeSyntax>();
        var separators = new List<Token>();
        while (true)
        {
            var itemResult = TryParseTypeSyntax();
            if (!itemResult.IsSuccess)
            {
                return ParseResult<SeparatedSyntaxList<TypeSyntax>>.Failure(itemResult.Diagnostic!);
            }

            items.Add(itemResult.Value);
            if (!TryMatch(TokenKind.Comma, out var comma))
            {
                break;
            }

            separators.Add(comma);
        }

        return ParseResult<SeparatedSyntaxList<TypeSyntax>>.Success(new SeparatedSyntaxList<TypeSyntax>(items, separators));
    }

    private ParseResult<TypeSyntax> CreateUnrecognizedTypeFailure()
    {
        return ParseResult<TypeSyntax>.Failure(
            CreateDiagnostic("Expected a type; unrecognized syntax."));
    }

    /// <summary>Bridges standalone nested type fragment parsing for use by <see cref="ParsingContext"/>.</summary>
    internal ParseResult<TypeSyntax> TryParseStandaloneTypeTextInternal(string text)
    {
        return TryParseType(text, dialectRegistry, out var type, out var diagnostic)
            ? ParseResult<TypeSyntax>.Success(type!)
            : ParseResult<TypeSyntax>.Failure(diagnostic!);
    }
}
