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

    /// <summary>Returns <see langword="true"/> when the current token is an identifier matching <paramref name="text"/>.</summary>
    private bool IsKeyword(string text)
    {
        return Is(TokenKind.Identifier) && Current.Text == text;
    }

    /// <summary>
    /// Attempts to parse a type using a dialect-registered custom assembly format.
    /// Peeks at the current position to determine the type name, looks it up in the dialect registry,
    /// and invokes the registered format handler. Resets the position on <c>NoMatch</c>.
    /// Returns <see cref="ParseOutcome.NoMatch"/> when no registry is available or the name is not registered.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseCustomTypeSyntaxResult()
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
        var result = definition.AssemblyFormat.TryParse(new TypeParsingContext(this));
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
    private ParseResult<TypeSyntax> TryParseTypeAssemblyFormat(
        ITypeAssemblyFormat assemblyFormat,
        TokenKind[] stopBefore,
        string[] stopBeforeKeywords,
        bool stopAtOperationBoundary)
    {
        var checkpoint = Mark();
        var result = assemblyFormat.TryParse(new TypeParsingContext(this, stopBefore, stopBeforeKeywords, stopAtOperationBoundary));
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
    private ParseResult<TypeSyntax> TryParseStandaloneTypeResult()
    {
        var parsed = TryParseTypeSyntaxUntilOperationBoundaryResult();
        if (!parsed.IsSuccess)
        {
            return parsed;
        }

        return !Is(TokenKind.EndOfFile)
            ? ParseResult<TypeSyntax>.Failure(CreateDiagnostic("Expected the type to consume the entire input."))
            : parsed;
    }

    /// <summary>
    /// Parses a type, stopping before any of the supplied delimiter token kinds.
    /// Does not stop at operation boundaries.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTypeSyntaxResult(params TokenKind[] stopBefore)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, stopAtOperationBoundary: false);
    }

    /// <summary>
    /// Parses a type, stopping before any of the supplied delimiter token kinds or keyword spellings.
    /// Does not stop at operation boundaries.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTypeSyntaxResult(string[] stopBeforeKeywords, params TokenKind[] stopBefore)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, stopBeforeKeywords, stopAtOperationBoundary: false);
    }

    /// <summary>
    /// Parses a type, stopping at an operation boundary (newline in leading trivia) rather than an explicit delimiter.
    /// Used for operation type signatures where no closing delimiter token is present.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTypeSyntaxUntilOperationBoundaryResult()
    {
        return TryParseTypeSyntaxCoreResult([], stopAtOperationBoundary: true);
    }

    /// <summary>
    /// Parses a comma-separated list of types until an operation boundary is reached.
    /// This is used by custom operation assembly formats such as <c>type($variadic)</c>,
    /// where the list is not enclosed in parentheses but still needs depth-aware parsing.
    /// </summary>
    private IReadOnlyList<TypeSyntax> ParseTypeSyntaxListUntilOperationBoundary()
    {
        var items = new List<TypeSyntax>();
        while (true)
        {
            var itemResult = TryParseTypeSyntaxCoreResult([TokenKind.Comma], stopAtOperationBoundary: true);
            if (!itemResult.IsSuccess)
            {
                throw new ParseException(itemResult.Diagnostic!);
            }

            items.Add(itemResult.Value);
            if (!TryMatch(TokenKind.Comma, out _))
            {
                break;
            }
        }

        return items;
    }

    /// <summary>
    /// Core type parsing dispatcher: tries default builtin type formats, then registered dialect types, then
    /// reports an unrecognized type fragment.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseTypeSyntaxCoreResult(TokenKind[] stopBefore, bool stopAtOperationBoundary)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, [], stopAtOperationBoundary);
    }

    /// <summary>
     /// Core type parsing dispatcher with both delimiter and keyword stop conditions.
     /// <list type="number">
     ///   <item><description>Tries the default builtin type assembly formats in order.</description></item>
     ///   <item><description>Tries registered dialect types via <see cref="TryParseCustomTypeSyntaxResult"/>.</description></item>
    ///   <item><description>Reports a diagnostic describing the unrecognized type fragment.</description></item>
     /// </list>
     /// </summary>
    private ParseResult<TypeSyntax> TryParseTypeSyntaxCoreResult(TokenKind[] stopBefore, string[] stopBeforeKeywords, bool stopAtOperationBoundary)
    {
        foreach (var assemblyFormat in DefaultTypeAssemblyFormats)
        {
            var builtinTypeResult = TryParseTypeAssemblyFormat(assemblyFormat, stopBefore, stopBeforeKeywords, stopAtOperationBoundary);
            if (!builtinTypeResult.IsNoMatch)
            {
                return builtinTypeResult;
            }
        }

        var customTypeResult = TryParseCustomTypeSyntaxResult();
        if (!customTypeResult.IsNoMatch)
        {
            return customTypeResult;
        }

        return CreateUnrecognizedTypeFailure(stopBefore, stopBeforeKeywords, stopAtOperationBoundary);
    }

    private ParseResult<TypeSyntax> CreateUnrecognizedTypeFailure(TokenKind[] stopBefore, string[] stopBeforeKeywords, bool stopAtOperationBoundary)
    {
        var rawResult = stopAtOperationBoundary
            ? TryParseRawUntilDelimiterOrBoundaryResult(stopBefore)
            : TryParseRawUntilDelimiterOrKeywordResult(stopBefore, stopBeforeKeywords);
        if (!rawResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(rawResult.Diagnostic!);
        }

        return ParseResult<TypeSyntax>.Failure(
            CreateDiagnostic("Expected a type; unrecognized raw syntax '" + rawResult.Value.Text + "'."));
    }

    /// <summary>Bridges <see cref="TryParseTypeSyntaxResult(TokenKind[])"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal ParseResult<TypeSyntax> TryParseTypeSyntaxInternal(params TokenKind[] delimiters)
    {
        return TryParseTypeSyntaxResult(delimiters);
    }

    /// <summary>Bridges <see cref="TryParseTypeSyntaxResult(string[], TokenKind[])"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal ParseResult<TypeSyntax> TryParseTypeSyntaxInternal(string[] stopBeforeKeywords, params TokenKind[] delimiters)
    {
        return TryParseTypeSyntaxResult(stopBeforeKeywords, delimiters);
    }

    /// <summary>Bridges <see cref="TryParseTypeSyntaxUntilOperationBoundaryResult"/> for use by <see cref="DialectParsingContext"/>.</summary>
    internal ParseResult<TypeSyntax> TryParseTypeSyntaxUntilOperationBoundaryInternal()
    {
        return TryParseTypeSyntaxUntilOperationBoundaryResult();
    }

    /// <summary>Bridges ambient stop-condition type parsing for use by <see cref="TypeParsingContext"/>.</summary>
    internal ParseResult<TypeSyntax> TryParseCurrentTypeSyntaxInternal(TokenKind[] stopBefore, string[] stopBeforeKeywords, bool stopAtOperationBoundary)
    {
        return TryParseTypeSyntaxCoreResult(stopBefore, stopBeforeKeywords, stopAtOperationBoundary);
    }

    /// <summary>Bridges standalone nested type fragment parsing for use by <see cref="TypeParsingContext"/>.</summary>
    internal ParseResult<TypeSyntax> TryParseStandaloneTypeTextInternal(string text)
    {
        return TryParseType(text, dialectRegistry, out var type, out var diagnostic)
            ? ParseResult<TypeSyntax>.Success(type!)
            : ParseResult<TypeSyntax>.Failure(diagnostic!);
    }

    /// <summary>Bridges <see cref="ParseTypeSyntaxListUntilOperationBoundary"/> for use by <see cref="OperationParsingContext"/>.</summary>
    internal IReadOnlyList<TypeSyntax> ParseTypeSyntaxListUntilOperationBoundaryInternal()
    {
        return ParseTypeSyntaxListUntilOperationBoundary();
    }
}
