namespace MLIR.Text;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Collections;
using MLIR.Syntax.Types.Collections;
using MLIR.Syntax.Types.Primitives;
using MLIR.Dialects.Attributes.Collections;
using MLIR.Dialects.Attributes.Primitives;

/// <summary>
/// Parses generic MLIR syntax into a concrete syntax tree.
/// </summary>
public sealed class Parser
{
    private readonly struct ParseMark
    {
        public ParseMark(int position)
        {
            Position = position;
        }

        public int Position { get; }
    }

    private readonly string source;
    private readonly IReadOnlyList<Token> tokens;
    private readonly DialectRegistry? dialectRegistry;
    private int position;
    private static readonly BooleanLiteralAttributeAssemblyFormat BooleanLiteralAttributeAssemblyFormat = new();
    private static readonly IntegerLiteralAttributeAssemblyFormat IntegerLiteralAttributeAssemblyFormat = new();
    private static readonly FloatingPointLiteralAttributeAssemblyFormat FloatingPointLiteralAttributeAssemblyFormat = new();
    private static readonly StringLiteralAttributeAssemblyFormat StringLiteralAttributeAssemblyFormat = new();
    private static readonly DenseIntegerArrayAttributeAssemblyFormat DenseArrayAttributeAssemblyFormat = new();
    private static readonly ElementsAttributeAssemblyFormat ElementsAttributeAssemblyFormat = new();

    private Parser(string source, DialectRegistry? dialectRegistry = null)
    {
        this.source = source;
        this.dialectRegistry = dialectRegistry;
        tokens = Lexer.Lex(source);
    }

    /// <summary>
    /// Parses a module from the supplied MLIR source text.
    /// </summary>
    /// <param name="source">The MLIR source text.</param>
    /// <returns>The parsed module syntax.</returns>
    public static ModuleSyntax ParseModule(string source)
    {
        return new Parser(source).ParseModuleCore();
    }

    /// <summary>
    /// Parses a module from the supplied MLIR source text, using registered dialects to recognize custom assembly formats.
    /// </summary>
    /// <param name="source">The MLIR source text.</param>
    /// <param name="dialectRegistry">The dialect registry used to recognize custom assembly formats.</param>
    /// <returns>The parsed module syntax.</returns>
    public static ModuleSyntax ParseModule(string source, DialectRegistry? dialectRegistry)
    {
        return new Parser(source, dialectRegistry).ParseModuleCore();
    }

    /// <summary>
    /// Parses a standalone attribute value from the supplied MLIR source text.
    /// </summary>
    public static AttributeValueSyntax ParseAttributeValue(string source, DialectRegistry? dialectRegistry = null, AttributeConstraintDefinition? expectedDefinition = null)
    {
        var parser = new Parser(source, dialectRegistry);
        var syntax = expectedDefinition != null
            ? parser.ParseAttributeValueSyntax(false, expectedDefinition)
            : parser.ParseAttributeValueSyntax(false, (AttributeConstraintDefinition?)null);
        if (!parser.Is(TokenKind.EndOfFile))
        {
            throw parser.Error("Expected the attribute value to consume the entire input.");
        }

        return syntax;
    }

    /// <summary>
    /// Parses a standalone type from the supplied MLIR source text.
    /// </summary>
    public static TypeSyntax ParseType(string source, DialectRegistry? dialectRegistry = null)
    {
        var parser = new Parser(source, dialectRegistry);
        var syntax = parser.ParseTypeSyntaxUntilOperationBoundary();
        if (!parser.Is(TokenKind.EndOfFile))
        {
            throw parser.Error("Expected the type to consume the entire input.");
        }

        return syntax;
    }

    private ModuleSyntax ParseModuleCore()
    {
        var operations = new List<OperationSyntax>();
        while (!Is(TokenKind.EndOfFile))
        {
            operations.Add(ParseOperation());
            EnsureOperationBoundary(false);
        }

        return new ModuleSyntax(operations, ToSyntaxToken(ConsumeToken()));
    }

    private OperationSyntax ParseOperation()
    {
        var resultTokens = new List<SyntaxToken>();
        var resultCommaTokens = new List<SyntaxToken>();
        SyntaxToken? equalsToken = null;

        if (Is(TokenKind.SsaName))
        {
            var firstResultToken = ParseSsaToken();
            resultTokens.Add(firstResultToken);

            if (TryMatch(TokenKind.Colon, out _))
            {
                var countToken = ExpectRawToken(TokenKind.Integer, "Expected result count after ':'.");
                var count = int.Parse(countToken.Text, CultureInfo.InvariantCulture);
                for (var i = 1; i < count; i++)
                {
                    resultTokens.Add(new SyntaxToken(firstResultToken.Text + "#" + i.ToString(CultureInfo.InvariantCulture)));
                }
            }

            while (TryMatch(TokenKind.Comma, out var resultCommaToken))
            {
                resultCommaTokens.Add(ToSyntaxToken(resultCommaToken));
                resultTokens.Add(ParseSsaToken());
            }

            equalsToken = ExpectToken(TokenKind.Equal, "Expected '=' after operation result list.");
        }

        var nameToken = ParseOperationNameToken();
        if (!nameToken.Text.StartsWith("\"", System.StringComparison.Ordinal)
            && TryParseCustomAssembly(nameToken, resultTokens, resultCommaTokens, equalsToken, out var customBody))
        {
            return new OperationSyntax(
                resultTokens,
                resultCommaTokens,
                equalsToken,
                nameToken,
                customBody);
        }

        if (!nameToken.Text.StartsWith("\"", System.StringComparison.Ordinal)
            && TryParseProjectedCustomLikeOperationBody(out var projectedBody))
        {
            return new OperationSyntax(
                resultTokens,
                resultCommaTokens,
                equalsToken,
                nameToken,
                projectedBody!);
        }

        var operands = ParseOperandsInternal();
        var successors = ParseSuccessorsInternal();

        var regions = new List<RegionSyntax>();
        while (Is(TokenKind.LBrace) && IsRegionStart())
        {
            regions.Add(ParseRegion());
        }

        var attributes = ParseAttrDictInternal();

        SyntaxToken? typeSignatureColonToken = null;
        TypeSyntax? typeSignatureSyntax = null;
        if (Is(TokenKind.Colon))
        {
            typeSignatureColonToken = ExpectToken(TokenKind.Colon, "Expected ':' before the type signature.");
            typeSignatureSyntax = ParseTypeSyntaxUntilOperationBoundary();
        }

        return new OperationSyntax(
            resultTokens,
            resultCommaTokens,
            equalsToken,
            nameToken,
            operands,
            successors,
            regions,
            attributes,
            typeSignatureColonToken,
            typeSignatureSyntax);
    }

    private bool TryParseProjectedCustomLikeOperationBody(out OperationBodySyntax? body)
    {
        body = null;
        var checkpoint = Mark();

        var operandTokens = new List<SyntaxToken>();
        var operandCommaTokens = new List<SyntaxToken>();
        if (Is(TokenKind.SsaName))
        {
            ParseCommaSeparatedItems(operandTokens, operandCommaTokens, ParseSsaToken);
        }

        var attributeDict = ParseAttrDictInternal();
        if (!TryMatch(TokenKind.Colon, out var colonToken))
        {
            Reset(checkpoint);
            return false;
        }

        var typeSignature = new RawTypeSyntax(ParseRawUntilOperationBoundaryInternal());
        body = new GenericOperationBodySyntax(
            new DelimitedSyntaxList<SyntaxToken>(
                new SyntaxToken("("),
                operandTokens,
                operandCommaTokens,
                new SyntaxToken(")")),
            new DelimitedSyntaxList<SyntaxToken>(null, new List<SyntaxToken>(), new List<SyntaxToken>(), null),
            new List<RegionSyntax>(),
            attributeDict,
            ToSyntaxToken(colonToken),
            typeSignature);
        return true;
    }

    private bool TryParseCustomAssembly(
        SyntaxToken nameToken,
        IReadOnlyList<SyntaxToken> resultTokens,
        IReadOnlyList<SyntaxToken> resultCommaTokens,
        SyntaxToken? equalsToken,
        out OperationBodySyntax body)
    {
        body = null!;
        if (dialectRegistry == null)
        {
            return false;
        }

        var normalizedName = NormalizeOperationName(nameToken.Text);
        if (!dialectRegistry.TryGetOperation(normalizedName, out var definition) || definition.AssemblyFormat == null)
        {
            return false;
        }

        var checkpoint = Mark();
        if (definition.AssemblyFormat.TryParse(
            nameToken,
            resultTokens,
            resultCommaTokens,
            equalsToken,
            new OperationParsingContext(this),
            out var customBody))
        {
            body = customBody!;
            return true;
        }

        Reset(checkpoint);
        return false;
    }

    private RegionSyntax ParseRegion()
    {
        var openBraceToken = ExpectToken(TokenKind.LBrace, "Expected '{' to start a region.");
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
                        new SyntaxToken("^entry"),
                        new DelimitedSyntaxList<BlockArgumentSyntax>(null, new List<BlockArgumentSyntax>(), new List<SyntaxToken>(), null),
                        new SyntaxToken(":"),
                        pendingEntryOperations.ToList()));
                    pendingEntryOperations.Clear();
                }

                blocks.Add(ParseBlock());
            }
            else
            {
                pendingEntryOperations.Add(ParseOperation());
                EnsureOperationBoundary(true);
            }
        }

        if (pendingEntryOperations.Count > 0 || blocks.Count == 0)
        {
            // Keep region bodies uniform even for empty regions and unlabeled entry operations.
            blocks.Insert(0, new BlockSyntax(
                new SyntaxToken("^entry"),
                new DelimitedSyntaxList<BlockArgumentSyntax>(null, new List<BlockArgumentSyntax>(), new List<SyntaxToken>(), null),
                new SyntaxToken(":"),
                pendingEntryOperations.ToList()));
        }

        var closeBraceToken = ExpectToken(TokenKind.RBrace, "Expected '}' to close a region.");
        return new RegionSyntax(openBraceToken, blocks, closeBraceToken);
    }

    private BlockSyntax ParseBlock()
    {
        var labelToken = ParseBlockLabelToken();
        var arguments = ParseOptionalCommaSeparatedDelimitedList(
            TokenKind.LParen,
            TokenKind.RParen,
            ParseBlockArgument,
            "Expected ')' after block argument list.");

        var colonToken = ExpectToken(TokenKind.Colon, "Expected ':' after block label.");
        var operations = new List<OperationSyntax>();
        while (!Is(TokenKind.RBrace) && !Is(TokenKind.BlockLabel))
        {
            operations.Add(ParseOperation());
            EnsureOperationBoundary(true);
        }

        return new BlockSyntax(
            labelToken,
            arguments,
            colonToken,
            operations);
    }

    private BlockArgumentSyntax ParseBlockArgument()
    {
        var nameToken = ParseSsaToken();
        var colonToken = ExpectToken(TokenKind.Colon, "Expected ':' after block argument name.");
        var type = ParseTypeSyntax(TokenKind.Comma, TokenKind.RParen);
        return new BlockArgumentSyntax(nameToken, colonToken, type);
    }

    private NamedAttributeSyntax ParseAttribute()
    {
        SyntaxToken nameToken;
        if (Is(TokenKind.Identifier) || Is(TokenKind.StringLiteral))
        {
            nameToken = ToSyntaxToken(ConsumeToken());
        }
        else
        {
            throw Error("Expected an attribute name.");
        }

        SyntaxToken separatorToken;
        if (TryMatch(TokenKind.Equal, out var equalsToken))
        {
            separatorToken = ToSyntaxToken(equalsToken);
        }
        else if (TryMatch(TokenKind.Colon, out var colonToken))
        {
            separatorToken = ToSyntaxToken(colonToken);
        }
        else
        {
            throw Error("Expected '=' or ':' after attribute name.");
        }

        var value = ParseAttributeValueSyntax(false, (AttributeConstraintDefinition?)null, TokenKind.Comma, TokenKind.RBrace);
        return new NamedAttributeSyntax(nameToken, separatorToken, value);
    }

    private AttributeValueSyntax ParseAttributeValueSyntax(bool stopAtOperationBoundary, string? expectedDefinitionName, params TokenKind[] stopBefore)
    {
        AttributeConstraintDefinition? expectedDefinition = null;
        if (!string.IsNullOrEmpty(expectedDefinitionName) && dialectRegistry != null)
        {
            dialectRegistry.TryResolveAttributeConstraint(expectedDefinitionName!, out expectedDefinition);
        }

        return ParseAttributeValueSyntax(stopAtOperationBoundary, expectedDefinition, stopBefore);
    }

    private AttributeValueSyntax ParseAttributeValueSyntax(bool stopAtOperationBoundary, AttributeConstraintDefinition? expectedDefinition, params TokenKind[] stopBefore)
    {
        if (expectedDefinition != null && TryParseCustomAttributeSyntax(expectedDefinition, out var syntax))
        {
            return syntax;
        }

        if (TryParseSelfIdentifyingAttributeSyntax(out syntax))
        {
            return syntax;
        }

        if (TryParseBuiltinStructuredAttributeSyntax(out syntax))
        {
            return syntax;
        }

        return new RawAttributeValueSyntax(
            stopAtOperationBoundary
                ? ParseRawUntilDelimiterOrBoundaryInternal(stopBefore)
                : ParseRawUntilDelimiter(stopBefore));
    }

    private TypeSyntax ParseTypeSyntax(params TokenKind[] stopBefore)
    {
        return ParseTypeSyntaxCore(stopBefore, stopAtOperationBoundary: false);
    }

    private TypeSyntax ParseTypeSyntax(string[] stopBeforeKeywords, params TokenKind[] stopBefore)
    {
        return ParseTypeSyntaxCore(stopBefore, stopBeforeKeywords, stopAtOperationBoundary: false);
    }

    private TypeSyntax ParseTypeSyntaxUntilOperationBoundary()
    {
        return ParseTypeSyntaxCore([], stopAtOperationBoundary: true);
    }

    private TypeSyntax ParseTypeSyntaxCore(TokenKind[] stopBefore, bool stopAtOperationBoundary)
    {
        return ParseTypeSyntaxCore(stopBefore, [], stopAtOperationBoundary);
    }

    private TypeSyntax ParseTypeSyntaxCore(TokenKind[] stopBefore, string[] stopBeforeKeywords, bool stopAtOperationBoundary)
    {
        if (TryParseBuiltinTypeSyntax(stopBefore, stopAtOperationBoundary, out var syntax))
        {
            return syntax;
        }

        if (TryParseCustomTypeSyntax(out syntax))
        {
            return syntax;
        }

        return new RawTypeSyntax(
            stopAtOperationBoundary
                ? ParseRawUntilDelimiterOrBoundaryInternal(stopBefore)
                : ParseRawUntilDelimiterOrKeyword(stopBefore, stopBeforeKeywords));
    }

    private bool TryParseCustomAttributeSyntax(string? expectedDefinitionName, out AttributeValueSyntax syntax)
    {
        syntax = null!;
        if (dialectRegistry == null)
        {
            return false;
        }

        var canonicalName = TryPeekAttributeDefinitionName();
        if (canonicalName == null)
        {
            return false;
        }

        return dialectRegistry.TryGetAttribute(canonicalName, out var definition)
            && TryParseCustomAttributeSyntax(definition, out syntax);
    }

    private bool TryParseSelfIdentifyingAttributeSyntax(out AttributeValueSyntax syntax)
    {
        syntax = null!;
        if (dialectRegistry == null)
        {
            return false;
        }

        var canonicalName = TryPeekAttributeDefinitionName();
        return canonicalName != null
            && dialectRegistry.TryGetAttribute(canonicalName, out var definition)
            && TryParseCustomAttributeSyntax(definition, out syntax);
    }

    private bool TryParseBuiltinStructuredAttributeSyntax(out AttributeValueSyntax syntax)
    {
        if (Is(TokenKind.LBracket))
        {
            syntax = ParseArrayAttributeValueSyntax();
            return true;
        }

        if (Is(TokenKind.LBrace))
        {
            syntax = new DictionaryAttributeValueSyntax(ParseAttrDictInternal());
            return true;
        }

        if (TryParseWith(BuiltinAttributeConstraintDefinition("DenseArrayAttr"), DenseArrayAttributeAssemblyFormat, out syntax))
        {
            return true;
        }

        if (TryParseWith(BuiltinAttributeConstraintDefinition("ElementsAttr"), ElementsAttributeAssemblyFormat, out syntax))
        {
            return true;
        }

        syntax = null!;
        return false;
    }

    private bool TryParseWith(AttributeConstraintDefinition? definition, IAttributeAssemblyFormat assemblyFormat, out AttributeValueSyntax syntax)
    {
        syntax = null!;
        var checkpoint = Mark();
        if (assemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition), out var customSyntax))
        {
            syntax = customSyntax!;
            return true;
        }

        Reset(checkpoint);
        return false;
    }

    private ArrayAttributeValueSyntax ParseArrayAttributeValueSyntax()
    {
        var list = ParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBracket,
            TokenKind.RBracket,
            () => ParseAttributeValueSyntax(false, (AttributeConstraintDefinition?)null, TokenKind.Comma, TokenKind.RBracket),
            "Expected '[' to start the array attribute.",
            "Expected ']' to close the array attribute.");
        return new ArrayAttributeValueSyntax(list.OpenToken!.Value, list.Items, list.SeparatorTokens, list.CloseToken!.Value);
    }

    private static AttributeConstraintDefinition BuiltinAttributeConstraintDefinition(string name)
    {
        return new AttributeConstraintDefinition(name);
    }

    private bool TryParseCustomAttributeSyntax(AttributeConstraintDefinition? definition, out AttributeValueSyntax syntax)
    {
        syntax = null!;
        if (definition?.AssemblyFormat == null)
        {
            return false;
        }

        var checkpoint = Mark();
        if (definition.AssemblyFormat.TryParse(new AttributeParsingContext(this, dialectRegistry, definition), out var customSyntax))
        {
            syntax = customSyntax!;
            return true;
        }

        Reset(checkpoint);
        return false;
    }

    private bool TryParseCustomTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (dialectRegistry == null)
        {
            return false;
        }

        var canonicalName = TryPeekTypeDefinitionName();
        if (canonicalName == null || !dialectRegistry.TryGetType(canonicalName, out var definition) || definition.AssemblyFormat == null)
        {
            return false;
        }

        var checkpoint = Mark();
        if (definition.AssemblyFormat.TryParse(new TypeParsingContext(this), out var customSyntax))
        {
            syntax = customSyntax!;
            return true;
        }

        Reset(checkpoint);
        return false;
    }

    private bool TryParseBuiltinTypeSyntax(TokenKind[] stopBefore, bool stopAtOperationBoundary, out TypeSyntax syntax)
    {
        syntax = null!;
        var checkpoint = Mark();
        if (TryParseFunctionTypeSyntax(stopBefore, stopAtOperationBoundary, out syntax)
            || TryParseTupleTypeSyntax(out syntax)
            || TryParseTensorTypeSyntax(out syntax)
            || TryParseVectorTypeSyntax(out syntax)
            || TryParseMemRefTypeSyntax(out syntax)
            || TryParseBuiltinPrimitiveTypeSyntax(out syntax))
        {
            return true;
        }

        Reset(checkpoint);
        return false;
    }

    private bool TryParseBuiltinPrimitiveTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (!Is(TokenKind.Identifier))
        {
            return false;
        }

        var token = ToSyntaxToken(ConsumeToken());
        if (TryParseBuiltinIntegerName(token.Text, out var signedness, out var width))
        {
            syntax = new BuiltinIntegerTypeSyntax(token, signedness, width);
            return true;
        }

        if (IsBuiltinFloatName(token.Text))
        {
            syntax = new BuiltinFloatTypeSyntax(token);
            return true;
        }

        if (token.Text == "index")
        {
            syntax = new BuiltinIndexTypeSyntax(token);
            return true;
        }

        if (token.Text == "none")
        {
            syntax = new BuiltinNoneTypeSyntax(token);
            return true;
        }

        position--;
        return false;
    }

    private bool TryParseFunctionTypeSyntax(TokenKind[] stopBefore, bool stopAtOperationBoundary, out TypeSyntax syntax)
    {
        syntax = null!;
        if (!Is(TokenKind.LParen))
        {
            return false;
        }

        var checkpoint = Mark();
        var inputs = ParseTypeList(TokenKind.LParen, TokenKind.RParen, stopAtOperationBoundary: false);
        if (!TryMatch(TokenKind.Arrow, out var arrowToken))
        {
            Reset(checkpoint);
            return false;
        }

        TypeSyntax? resultType = null;
        DelimitedSyntaxList<TypeSyntax> resultTypes;
        if (Is(TokenKind.LParen))
        {
            resultTypes = ParseTypeList(TokenKind.LParen, TokenKind.RParen, stopAtOperationBoundary);
        }
        else
        {
            resultTypes = new DelimitedSyntaxList<TypeSyntax>(null, [], [], null);
            resultType = ParseTypeSyntaxCore(stopBefore, stopAtOperationBoundary);
        }

        syntax = new FunctionTypeSyntax(inputs, ToSyntaxToken(arrowToken), resultType, resultTypes);
        return true;
    }

    private bool TryParseTupleTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (!IsKeyword("tuple"))
        {
            return false;
        }

        var keyword = ExpectKeywordInternal("tuple", "Expected 'tuple'.");
        var elements = ParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LessThan,
            TokenKind.GreaterThan,
            () => ParseTypeSyntax(TokenKind.Comma, TokenKind.GreaterThan),
            "Expected '<' after 'tuple'.",
            "Expected '>' to close the tuple type.");

        syntax = new TupleTypeSyntax(keyword, elements.OpenToken!.Value, elements.Items, elements.SeparatorTokens, elements.CloseToken!.Value);
        return true;
    }

    private bool TryParseTensorTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (!IsKeyword("tensor"))
        {
            return false;
        }

        var keyword = ExpectKeywordInternal("tensor", "Expected 'tensor'.");
        var lessThan = ExpectToken(TokenKind.LessThan, "Expected '<' after 'tensor'.");
        var prefix = ParseRawUntilDelimiter(TokenKind.Comma, TokenKind.GreaterThan);
        if (!TryParseShapedTypeBody(prefix.Text, allowUnranked: true, minimumDimensionCount: 0, out var dimensions, out var xTokens, out var unrankedToken, out var elementTypeText))
        {
            return false;
        }

        var elementType = ParseType(elementTypeText, dialectRegistry);
        var trailingCommaTokens = new List<SyntaxToken>();
        var trailingParameters = new List<RawSyntaxText>();
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            trailingCommaTokens.Add(ToSyntaxToken(comma));
            trailingParameters.Add(ParseRawUntilDelimiter(TokenKind.Comma, TokenKind.GreaterThan));
        }

        var greaterThan = ExpectToken(TokenKind.GreaterThan, "Expected '>' to close the tensor type.");
        syntax = new TensorTypeSyntax(keyword, lessThan, dimensions, xTokens, unrankedToken, elementType, trailingCommaTokens, trailingParameters, greaterThan);
        return true;
    }

    private bool TryParseVectorTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (!IsKeyword("vector"))
        {
            return false;
        }

        var checkpoint = Mark();
        var keyword = ExpectKeywordInternal("vector", "Expected 'vector'.");
        var lessThan = ExpectToken(TokenKind.LessThan, "Expected '<' after 'vector'.");
        var prefix = ParseRawUntilDelimiter(TokenKind.GreaterThan);
        if (!TryParseShapedTypeBody(prefix.Text, allowUnranked: false, minimumDimensionCount: 1, out var dimensions, out var xTokens, out _, out var elementTypeText))
        {
            Reset(checkpoint);
            return false;
        }

        var elementType = ParseType(elementTypeText, dialectRegistry);
        var greaterThan = ExpectToken(TokenKind.GreaterThan, "Expected '>' to close the vector type.");
        syntax = new VectorTypeSyntax(keyword, lessThan, dimensions, xTokens, elementType, greaterThan);
        return true;
    }

    private bool TryParseMemRefTypeSyntax(out TypeSyntax syntax)
    {
        syntax = null!;
        if (!IsKeyword("memref"))
        {
            return false;
        }

        var keyword = ExpectKeywordInternal("memref", "Expected 'memref'.");
        var lessThan = ExpectToken(TokenKind.LessThan, "Expected '<' after 'memref'.");
        var prefix = ParseRawUntilDelimiter(TokenKind.Comma, TokenKind.GreaterThan);
        if (!TryParseShapedTypeBody(prefix.Text, allowUnranked: true, minimumDimensionCount: 0, out var dimensions, out var xTokens, out var unrankedToken, out var elementTypeText))
        {
            return false;
        }

        var elementType = ParseType(elementTypeText, dialectRegistry);
        var trailingCommaTokens = new List<SyntaxToken>();
        var trailingParameters = new List<RawSyntaxText>();
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            trailingCommaTokens.Add(ToSyntaxToken(comma));
            trailingParameters.Add(ParseRawUntilDelimiter(TokenKind.Comma, TokenKind.GreaterThan));
        }

        var greaterThan = ExpectToken(TokenKind.GreaterThan, "Expected '>' to close the memref type.");
        syntax = new MemRefTypeSyntax(keyword, lessThan, dimensions, xTokens, unrankedToken, elementType, trailingCommaTokens, trailingParameters, greaterThan);
        return true;
    }

    private static bool TryParseShapedTypeBody(
        string text,
        bool allowUnranked,
        int minimumDimensionCount,
        out List<ShapedTypeDimensionSyntax> dimensions,
        out List<SyntaxToken> xTokens,
        out SyntaxToken? unrankedToken,
        out string elementTypeText)
    {
        dimensions = [];
        xTokens = [];
        unrankedToken = null;
        elementTypeText = string.Empty;

        if (allowUnranked && text.StartsWith("*x", System.StringComparison.Ordinal))
        {
            unrankedToken = new SyntaxToken("*");
            xTokens.Add(new SyntaxToken("x"));
            elementTypeText = text.Substring(2);
            return elementTypeText.Length > 0;
        }

        var index = 0;
        while (index < text.Length)
        {
            if (text[index] == '?')
            {
                dimensions.Add(new DynamicShapedTypeDimensionSyntax(new SyntaxToken("?")));
                index++;
            }
            else if (char.IsDigit(text[index]))
            {
                var start = index;
                while (index < text.Length && char.IsDigit(text[index]))
                {
                    index++;
                }

                var digits = text.Substring(start, index - start);
                dimensions.Add(new StaticShapedTypeDimensionSyntax(new SyntaxToken(digits), long.Parse(digits)));
            }
            else
            {
                break;
            }

            if (index >= text.Length || text[index] != 'x')
            {
                return false;
            }

            xTokens.Add(new SyntaxToken("x"));
            index++;
        }

        if (dimensions.Count < minimumDimensionCount)
        {
            return false;
        }

        elementTypeText = text.Substring(index);
        return elementTypeText.Length > 0;
    }

    private DelimitedSyntaxList<TypeSyntax> ParseTypeList(TokenKind openKind, TokenKind closeKind, bool stopAtOperationBoundary)
    {
        return ParseRequiredCommaSeparatedDelimitedList(
            openKind,
            closeKind,
            () => ParseTypeSyntaxCore([TokenKind.Comma, closeKind], stopAtOperationBoundary),
            $"Expected '{TokenText(openKind)}' to start the type list.",
            $"Expected '{TokenText(closeKind)}' to close the type list.");
    }

    private static bool TryParseBuiltinIntegerName(string text, out IntegerTypeSignedness signedness, out int width)
    {
        signedness = IntegerTypeSignedness.Signless;
        width = 0;

        string digits;
        if (text.Length > 1 && text[0] == 'i')
        {
            digits = text.Substring(1);
        }
        else if (text.Length > 2 && text[0] == 's' && text[1] == 'i')
        {
            signedness = IntegerTypeSignedness.Signed;
            digits = text.Substring(2);
        }
        else if (text.Length > 2 && text[0] == 'u' && text[1] == 'i')
        {
            signedness = IntegerTypeSignedness.Unsigned;
            digits = text.Substring(2);
        }
        else
        {
            return false;
        }

        return int.TryParse(digits, out width);
    }

    private static bool IsBuiltinFloatName(string text)
    {
        return text is "bf16" or "f16" or "f32" or "f64" or "f80" or "f128" or "tf32";
    }

    private bool IsKeyword(string text)
    {
        return Is(TokenKind.Identifier) && Current.Text == text;
    }

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

    private SyntaxToken ParseOperationNameToken()
    {
        if (!Is(TokenKind.Identifier) && !Is(TokenKind.StringLiteral))
        {
            throw Error("Expected an operation name.");
        }

        return ToSyntaxToken(ConsumeToken());
    }

    private SyntaxToken ParseSsaToken()
    {
        return ExpectToken(TokenKind.SsaName, "Expected an SSA value name.");
    }

    private SyntaxToken ParseBlockLabelToken()
    {
        return ExpectToken(TokenKind.BlockLabel, "Expected a block label name.");
    }

    private RawSyntaxText ParseRawUntilDelimiter(params TokenKind[] delimiters)
    {
        return ParseRawUntilDelimiterOrKeyword(delimiters, []);
    }

    private RawSyntaxText ParseRawUntilDelimiterOrKeyword(TokenKind[] delimiters, string[] keywords)
    {
        return ScanRawFragment(
            delimiters,
            keywords,
            stopAtOperationBoundary: false,
            allowEmpty: false,
            eofMessage: "Unexpected end of file while parsing raw syntax.");
    }

    private RawSyntaxText ParseRawUntilOperationBoundary()
    {
        return ScanRawFragment([], [], stopAtOperationBoundary: true, allowEmpty: false, eofMessage: null);
    }

    private bool IsRegionStart()
    {
        if (!Is(TokenKind.LBrace))
        {
            return false;
        }

        // A '{' can start either a region or an attribute dictionary. Peek ahead to decide
        // which production we are looking at without consuming any tokens.
        var lookahead = position + 1;
        if (tokens[lookahead].Kind == TokenKind.RBrace)
        {
            return false;
        }

        if (tokens[lookahead].Kind == TokenKind.BlockLabel || tokens[lookahead].Kind == TokenKind.StringLiteral || tokens[lookahead].Kind == TokenKind.SsaName)
        {
            return true;
        }

        if (tokens[lookahead].Kind != TokenKind.Identifier)
        {
            return false;
        }

        var secondLookahead = tokens[lookahead + 1];
        return secondLookahead.Kind != TokenKind.Equal
            && secondLookahead.Kind != TokenKind.Colon
            && secondLookahead.Kind != TokenKind.Comma;
    }

    private void EnsureOperationBoundary(bool allowBlockStart)
    {
        if (!IsOperationBoundary(Current, allowBlockStart))
        {
            throw Error("Expected the end of the operation.");
        }
    }

    private bool IsOperationBoundary(Token token, bool allowBlockStart)
    {
        if (token.Kind == TokenKind.EndOfFile || token.Kind == TokenKind.RBrace)
        {
            return true;
        }

        if (allowBlockStart && token.Kind == TokenKind.BlockLabel && token.LeadingTrivia.Contains('\n'))
        {
            return true;
        }

        return token.LeadingTrivia.Contains('\n');
    }

    private static void UpdateDepth(TokenKind kind, ref int depthParen, ref int depthBrace, ref int depthBracket, ref int depthAngle)
    {
        switch (kind)
        {
            case TokenKind.LParen:
                depthParen++;
                break;
            case TokenKind.RParen:
                depthParen--;
                break;
            case TokenKind.LBrace:
                depthBrace++;
                break;
            case TokenKind.RBrace:
                depthBrace--;
                break;
            case TokenKind.LBracket:
                depthBracket++;
                break;
            case TokenKind.RBracket:
                depthBracket--;
                break;
            case TokenKind.LessThan:
                depthAngle++;
                break;
            case TokenKind.GreaterThan:
                depthAngle--;
                break;
        }
    }

    private static List<SyntaxToken> CreateSyntaxTokenList(IReadOnlyList<Token> tokens, int start, int end)
    {
        var result = new List<SyntaxToken>(end - start);
        for (var i = start; i < end; i++)
        {
            result.Add(ToSyntaxToken(tokens[i]));
        }

        return result;
    }

    private bool TryMatch(TokenKind kind, out Token token)
    {
        if (Current.Kind != kind)
        {
            token = default;
            return false;
        }

        token = ConsumeToken();
        return true;
    }

    private SyntaxToken ExpectToken(TokenKind kind, string message)
    {
        return ToSyntaxToken(ExpectRawToken(kind, message));
    }

    private Token ExpectRawToken(TokenKind kind, string message)
    {
        if (!TryMatch(kind, out var token))
        {
            throw Error(message);
        }

        return token;
    }

    private bool Is(TokenKind kind)
    {
        return Current.Kind == kind;
    }

    private ParseMark Mark()
    {
        return new ParseMark(position);
    }

    private void Reset(ParseMark mark)
    {
        position = mark.Position;
    }

    private void ParseCommaSeparatedItems<T>(
        List<T> items,
        List<SyntaxToken> separators,
        Func<T> parseElement)
    {
        items.Add(parseElement());
        while (TryMatch(TokenKind.Comma, out var comma))
        {
            separators.Add(ToSyntaxToken(comma));
            items.Add(parseElement());
        }
    }

    private DelimitedSyntaxList<T> ParseRequiredCommaSeparatedDelimitedList<T>(
        TokenKind openKind,
        TokenKind closeKind,
        Func<T> parseElement,
        string openMessage,
        string closeMessage)
    {
        var openToken = ExpectToken(openKind, openMessage);
        return ParseCommaSeparatedDelimitedListCore(openToken, closeKind, parseElement, closeMessage);
    }

    private DelimitedSyntaxList<T> ParseOptionalCommaSeparatedDelimitedList<T>(
        TokenKind openKind,
        TokenKind closeKind,
        Func<T> parseElement,
        string closeMessage)
    {
        if (!TryMatch(openKind, out var openToken))
        {
            return EmptyDelimitedSyntaxList<T>();
        }

        return ParseCommaSeparatedDelimitedListCore(ToSyntaxToken(openToken), closeKind, parseElement, closeMessage);
    }

    private DelimitedSyntaxList<T> ParseCommaSeparatedDelimitedListCore<T>(
        SyntaxToken openToken,
        TokenKind closeKind,
        Func<T> parseElement,
        string closeMessage)
    {
        var items = new List<T>();
        var separators = new List<SyntaxToken>();
        if (!TryMatch(closeKind, out var closeToken))
        {
            ParseCommaSeparatedItems(items, separators, parseElement);
            closeToken = ExpectRawToken(closeKind, closeMessage);
        }

        return new DelimitedSyntaxList<T>(openToken, items, separators, ToSyntaxToken(closeToken));
    }

    private RawSyntaxText ScanRawFragment(
        TokenKind[] delimiters,
        string[] keywords,
        bool stopAtOperationBoundary,
        bool allowEmpty,
        string? eofMessage)
    {
        var firstTokenIndex = position;
        var depthParen = 0;
        var depthBrace = 0;
        var depthBracket = 0;
        var depthAngle = 0;

        // Raw syntax fragments may themselves contain nested delimiters, so only stop when
        // we reach a requested delimiter or operation boundary at the outermost nesting level.
        while (true)
        {
            if (depthParen == 0 && depthBrace == 0 && depthBracket == 0 && depthAngle == 0)
            {
                if (IsAnyDelimiter(delimiters, Current.Kind))
                {
                    break;
                }

                if (Current.Kind == TokenKind.Identifier && IsAnyKeyword(keywords, Current.Text))
                {
                    break;
                }

                if (stopAtOperationBoundary && IsOperationBoundary(Current, false))
                {
                    break;
                }
            }

            if (Is(TokenKind.EndOfFile))
            {
                if (eofMessage != null)
                {
                    throw Error(eofMessage);
                }

                break;
            }

            UpdateDepth(Current.Kind, ref depthParen, ref depthBrace, ref depthBracket, ref depthAngle);
            ConsumeToken();
        }

        if (position == firstTokenIndex)
        {
            return allowEmpty
                ? new RawSyntaxText(new List<SyntaxToken>(), string.Empty)
                : throw Error("Expected raw syntax.");
        }

        var firstToken = tokens[firstTokenIndex];
        var end = tokens[position - 1].End;

        return new RawSyntaxText(
            CreateSyntaxTokenList(tokens, firstTokenIndex, position),
            source.Substring(firstToken.TokenStart, end - firstToken.TokenStart));
    }

    private static bool IsAnyDelimiter(TokenKind[] delimiters, TokenKind kind)
    {
        for (var i = 0; i < delimiters.Length; i++)
        {
            if (delimiters[i] == kind)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAnyKeyword(string[] keywords, string text)
    {
        for (var i = 0; i < keywords.Length; i++)
        {
            if (string.Equals(keywords[i], text, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static DelimitedSyntaxList<T> EmptyDelimitedSyntaxList<T>()
    {
        return new DelimitedSyntaxList<T>(null, new List<T>(), new List<SyntaxToken>(), null);
    }

    private Token ConsumeToken()
    {
        var token = Current;
        position++;
        return token;
    }

    private ParseException Error(string message)
    {
        return new ParseException(new Diagnostic(message, Current.Line, Current.Column));
    }

    internal static SyntaxToken ToSyntaxToken(Token token)
    {
        return new SyntaxToken(token.Text, token.LeadingTrivia, token.Line, token.Column);
    }

    internal bool IsToken(TokenKind kind)
    {
        return Is(kind);
    }

    internal bool TryMatchToken(TokenKind kind, out Token token)
    {
        return TryMatch(kind, out token);
    }

    internal SyntaxToken ExpectTokenInternal(TokenKind kind, string message)
    {
        return ExpectToken(kind, message);
    }

    internal SyntaxToken ParseSsaTokenInternal()
    {
        return ParseSsaToken();
    }

    internal SyntaxToken ParseBlockLabelTokenInternal()
    {
        return ParseBlockLabelToken();
    }

    internal RegionSyntax ParseRegionInternal()
    {
        return ParseRegion();
    }

    internal NamedAttributeSyntax ParseAttributeInternal()
    {
        return ParseAttribute();
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(false, (AttributeDefinition?)null, delimiters);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(false, expectedDefinitionName, delimiters);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(false, expectedDefinition, delimiters);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(true, (AttributeDefinition?)null, delimiters);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(string? expectedDefinitionName, params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(true, expectedDefinitionName, delimiters);
    }

    internal AttributeValueSyntax ParseAttributeValueSyntaxOrBoundaryInternal(AttributeConstraintDefinition expectedDefinition, params TokenKind[] delimiters)
    {
        return ParseAttributeValueSyntax(true, expectedDefinition, delimiters);
    }

    internal TypeSyntax ParseTypeSyntaxInternal(params TokenKind[] delimiters)
    {
        return ParseTypeSyntax(delimiters);
    }

    internal TypeSyntax ParseTypeSyntaxInternal(string[] stopBeforeKeywords, params TokenKind[] delimiters)
    {
        return ParseTypeSyntax(stopBeforeKeywords, delimiters);
    }

    internal TypeSyntax ParseTypeSyntaxUntilOperationBoundaryInternal()
    {
        return ParseTypeSyntaxUntilOperationBoundary();
    }

    internal RawSyntaxText ParseRawUntilDelimiterInternal(params TokenKind[] delimiters)
    {
        return ParseRawUntilDelimiter(delimiters);
    }

    internal RawSyntaxText ParseRawUntilDelimiterOrKeywordInternal(string[] keywords, params TokenKind[] delimiters)
    {
        return ParseRawUntilDelimiterOrKeyword(delimiters, keywords);
    }

    internal RawSyntaxText ParseRawUntilOperationBoundaryInternal()
    {
        return ParseRawUntilOperationBoundary();
    }

    internal RawSyntaxText ParseRawUntilDelimiterOrBoundaryInternal(params TokenKind[] delimiters)
    {
        return ScanRawFragment(delimiters, [], stopAtOperationBoundary: true, allowEmpty: true, eofMessage: null);
    }

    internal DelimitedSyntaxList<NamedAttributeSyntax> ParseAttrDictInternal()
    {
        if (!Is(TokenKind.LBrace))
        {
            return EmptyDelimitedSyntaxList<NamedAttributeSyntax>();
        }

        return ParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBrace,
            TokenKind.RBrace,
            ParseAttribute,
            "Expected '{' to start the attribute dictionary.",
            "Expected '}' to close the attribute dictionary.");
    }

    internal DelimitedSyntaxList<NamedAttributeSyntax> ParseAttrDictWithKeywordInternal()
    {
        if (!Is(TokenKind.Identifier) || !string.Equals(Current.Text, "attributes", System.StringComparison.Ordinal))
        {
            return EmptyDelimitedSyntaxList<NamedAttributeSyntax>();
        }

        ConsumeToken();
        return ParseAttrDictInternal();
    }

    internal SyntaxToken ExpectKeywordInternal(string spelling, string message)
    {
        if (!Is(TokenKind.Identifier) || !string.Equals(Current.Text, spelling, System.StringComparison.Ordinal))
        {
            throw Error(message);
        }

        return ToSyntaxToken(ConsumeToken());
    }

    internal IReadOnlyList<RegionSyntax> ParseRegionsInternal()
    {
        var regions = new List<RegionSyntax>();
        while (Is(TokenKind.LBrace))
        {
            regions.Add(ParseRegion());
        }

        return regions;
    }

    internal DelimitedSyntaxList<SyntaxToken> ParseSuccessorsInternal()
    {
        if (!Is(TokenKind.LBracket))
        {
            return EmptyDelimitedSyntaxList<SyntaxToken>();
        }

        return ParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LBracket,
            TokenKind.RBracket,
            ParseBlockLabelToken,
            "Expected '[' for the successor list.",
            "Expected ']' to close the successor list.");
    }

    internal DelimitedSyntaxList<SyntaxToken> ParseOperandsInternal()
    {
        return ParseRequiredCommaSeparatedDelimitedList(
            TokenKind.LParen,
            TokenKind.RParen,
            ParseSsaToken,
            "Expected '(' for the operand list.",
            "Expected ')' to close the operand list.");
    }

    internal bool IsKeywordInternal(string spelling)
    {
        return Is(TokenKind.Identifier) && string.Equals(Current.Text, spelling, System.StringComparison.Ordinal);
    }

    private string? TryPeekAttributeDefinitionName()
    {
        if (!Is(TokenKind.Hash))
        {
            return null;
        }

        var lookahead = position + 1;
        return lookahead < tokens.Count && tokens[lookahead].Kind == TokenKind.Identifier
            ? tokens[lookahead].Text
            : null;
    }

    private string? TryPeekTypeDefinitionName()
    {
        if (Is(TokenKind.Identifier))
        {
            return Current.Text;
        }

        if (Is(TokenKind.Bang))
        {
            var lookahead = position + 1;
            return lookahead < tokens.Count && tokens[lookahead].Kind == TokenKind.Identifier
                ? tokens[lookahead].Text
                : null;
        }

        return null;
    }

    private static string NormalizeOperationName(string name)
    {
        return name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"' ? name.Substring(1, name.Length - 2) : name;
    }

    private Token Current => tokens[position];
}
