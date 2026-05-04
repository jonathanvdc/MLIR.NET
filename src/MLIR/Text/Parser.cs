namespace MLIR.Text;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MLIR.Dialects;
using MLIR.Syntax;

/// <summary>
/// Parses generic MLIR syntax into a concrete syntax tree.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Parser"/> is a hand-written, single-pass recursive-descent parser that converts MLIR text into a
/// <em>Concrete Syntax Tree</em> (CST). The CST is the source of truth for both parsing and round-trip printing:
/// no information is discarded, and the original whitespace and comments are preserved as leading trivia on each token.
/// </para>
///
/// <para><strong>Layered architecture</strong></para>
/// <para>
/// The intended flow through the runtime stack is:
/// <list type="number">
///   <item><description><c>MLIR text</c> → <see cref="Lexer"/> → flat token list</description></item>
///   <item><description>flat token list → <see cref="Parser"/> → CST (<see cref="ModuleSyntax"/> and friends)</description></item>
///   <item><description>CST → <c>Binder</c> → typed semantic objects (<c>Module</c>, <c>Operation</c>, etc.)</description></item>
///   <item><description>typed semantic objects → <c>ConcreteSyntaxBuilder</c> → CST → <see cref="Printer"/> → MLIR text</description></item>
/// </list>
/// The parser deliberately produces only CST. If you need typed values (e.g., <c>Value</c>, <c>Region</c>), pass
/// the <see cref="ModuleSyntax"/> to the binder rather than extending the parser to perform semantic work.
/// </para>
///
/// <para><strong>Three-valued parse results</strong></para>
/// <para>
/// Every internal parsing step returns a <see cref="ParseResult{T}"/> with one of three outcomes:
/// <list type="bullet">
///   <item><description><see cref="ParseOutcome.Success"/> – the production matched and a value was produced.</description></item>
///   <item><description><see cref="ParseOutcome.NoMatch"/> – the production did not apply; no tokens were consumed and the
///     parser can safely try an alternative.</description></item>
///   <item><description><see cref="ParseOutcome.Error"/> – the production started to match but encountered malformed syntax;
///     a <see cref="Diagnostic"/> is attached and propagation stops.</description></item>
/// </list>
/// The distinction between <c>NoMatch</c> and <c>Error</c> is critical for correct backtracking: only <c>NoMatch</c>
/// results should cause the parser to reset its position via <see cref="Mark"/> / <see cref="Reset"/>.
/// </para>
///
/// <para><strong>Backtracking</strong></para>
/// <para>
/// The parser supports bounded backtracking through <see cref="Mark"/> and <see cref="Reset"/>. A <see cref="ParseMark"/>
/// records the current token index. When a speculative parse fails with <c>NoMatch</c>, the parser resets to the recorded
/// mark and tries the next alternative. This pattern is used for ambiguous constructs such as the
/// <c>{</c> token, which may start either a region or an attribute dictionary, and for custom assembly format dispatch.
/// </para>
///
/// <para><strong>Operation boundary detection</strong></para>
/// <para>
/// MLIR does not require a terminator token between consecutive operations. Instead, the parser treats a newline
/// in the leading trivia of the current token as an <em>operation boundary</em>. The method
/// <see cref="IsOperationBoundary"/> implements this rule, and <see cref="EnsureOperationBoundaryResult"/> enforces it
/// after each top-level or block-level operation. This matches the upstream MLIR parser's whitespace-sensitivity for
/// operation separation.
/// </para>
///
/// <para><strong>Custom assembly format integration</strong></para>
/// <para>
/// When a <see cref="DialectRegistry"/> is supplied, the parser attempts to dispatch each operation, type, or attribute
/// to the corresponding dialect-registered assembly-format handler before falling back to generic or raw parsing:
/// <list type="bullet">
///   <item><description>Operations: the registry is queried by normalized operation name; if a matching
///     <c>IOperationAssemblyFormat</c> is found, its <c>TryParse</c> method is called with an
///     <see cref="OperationParsingContext"/> after peeking the operation name.</description></item>
///   <item><description>Types: the registry is queried by type name and dispatched to an
///     <c>ITypeAssemblyFormat</c> through a <see cref="TypeParsingContext"/>.</description></item>
///   <item><description>Attributes: self-identifying attributes (prefixed with <c>#identifier</c>) are looked up first;
///     caller-supplied <c>AttributeConstraintDefinition</c> hints are tried next; built-in structured forms
///     (arrays, dictionaries, dense arrays, elements) are checked last before falling back to raw syntax.</description></item>
/// </list>
/// All custom format handlers receive a context object derived from <see cref="DialectParsingContext"/> rather than the
/// parser itself, keeping the parser's internals private and ensuring that dialect code can only advance the token
/// stream through well-defined primitives.
/// </para>
///
/// <para><strong>Raw syntax fallback</strong></para>
/// <para>
/// When no structured match is found for a type or attribute, the parser captures the remaining tokens as a
/// <c>RawSyntaxText</c> node using <see cref="TryScanRawFragment"/>. The scan is bracket-aware (it tracks
/// <c>()</c>, <c>{}</c>, <c>[]</c>, and <c>&lt;&gt;</c> depth) and stops at the first unbalanced delimiter,
/// operation boundary, or explicit stop token, whichever comes first.
/// </para>
///
/// <para><strong>Error handling</strong></para>
/// <para>
/// On parse failure the public API surface follows one of two contracts:
/// <list type="bullet">
///   <item><description>Throwing: <see cref="ParseModule(string)"/>, <see cref="ParseAttributeValue"/>, and
///     <see cref="ParseType(string,DialectRegistry?)"/> throw a <see cref="ParseException"/> whose
///     <see cref="ParseException.Diagnostic"/> carries the error location.</description></item>
///   <item><description>Non-throwing: <see cref="TryParseModule(string,out ModuleSyntax?,out Diagnostic?)"/>,
///     <see cref="TryParseAttributeValue(string,out AttributeValueSyntax?,out Diagnostic?)"/>, and
///     <see cref="TryParseType(string,out TypeSyntax?,out Diagnostic?)"/> return <see langword="false"/> and
///     populate the <c>diagnostic</c> out-parameter.</description></item>
/// </list>
/// </para>
///
/// <para><strong>Usage example – parsing a module</strong></para>
/// <example>
/// <code>
/// // Throwing form — suitable when the source is trusted to be valid MLIR.
/// ModuleSyntax module = Parser.ParseModule("""
///     func.func @add(%a: i32, %b: i32) -> i32 {
///       %sum = arith.addi %a, %b : i32
///       return %sum : i32
///     }
///     """);
///
/// // Non-throwing form — preferred when the source may be user-supplied.
/// if (!Parser.TryParseModule(source, out ModuleSyntax? module, out Diagnostic? diagnostic))
/// {
///     Console.Error.WriteLine(diagnostic);
/// }
/// </code>
/// </example>
///
/// <para><strong>Usage example – dialect-aware parsing</strong></para>
/// <example>
/// <code>
/// // Register dialects so that custom assembly formats are recognized during parsing.
/// var registry = new DialectRegistry();
/// registry.RegisterDialect(new ArithDialect());
///
/// ModuleSyntax module = Parser.ParseModule(source, registry);
/// </code>
/// </example>
///
/// <para><strong>Extension pattern – implementing a custom assembly format</strong></para>
/// <example>
/// <code>
/// // Custom operation assembly formats receive an OperationParsingContext that exposes
/// // safe, composable parser primitives without exposing the parser internals.
/// public bool TryParse(
///     Token nameToken,
///     SeparatedSyntaxList&lt;Token&gt; resultList,
///     Token? equalsToken,
///     OperationParsingContext ctx,
///     out OperationBodySyntax? body)
/// {
///     // Parse `%operand : type`
///     if (!ctx.TryParseSsaToken().TryGetValue(out var operand))  return false;
///     if (!ctx.Expect(TokenKind.Colon, "Expected ':'.").TryGetValue(out var colon)) return false;
///     if (!ctx.TryParseTypeSyntax().TryGetValue(out var type)) return false;
///     // ... build and return the body ...
/// }
/// </code>
/// </example>
/// </remarks>
public sealed partial class Parser
{
    /// <summary>
    /// Records a token-stream position so that the parser can reset to it if a speculative
    /// production fails with <see cref="ParseOutcome.NoMatch"/>.
    /// </summary>
    private readonly struct ParseMark
    {
        public ParseMark(int position)
        {
            Position = position;
        }

        /// <summary>Gets the token index captured when the mark was created.</summary>
        public int Position { get; }
    }

    /// <summary>The raw MLIR source text, used to reconstruct span text for raw syntax nodes.</summary>
    private readonly string source;

    /// <summary>
    /// The flat, immutable token list produced by the <see cref="Lexer"/>.
    /// Each token is source-backed, carrying document-relative offset information for
    /// on-demand line/column resolution via its <see cref="MLIR.Syntax.Token.Location"/>.
    /// The last element is always an <see cref="TokenKind.EndOfFile"/> sentinel so that
    /// <see cref="Current"/> never reads past the end of the array.
    /// </summary>
    private readonly IReadOnlyList<Token> tokens;

    /// <summary>
    /// Optional registry of dialect-specific assembly format handlers.
    /// When <see langword="null"/>, all operations, types, and attributes fall through to
    /// generic or raw parsing.
    /// </summary>
    private readonly DialectRegistry? dialectRegistry;

    /// <summary>
    /// Current read position in <see cref="tokens"/>. Advanced by <see cref="ConsumeToken"/>
    /// and restored by <see cref="Reset"/> during backtracking.
    /// </summary>
    private int position;

    /// <summary>
    /// Initializes a new <see cref="Parser"/> instance backed by the supplied token list.
    /// Use the static factory <see cref="TryCreateParser"/> rather than constructing directly.
    /// </summary>
    /// <param name="source">The original MLIR source text.</param>
    /// <param name="tokens">Source-backed token list produced by the lexer; must end with an EOF token.</param>
    /// <param name="dialectRegistry">Optional registry for dialect-specific assembly formats.</param>
    private Parser(string source, IReadOnlyList<Token> tokens, DialectRegistry? dialectRegistry = null)
    {
        this.source = source;
        this.dialectRegistry = dialectRegistry;
        this.tokens = tokens;
    }

    /// <summary>
    /// Parses a module from the supplied MLIR source text.
    /// </summary>
    /// <param name="source">The MLIR source text.</param>
    /// <returns>The parsed module syntax.</returns>
    public static ModuleSyntax ParseModule(string source)
    {
        return TryParseModule(source, out var syntax, out var diagnostic)
            ? syntax!
            : throw new ParseException(diagnostic!);
    }

    /// <summary>
    /// Parses a module from the supplied MLIR source text, using registered dialects to recognize custom assembly formats.
    /// </summary>
    /// <param name="source">The MLIR source text.</param>
    /// <param name="dialectRegistry">The dialect registry used to recognize custom assembly formats.</param>
    /// <returns>The parsed module syntax.</returns>
    public static ModuleSyntax ParseModule(string source, DialectRegistry? dialectRegistry)
    {
        return TryParseModule(source, dialectRegistry, out var syntax, out var diagnostic)
            ? syntax!
            : throw new ParseException(diagnostic!);
    }

    /// <summary>
    /// Tries to parse a module from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseModule(string source, out ModuleSyntax? syntax, out Diagnostic? diagnostic)
    {
        return TryParseModule(source, null, out syntax, out diagnostic);
    }

    /// <summary>
    /// Tries to parse a module from the supplied MLIR source text, using registered dialects to recognize custom assembly formats.
    /// </summary>
    public static bool TryParseModule(string source, DialectRegistry? dialectRegistry, out ModuleSyntax? syntax, out Diagnostic? diagnostic)
    {
        syntax = null;
        var parserResult = TryCreateParser(source, dialectRegistry);
        if (!parserResult.IsSuccess)
        {
            diagnostic = parserResult.Diagnostic;
            return false;
        }

        var result = parserResult.Value.TryParseModuleCoreResult();
        syntax = result.IsSuccess ? result.Value : null;
        diagnostic = result.Diagnostic;
        return result.IsSuccess;
    }

    /// <summary>
    /// Parses a standalone attribute value from the supplied MLIR source text.
    /// </summary>
    public static AttributeValueSyntax ParseAttributeValue(string source, DialectRegistry? dialectRegistry = null, AttributeConstraintDefinition? expectedDefinition = null)
    {
        return TryParseAttributeValue(source, dialectRegistry, expectedDefinition, out var syntax, out var diagnostic)
            ? syntax!
            : throw new ParseException(diagnostic!);
    }

    /// <summary>
    /// Tries to parse a standalone attribute value from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseAttributeValue(
        string source,
        out AttributeValueSyntax? syntax,
        out Diagnostic? diagnostic)
    {
        return TryParseAttributeValue(source, null, null, out syntax, out diagnostic);
    }

    /// <summary>
    /// Tries to parse a standalone attribute value from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseAttributeValue(
        string source,
        DialectRegistry? dialectRegistry,
        out AttributeValueSyntax? syntax,
        out Diagnostic? diagnostic)
    {
        return TryParseAttributeValue(source, dialectRegistry, null, out syntax, out diagnostic);
    }

    /// <summary>
    /// Tries to parse a standalone attribute value from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseAttributeValue(
        string source,
        DialectRegistry? dialectRegistry,
        AttributeConstraintDefinition? expectedDefinition,
        out AttributeValueSyntax? syntax,
        out Diagnostic? diagnostic)
    {
        syntax = null;
        var parserResult = TryCreateParser(source, dialectRegistry);
        if (!parserResult.IsSuccess)
        {
            diagnostic = parserResult.Diagnostic;
            return false;
        }

        var parser = parserResult.Value;
        var result = parser.TryParseStandaloneAttributeValue(expectedDefinition);
        syntax = result.IsSuccess ? result.Value : null;
        diagnostic = result.Diagnostic;
        return result.IsSuccess;
    }

    /// <summary>
    /// Parses a standalone type from the supplied MLIR source text.
    /// </summary>
    public static TypeSyntax ParseType(string source, DialectRegistry? dialectRegistry = null)
    {
        return TryParseType(source, dialectRegistry, out var syntax, out var diagnostic)
            ? syntax!
            : throw new ParseException(diagnostic!);
    }

    /// <summary>
    /// Tries to parse a standalone type from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseType(string source, DialectRegistry? dialectRegistry, out TypeSyntax? syntax, out Diagnostic? diagnostic)
    {
        syntax = null;
        var parserResult = TryCreateParser(source, dialectRegistry);
        if (!parserResult.IsSuccess)
        {
            diagnostic = parserResult.Diagnostic;
            return false;
        }

        var parser = parserResult.Value;
        var result = parser.TryParseStandaloneTypeResult();
        syntax = result.IsSuccess ? result.Value : null;
        diagnostic = result.Diagnostic;
        return result.IsSuccess;
    }

    /// <summary>
    /// Tries to parse a standalone type from the supplied MLIR source text.
    /// </summary>
    public static bool TryParseType(string source, out TypeSyntax? syntax, out Diagnostic? diagnostic)
    {
        return TryParseType(source, null, out syntax, out diagnostic);
    }

    /// <summary>
    /// Parses a top-level sequence of operations that forms an implicit module.
    /// Stops at <see cref="TokenKind.EndOfFile"/> and enforces operation boundaries between consecutive items.
    /// </summary>
    private ParseResult<ModuleSyntax> TryParseModuleCoreResult()
    {
        var operations = new List<OperationSyntax>();
        while (!Is(TokenKind.EndOfFile))
        {
            var operationResult = TryParseOperationResult();
            if (!operationResult.IsSuccess)
            {
                return ParseResult<ModuleSyntax>.Failure(operationResult.Diagnostic!);
            }

            operations.Add(operationResult.Value);
            var boundaryResult = EnsureOperationBoundaryResult(false);
            if (!boundaryResult.IsSuccess)
            {
                return ParseResult<ModuleSyntax>.Failure(boundaryResult.Diagnostic!);
            }
        }

        return ParseResult<ModuleSyntax>.Success(new ModuleSyntax(operations, ConsumeToken()));
    }

    /// <summary>
    /// Parses a single MLIR operation, including its optional result list, operation name,
    /// operands, successors, regions, attribute dictionary, and type signature.
    /// </summary>
    /// <remarks>
    /// The parse strategy follows the MLIR generic operation grammar and then tries custom formats:
    /// <list type="number">
    ///   <item><description>If the current token is an SSA name (<c>%</c>), parse the result list and the
    ///     mandatory <c>=</c> that follows it.</description></item>
    ///   <item><description>Parse the operation name (bare identifier or quoted string).</description></item>
    ///   <item><description>For unquoted names, attempt a registered custom assembly format via
    ///     <see cref="TryParseCustomAssemblyResult"/>. If that returns <c>NoMatch</c>, attempt a
    ///     "projected custom-like" form (bare operands + <c>:</c> + raw type).</description></item>
    ///   <item><description>If no custom form matches, fall through to the full generic format:
    ///     <c>(operands) [successors] { regions } {attrs} : type</c>.</description></item>
    /// </list>
    /// </remarks>
    private ParseResult<OperationSyntax> TryParseOperationResult()
    {
        var customOperationResult = TryParseCustomAssemblyResult();
        if (customOperationResult.IsSuccess || customOperationResult.IsError)
        {
            return customOperationResult;
        }

        var resultItems = new List<Token>();
        var resultSeparators = new List<Token>();
        Token? equalsToken = null;

        if (Is(TokenKind.SsaName))
        {
            var firstResultTokenResult = TryParseSsaTokenResult();
            if (!firstResultTokenResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Failure(firstResultTokenResult.Diagnostic!);
            }

            var firstResultToken = firstResultTokenResult.Value;
            resultItems.Add(firstResultToken);

            if (TryMatch(TokenKind.Colon, out _))
            {
                var countTokenResult = ExpectTokenResult(TokenKind.Integer, "Expected result count after ':'.");
                if (!countTokenResult.IsSuccess)
                {
                    return ParseResult<OperationSyntax>.Failure(countTokenResult.Diagnostic!);
                }

                var countToken = countTokenResult.Value;
                var count = int.Parse(countToken.Text, CultureInfo.InvariantCulture);
                for (var i = 1; i < count; i++)
                {
                    resultItems.Add(TokenFactory.SsaName(firstResultToken.Text + "#" + i.ToString(CultureInfo.InvariantCulture)));
                }
            }

            while (TryMatch(TokenKind.Comma, out var resultCommaToken))
            {
                resultSeparators.Add(resultCommaToken);
                var nextResultToken = TryParseSsaTokenResult();
                if (!nextResultToken.IsSuccess)
                {
                    return ParseResult<OperationSyntax>.Failure(nextResultToken.Diagnostic!);
                }

                resultItems.Add(nextResultToken.Value);
            }

            var equalsTokenResult = ExpectTokenResult(TokenKind.Equal, "Expected '=' after operation result list.");
            if (!equalsTokenResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Failure(equalsTokenResult.Diagnostic!);
            }

            equalsToken = equalsTokenResult.Value;
        }

        var resultList = new SeparatedSyntaxList<Token>(resultItems, resultSeparators);

        var nameTokenResult = TryParseOperationNameTokenResult();
        if (!nameTokenResult.IsSuccess)
        {
            return ParseResult<OperationSyntax>.Failure(nameTokenResult.Diagnostic!);
        }

        var nameToken = nameTokenResult.Value;
        if (!nameToken.Text.StartsWith("\"", System.StringComparison.Ordinal))
        {
            var projectedBodyResult = TryParseProjectedCustomLikeOperationBodyResult();
            if (projectedBodyResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Success(new OperationSyntax(
                    resultList,
                    equalsToken,
                    nameToken,
                    projectedBodyResult.Value));
            }
        }

        var operandsResult = TryParseOperandsResult();
        if (!operandsResult.IsSuccess)
        {
            return ParseResult<OperationSyntax>.Failure(operandsResult.Diagnostic!);
        }

        var successorsResult = TryParseSuccessorsResult();
        if (!successorsResult.IsSuccess)
        {
            return ParseResult<OperationSyntax>.Failure(successorsResult.Diagnostic!);
        }

        var regions = new List<RegionSyntax>();
        while (Is(TokenKind.LBrace) && IsRegionStart())
        {
            var regionResult = TryParseRegionResult();
            if (!regionResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Failure(regionResult.Diagnostic!);
            }

            regions.Add(regionResult.Value);
        }

        var attributesResult = TryParseAttrDict();
        if (!attributesResult.IsSuccess)
        {
            return ParseResult<OperationSyntax>.Failure(attributesResult.Diagnostic!);
        }

        Token? typeSignatureColonToken = null;
        TypeSyntax? typeSignatureSyntax = null;
        if (Is(TokenKind.Colon))
        {
            var colonResult = ExpectTokenResult(TokenKind.Colon, "Expected ':' before the type signature.");
            if (!colonResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Failure(colonResult.Diagnostic!);
            }

            typeSignatureColonToken = colonResult.Value;
            var typeResult = TryParseTypeSyntaxUntilOperationBoundaryResult();
            if (!typeResult.IsSuccess)
            {
                return ParseResult<OperationSyntax>.Failure(typeResult.Diagnostic!);
            }

            typeSignatureSyntax = typeResult.Value;
        }

        return ParseResult<OperationSyntax>.Success(new OperationSyntax(
            resultList,
            equalsToken,
            nameToken,
            operandsResult.Value,
            successorsResult.Value,
            regions,
            attributesResult.Value,
            typeSignatureColonToken,
            typeSignatureSyntax));
    }

    /// <summary>
    /// Attempts to parse an operation body using a "projected" format that resembles custom assembly:
    /// bare operand names, an optional attribute dictionary, a <c>:</c> separator, and a structured type signature.
    /// </summary>
    /// <remarks>
    /// This production handles non-generic operations whose assembly format is not registered in the dialect
    /// registry but still follows the common pattern <c>%a, %b {attrs} : type</c> or <c>%a : type to type</c>.
    /// The result is wrapped in a
    /// <see cref="GenericOperationBodySyntax"/> with synthetic parentheses around the operands so the CST
    /// has a uniform shape regardless of which parse path was taken.
    /// Returns <see cref="ParseOutcome.NoMatch"/> if no <c>:</c> is found, resetting the parser to the
    /// checkpoint so the caller can fall through to the full generic format.
    /// </remarks>
    private ParseResult<OperationBodySyntax> TryParseProjectedCustomLikeOperationBodyResult()
    {
        var checkpoint = Mark();

        var operandTokens = new List<Token>();
        var operandCommaTokens = new List<Token>();
        if (Is(TokenKind.SsaName))
        {
            var firstOperandResult = TryParseSsaTokenResult();
            if (!firstOperandResult.IsSuccess)
            {
                return ParseResult<OperationBodySyntax>.Failure(firstOperandResult.Diagnostic!);
            }

            operandTokens.Add(firstOperandResult.Value);
            while (TryMatch(TokenKind.Comma, out var comma))
            {
                operandCommaTokens.Add(comma);
                var operandResult = TryParseSsaTokenResult();
                if (!operandResult.IsSuccess)
                {
                    return ParseResult<OperationBodySyntax>.Failure(operandResult.Diagnostic!);
                }

                operandTokens.Add(operandResult.Value);
            }
        }

        var attributeDictResult = TryParseAttrDict();
        if (!attributeDictResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(attributeDictResult.Diagnostic!);
        }

        if (!TryMatch(TokenKind.Colon, out var colonToken))
        {
            Reset(checkpoint);
            return ParseResult<OperationBodySyntax>.NoMatch();
        }

        var typeSignatureResult = TryParseProjectedCustomLikeTypeSignatureResult();
        if (!typeSignatureResult.IsSuccess)
        {
            return ParseResult<OperationBodySyntax>.Failure(typeSignatureResult.Diagnostic!);
        }

        return ParseResult<OperationBodySyntax>.Success(new GenericOperationBodySyntax(
            new DelimitedSyntaxList<Token>(
                TokenFactory.LParen(),
                operandTokens,
                operandCommaTokens,
                TokenFactory.RParen()),
            new DelimitedSyntaxList<Token>(null, new List<Token>(), new List<Token>(), null),
            new List<RegionSyntax>(),
            attributeDictResult.Value,
            colonToken,
            typeSignatureResult.Value));
    }

    /// <summary>
    /// Parses the trailing type portion of an unregistered custom-like operation.
    /// This accepts either a single type or the common custom-assembly shape <c>sourceType to resultType</c>.
    /// </summary>
    private ParseResult<TypeSyntax> TryParseProjectedCustomLikeTypeSignatureResult()
    {
        var sourceTypeResult = TryParseTypeSyntaxResult(["to"]);
        if (!sourceTypeResult.IsSuccess)
        {
            return sourceTypeResult;
        }

        if (!IsKeyword("to"))
        {
            return sourceTypeResult;
        }

        var toKeywordResult = ExpectKeywordResult("to", "Expected 'to'.");
        if (!toKeywordResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(toKeywordResult.Diagnostic!);
        }

        var resultTypeResult = TryParseTypeSyntaxUntilOperationBoundaryResult();
        if (!resultTypeResult.IsSuccess)
        {
            return resultTypeResult;
        }

        return ParseResult<TypeSyntax>.Success(new ProjectedToTypeSyntax(
            sourceTypeResult.Value,
            toKeywordResult.Value,
            resultTypeResult.Value));
    }

    /// <summary>
    /// Attempts to dispatch parsing of a whole operation to a dialect-registered custom assembly format.
    /// </summary>
    /// <returns>
    /// A successful result when the dialect handler accepted and parsed the operation;
    /// <see cref="ParseOutcome.Error"/> when the handler committed to the format but found malformed syntax;
    /// <see cref="ParseOutcome.NoMatch"/> when no handler is registered for the operation name.
    /// The parser position is reset to the pre-call checkpoint on <c>NoMatch</c>.
    /// </returns>
    private ParseResult<OperationSyntax> TryParseCustomAssemblyResult()
    {
        if (dialectRegistry == null)
        {
            return ParseResult<OperationSyntax>.NoMatch();
        }

        // First, set a checkpoint so we can reset if the parse fails or the format doesn't match. Then parse the operation name
        // and result values (if any) so we can pass them to the custom format handler.
        // If the name is quoted, it's not a valid candidate for custom-format parsing, so bail early.
        var checkpoint = Mark();
        var context = new OperationParsingContext(this);
        var headerResult = context.TryParseHeader();
        if (!headerResult.IsSuccess || headerResult.Value.NameToken.Text.StartsWith("\"", StringComparison.Ordinal))
        {
            Reset(checkpoint);
            return ParseResult<OperationSyntax>.NoMatch();
        }

        // Next, look up the operation name in the registry. If it's not found or the format is not an assembly format, bail with
        // NoMatch so the caller can fall back to generic parsing.
        var nameToken = headerResult.Value.NameToken;
        var normalizedName = NormalizeOperationName(nameToken.Text);
        if (!dialectRegistry.TryGetOperationForParsing(normalizedName, out var definition) || definition.AssemblyFormat == null)
        {
            Reset(checkpoint);
            return ParseResult<OperationSyntax>.NoMatch();
        }

        // Finally, invoke the custom assembly format's TryParse method. If it returns NoMatch, reset and fall back to generic parsing;
        // if it returns Error, propagate the error without resetting since the format handler has already committed to this parse path.
        ParseResult<OperationSyntax> result;
        if (definition.AssemblyFormat is BodyOnlyOperationAssemblyFormat format)
        {
            // Optimization for body-only formats: pass the parsed header components in a context object so the format handler can avoid
            // re-parsing them from the token stream.
            result = format.TryParseAfterHeader(headerResult.Value, context);
        }
        else
        {
            // Otherwise, just call the general TryParse and let the format handler parse everything, including the header.
            // This is less efficient since the format handler will have to re-parse the operation name and results,
            // but it allows maximum flexibility.
            Reset(checkpoint);
            result = definition.AssemblyFormat.TryParse(context);
        }

        if (result.IsSuccess || result.IsError)
        {
            return result;
        }

        Reset(checkpoint);
        return ParseResult<OperationSyntax>.NoMatch();
    }

    /// <summary>
    /// Parses a single MLIR region: <c>{ block* }</c>.
    /// </summary>
    /// <remarks>
    /// MLIR regions may contain an implicit entry block with no label. Such unlabeled leading
    /// operations are collected and then wrapped in a synthetic <c>^entry</c> block so the CST
    /// always has a uniform block-based structure, regardless of whether the source used explicit
    /// block labels. Empty regions also receive a synthetic empty entry block.
    /// </remarks>
    private ParseResult<RegionSyntax> TryParseRegionResult()
    {
        var openBraceResult = ExpectTokenResult(TokenKind.LBrace, "Expected '{' to start a region.");
        if (!openBraceResult.IsSuccess)
        {
            return ParseResult<RegionSyntax>.Failure(openBraceResult.Diagnostic!);
        }

        var openBraceToken = openBraceResult.Value;
        var blocks = new List<BlockSyntax>();
        var pendingEntryOperations = new List<OperationSyntax>();

        while (!Is(TokenKind.RBrace))
        {
            if (Is(TokenKind.BlockLabel))
            {
                if (pendingEntryOperations.Count > 0)
                {
                    // MLIR allows unlabeled operations at the start of a region. Model them as
                    // a synthetic entry block so the CST always has a block-based shape.
                    blocks.Add(new BlockSyntax(
                        TokenFactory.BlockLabel("^entry"),
                        new DelimitedSyntaxList<BlockArgumentSyntax>(null, new List<BlockArgumentSyntax>(), new List<Token>(), null),
                        TokenFactory.Colon(),
                        pendingEntryOperations.ToList()));
                    pendingEntryOperations.Clear();
                }

                var blockResult = TryParseBlockResult();
                if (!blockResult.IsSuccess)
                {
                    return ParseResult<RegionSyntax>.Failure(blockResult.Diagnostic!);
                }

                blocks.Add(blockResult.Value);
            }
            else
            {
                var operationResult = TryParseOperationResult();
                if (!operationResult.IsSuccess)
                {
                    return ParseResult<RegionSyntax>.Failure(operationResult.Diagnostic!);
                }

                pendingEntryOperations.Add(operationResult.Value);
                var boundaryResult = EnsureOperationBoundaryResult(true);
                if (!boundaryResult.IsSuccess)
                {
                    return ParseResult<RegionSyntax>.Failure(boundaryResult.Diagnostic!);
                }
            }
        }

        if (pendingEntryOperations.Count > 0 || blocks.Count == 0)
        {
            // Keep region bodies uniform even for empty regions and unlabeled entry operations.
            blocks.Insert(0, new BlockSyntax(
                TokenFactory.BlockLabel("^entry"),
                new DelimitedSyntaxList<BlockArgumentSyntax>(null, new List<BlockArgumentSyntax>(), new List<Token>(), null),
                TokenFactory.Colon(),
                pendingEntryOperations.ToList()));
        }

        var closeBraceResult = ExpectTokenResult(TokenKind.RBrace, "Expected '}' to close a region.");
        if (!closeBraceResult.IsSuccess)
        {
            return ParseResult<RegionSyntax>.Failure(closeBraceResult.Diagnostic!);
        }

        return ParseResult<RegionSyntax>.Success(new RegionSyntax(openBraceToken, blocks, closeBraceResult.Value));
    }

    /// <summary>
    /// Parses a labeled MLIR block: <c>^label(args): op*</c>.
    /// Stops when the next token is <c>}</c> (region close) or a new <c>^block_label</c>.
    /// </summary>
    private ParseResult<BlockSyntax> TryParseBlockResult()
    {
        var labelResult = TryParseBlockLabelTokenResult();
        if (!labelResult.IsSuccess)
        {
            return ParseResult<BlockSyntax>.Failure(labelResult.Diagnostic!);
        }

        var argumentsResult = TryParseOptionalCommaSeparatedDelimitedList(
            TokenKind.LParen,
            TokenKind.RParen,
            TryParseBlockArgumentResult,
            "Expected ')' after block argument list.");
        if (!argumentsResult.IsSuccess)
        {
            return ParseResult<BlockSyntax>.Failure(argumentsResult.Diagnostic!);
        }

        var colonResult = ExpectTokenResult(TokenKind.Colon, "Expected ':' after block label.");
        if (!colonResult.IsSuccess)
        {
            return ParseResult<BlockSyntax>.Failure(colonResult.Diagnostic!);
        }

        var operations = new List<OperationSyntax>();
        while (!Is(TokenKind.RBrace) && !Is(TokenKind.BlockLabel))
        {
            var operationResult = TryParseOperationResult();
            if (!operationResult.IsSuccess)
            {
                return ParseResult<BlockSyntax>.Failure(operationResult.Diagnostic!);
            }

            operations.Add(operationResult.Value);
            var boundaryResult = EnsureOperationBoundaryResult(true);
            if (!boundaryResult.IsSuccess)
            {
                return ParseResult<BlockSyntax>.Failure(boundaryResult.Diagnostic!);
            }
        }

        return ParseResult<BlockSyntax>.Success(new BlockSyntax(
            labelResult.Value,
            argumentsResult.Value,
            colonResult.Value,
            operations));
    }

    /// <summary>
    /// Parses a single block argument: <c>%name : type</c>.
    /// </summary>
    private ParseResult<BlockArgumentSyntax> TryParseBlockArgumentResult()
    {
        var nameResult = TryParseSsaTokenResult();
        if (!nameResult.IsSuccess)
        {
            return ParseResult<BlockArgumentSyntax>.Failure(nameResult.Diagnostic!);
        }

        var colonResult = ExpectTokenResult(TokenKind.Colon, "Expected ':' after block argument name.");
        if (!colonResult.IsSuccess)
        {
            return ParseResult<BlockArgumentSyntax>.Failure(colonResult.Diagnostic!);
        }

        var typeResult = TryParseTypeSyntaxResult(TokenKind.Comma, TokenKind.RParen);
        if (!typeResult.IsSuccess)
        {
            return ParseResult<BlockArgumentSyntax>.Failure(typeResult.Diagnostic!);
        }

        return ParseResult<BlockArgumentSyntax>.Success(new BlockArgumentSyntax(nameResult.Value, colonResult.Value, typeResult.Value));
    }

    /// <summary>
    /// Returns a human-readable spelling of the supplied token kind, used in error messages.
    /// </summary>
    private static string TokenText(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.LParen => "(",
            TokenKind.RParen => ")",
            TokenKind.LessThan => "<",
            TokenKind.GreaterThan => ">",
            _ => kind.ToString(),
        };
    }

    /// <summary>
    /// Parses the operation name token, which is either a bare identifier (<c>dialect.op</c>)
    /// or a quoted string (<c>"dialect.op"</c>).
    /// </summary>
    private ParseResult<Token> TryParseOperationNameTokenResult()
    {
        if (!Is(TokenKind.Identifier) && !Is(TokenKind.StringLiteral))
        {
            return ParseResult<Token>.Failure(CreateDiagnostic("Expected an operation name."));
        }

        return ParseResult<Token>.Success(ConsumeToken());
    }

    /// <summary>
    /// Expects and consumes the current token as an SSA value name (<c>%name</c>).
    /// </summary>
    private ParseResult<Token> TryParseSsaTokenResult()
    {
        return ExpectTokenResult(TokenKind.SsaName, "Expected an SSA value name.");
    }

    /// <summary>
    /// Expects and consumes the current token as a block label name (<c>^label</c>).
    /// </summary>
    private ParseResult<Token> TryParseBlockLabelTokenResult()
    {
        return ExpectTokenResult(TokenKind.BlockLabel, "Expected a block label name.");
    }

    /// <summary>
    /// Scans raw tokens until one of the supplied delimiter token kinds is reached at depth zero,
    /// without stopping at operation boundaries.
    /// </summary>
    private ParseResult<RawSyntaxText> TryParseRawUntilDelimiterResult(params TokenKind[] delimiters)
    {
        return TryParseRawUntilDelimiterOrKeywordResult(delimiters, []);
    }

    /// <summary>
    /// Scans raw tokens until one of the supplied delimiter token kinds or keyword spellings is reached
    /// at depth zero, without stopping at operation boundaries.
    /// </summary>
    private ParseResult<RawSyntaxText> TryParseRawUntilDelimiterOrKeywordResult(TokenKind[] delimiters, string[] keywords)
    {
        return TryScanRawFragment(
            delimiters,
            keywords,
            stopAtOperationBoundary: false,
            allowEmpty: false,
            eofMessage: "Unexpected end of file while parsing raw syntax.");
    }

    /// <summary>
    /// Parses a required operand list of the form <c>( %a, %b, ... )</c>.
    /// </summary>
    private ParseResult<DelimitedSyntaxList<Token>> TryParseOperandsResult()
    {
        return TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LParen,
            TokenKind.RParen,
            TryParseSsaTokenResult,
            "Expected '(' for the operand list.",
            "Expected ')' to close the operand list.");
    }

    /// <summary>
    /// Parses an optional successor list of the form <c>[ ^bb1, ^bb2, ... ]</c>.
    /// Returns an empty list when no <c>[</c> is present.
    /// </summary>
    private ParseResult<DelimitedSyntaxList<Token>> TryParseSuccessorsResult()
    {
        if (!Is(TokenKind.LBracket))
        {
            return ParseResult<DelimitedSyntaxList<Token>>.Success(EmptyDelimitedSyntaxList<Token>());
        }

        return TryParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBracket,
            TokenKind.RBracket,
            TryParseBlockLabelTokenResult,
            "Expected '[' for the successor list.",
            "Expected ']' to close the successor list.");
    }
}
