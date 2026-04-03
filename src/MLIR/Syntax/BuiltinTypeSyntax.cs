using MLIR.Semantics;

namespace MLIR.Syntax;

/// <summary>
/// Identifies the signedness marker used by builtin integer types.
/// </summary>
public enum IntegerTypeSignedness
{
    Signless,
    Signed,
    Unsigned,
}

/// <summary>
/// Represents one dimension in a ranked builtin shaped type.
/// </summary>
public abstract class ShapedTypeDimensionSyntax
{
    /// <summary>
    /// Attempts to project this dimension into preserved raw syntax text.
    /// </summary>
    public abstract bool TryGetRawText(out RawSyntaxText? rawText);

    /// <summary>
    /// Gets the preserved raw syntax text for this dimension.
    /// </summary>
    public RawSyntaxText GetRawText()
    {
        if (TryGetRawText(out var rawText))
        {
            return rawText!;
        }

        throw new InvalidOperationException("This shaped-type dimension does not provide a raw syntax-text projection.");
    }

    /// <summary>
    /// Writes this dimension to the supplied syntax writer.
    /// </summary>
    public abstract void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia);

    /// <summary>
    /// Gets the source location of this dimension, if known.
    /// </summary>
    public virtual SourceLocation Location => GetRawText().Location;
}

/// <summary>
/// Represents a static integer dimension in a shaped type.
/// </summary>
public sealed class StaticShapedTypeDimensionSyntax(SyntaxToken sizeToken, long size) : ShapedTypeDimensionSyntax
{
    /// <summary>
    /// Gets the size token.
    /// </summary>
    public SyntaxToken SizeToken { get; } = sizeToken;

    /// <summary>
    /// Gets the parsed dimension size.
    /// </summary>
    public long Size { get; } = size;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText([SizeToken]);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(SizeToken, defaultLeadingTrivia);
    }
}

/// <summary>
/// Represents a dynamic dimension marker (<c>?</c>) in a shaped type.
/// </summary>
public sealed class DynamicShapedTypeDimensionSyntax(SyntaxToken questionToken) : ShapedTypeDimensionSyntax
{
    /// <summary>
    /// Gets the question-mark token.
    /// </summary>
    public SyntaxToken QuestionToken { get; } = questionToken;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText([QuestionToken]);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(QuestionToken, defaultLeadingTrivia);
    }
}

/// <summary>
/// Represents a builtin integer type such as <c>i32</c>, <c>si64</c>, or <c>ui8</c>.
/// </summary>
public sealed class BuiltinIntegerTypeSyntax(SyntaxToken nameToken, IntegerTypeSignedness signedness, int width) : TypeSyntax
{
    /// <summary>
    /// Gets the original identifier token.
    /// </summary>
    public SyntaxToken NameToken { get; } = nameToken;

    /// <summary>
    /// Gets the signedness marker.
    /// </summary>
    public IntegerTypeSignedness Signedness { get; } = signedness;

    /// <summary>
    /// Gets the integer bit width.
    /// </summary>
    public int Width { get; } = width;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText([NameToken]);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(NameToken, defaultLeadingTrivia);
    }
}

/// <summary>
/// Represents a builtin floating-point type such as <c>f32</c> or <c>bf16</c>.
/// </summary>
public sealed class BuiltinFloatTypeSyntax(SyntaxToken nameToken) : TypeSyntax
{
    /// <summary>
    /// Gets the original identifier token.
    /// </summary>
    public SyntaxToken NameToken { get; } = nameToken;

    /// <summary>
    /// Gets the canonical builtin type name.
    /// </summary>
    public string Name => NameToken.Text;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText([NameToken]);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(NameToken, defaultLeadingTrivia);
    }
}

/// <summary>
/// Represents the builtin <c>index</c> type.
/// </summary>
public sealed class BuiltinIndexTypeSyntax(SyntaxToken keywordToken) : TypeSyntax
{
    /// <summary>
    /// Gets the keyword token.
    /// </summary>
    public SyntaxToken KeywordToken { get; } = keywordToken;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = new RawSyntaxText([KeywordToken]);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(KeywordToken, defaultLeadingTrivia);
    }
}

/// <summary>
/// Represents a tuple type such as <c>tuple&lt;i32, f32&gt;</c>.
/// </summary>
public sealed class TupleTypeSyntax(
    SyntaxToken keywordToken,
    SyntaxToken lessThanToken,
    IReadOnlyList<TypeSyntax> elements,
    IReadOnlyList<SyntaxToken> commaTokens,
    SyntaxToken greaterThanToken) : TypeSyntax
{
    /// <summary>
    /// Gets the keyword token.
    /// </summary>
    public SyntaxToken KeywordToken { get; } = keywordToken;

    /// <summary>
    /// Gets the opening angle-bracket token.
    /// </summary>
    public SyntaxToken LessThanToken { get; } = lessThanToken;

    /// <summary>
    /// Gets the tuple element types.
    /// </summary>
    public IReadOnlyList<TypeSyntax> Elements { get; } = elements;

    /// <summary>
    /// Gets the separator tokens between tuple elements.
    /// </summary>
    public IReadOnlyList<SyntaxToken> CommaTokens { get; } = commaTokens;

    /// <summary>
    /// Gets the closing angle-bracket token.
    /// </summary>
    public SyntaxToken GreaterThanToken { get; } = greaterThanToken;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = SyntaxTextComposer.Compose(KeywordToken, LessThanToken, Interleave(Elements, CommaTokens), GreaterThanToken);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(KeywordToken, defaultLeadingTrivia);
        writer.WriteToken(LessThanToken, string.Empty);
        WriteSeparatedTypes(writer, Elements, CommaTokens);
        writer.WriteToken(GreaterThanToken, string.Empty);
    }

    private static IEnumerable<object> Interleave(IReadOnlyList<TypeSyntax> items, IReadOnlyList<SyntaxToken> separators)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                yield return separators[i - 1];
            }

            yield return items[i];
        }
    }

    private static void WriteSeparatedTypes(Text.SyntaxWriter writer, IReadOnlyList<TypeSyntax> items, IReadOnlyList<SyntaxToken> separators)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                writer.WriteToken(separators[i - 1], string.Empty);
            }

            items[i].WriteTo(writer, i > 0 ? " " : string.Empty);
        }
    }
}

/// <summary>
/// Represents a builtin function type such as <c>(i32) -> i64</c>.
/// </summary>
public sealed class FunctionTypeSyntax(
    DelimitedSyntaxList<TypeSyntax> inputTypes,
    SyntaxToken arrowToken,
    TypeSyntax? resultType,
    DelimitedSyntaxList<TypeSyntax> resultTypes) : TypeSyntax
{
    /// <summary>
    /// Gets the input type list.
    /// </summary>
    public DelimitedSyntaxList<TypeSyntax> InputTypes { get; } = inputTypes;

    /// <summary>
    /// Gets the arrow token.
    /// </summary>
    public SyntaxToken ArrowToken { get; } = arrowToken;

    /// <summary>
    /// Gets the single bare result type when the result was not parenthesized.
    /// </summary>
    public TypeSyntax? ResultType { get; } = resultType;

    /// <summary>
    /// Gets the parenthesized result type list when present.
    /// </summary>
    public DelimitedSyntaxList<TypeSyntax> ResultTypes { get; } = resultTypes;

    /// <summary>
    /// Gets a value indicating whether the result list is parenthesized.
    /// </summary>
    public bool HasDelimitedResults => ResultTypes.IsPresent;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        rawText = SyntaxTextComposer.Compose(
            InputTypes.OpenToken,
            Interleave(InputTypes.Items, InputTypes.SeparatorTokens),
            InputTypes.CloseToken,
            ArrowToken,
            HasDelimitedResults
                ? SyntaxTextComposer.Compose(ResultTypes.OpenToken, Interleave(ResultTypes.Items, ResultTypes.SeparatorTokens), ResultTypes.CloseToken)
                : ResultType);
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteDelimitedList(InputTypes, defaultLeadingTrivia);
        writer.WriteToken(ArrowToken, " ");
        if (HasDelimitedResults)
        {
            writer.WriteDelimitedList(ResultTypes, " ");
        }
        else
        {
            ResultType!.WriteTo(writer, " ");
        }
    }

    private static IEnumerable<object> Interleave(IReadOnlyList<TypeSyntax> items, IReadOnlyList<SyntaxToken> separators)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                yield return separators[i - 1];
            }

            yield return items[i];
        }
    }
}

/// <summary>
/// Represents a builtin tensor type.
/// </summary>
public sealed class TensorTypeSyntax(
    SyntaxToken keywordToken,
    SyntaxToken lessThanToken,
    IReadOnlyList<ShapedTypeDimensionSyntax> dimensions,
    IReadOnlyList<SyntaxToken> xTokens,
    SyntaxToken? unrankedToken,
    TypeSyntax elementType,
    IReadOnlyList<SyntaxToken> trailingCommaTokens,
    IReadOnlyList<RawSyntaxText> trailingParameters,
    SyntaxToken greaterThanToken) : TypeSyntax
{
    public SyntaxToken KeywordToken { get; } = keywordToken;
    public SyntaxToken LessThanToken { get; } = lessThanToken;
    public IReadOnlyList<ShapedTypeDimensionSyntax> Dimensions { get; } = dimensions;
    public IReadOnlyList<SyntaxToken> XTokens { get; } = xTokens;
    public SyntaxToken? UnrankedToken { get; } = unrankedToken;
    public TypeSyntax ElementType { get; } = elementType;
    public IReadOnlyList<SyntaxToken> TrailingCommaTokens { get; } = trailingCommaTokens;
    public IReadOnlyList<RawSyntaxText> TrailingParameters { get; } = trailingParameters;
    public SyntaxToken GreaterThanToken { get; } = greaterThanToken;

    public bool IsUnranked => UnrankedToken.HasValue;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        var parts = new List<object?> { KeywordToken, LessThanToken };
        AppendShapedPrefix(parts);
        parts.Add(ElementType);
        AppendTrailing(parts);
        parts.Add(GreaterThanToken);
        rawText = SyntaxTextComposer.Compose(parts.ToArray());
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(KeywordToken, defaultLeadingTrivia);
        writer.WriteToken(LessThanToken, string.Empty);
        WriteShapedPrefix(writer);
        ElementType.WriteTo(writer, string.Empty);
        WriteTrailing(writer);
        writer.WriteToken(GreaterThanToken, string.Empty);
    }

    private void AppendShapedPrefix(List<object?> parts)
    {
        if (IsUnranked)
        {
            parts.Add(UnrankedToken);
            parts.Add(XTokens[0]);
            return;
        }

        for (var i = 0; i < Dimensions.Count; i++)
        {
            parts.Add(Dimensions[i]);
            parts.Add(XTokens[i]);
        }
    }

    private void WriteShapedPrefix(Text.SyntaxWriter writer)
    {
        if (IsUnranked)
        {
            writer.WriteToken(UnrankedToken!.Value, string.Empty);
            writer.WriteToken(XTokens[0], string.Empty);
            return;
        }

        for (var i = 0; i < Dimensions.Count; i++)
        {
            Dimensions[i].WriteTo(writer, string.Empty);
            writer.WriteToken(XTokens[i], string.Empty);
        }
    }

    private void AppendTrailing(List<object?> parts)
    {
        for (var i = 0; i < TrailingParameters.Count; i++)
        {
            parts.Add(TrailingCommaTokens[i]);
            parts.Add(TrailingParameters[i]);
        }
    }

    private void WriteTrailing(Text.SyntaxWriter writer)
    {
        for (var i = 0; i < TrailingParameters.Count; i++)
        {
            writer.WriteToken(TrailingCommaTokens[i], string.Empty);
            writer.WriteRaw(TrailingParameters[i], " ");
        }
    }
}

/// <summary>
/// Represents a builtin vector type.
/// </summary>
public sealed class VectorTypeSyntax(
    SyntaxToken keywordToken,
    SyntaxToken lessThanToken,
    IReadOnlyList<ShapedTypeDimensionSyntax> dimensions,
    IReadOnlyList<SyntaxToken> xTokens,
    TypeSyntax elementType,
    SyntaxToken greaterThanToken) : TypeSyntax
{
    public SyntaxToken KeywordToken { get; } = keywordToken;
    public SyntaxToken LessThanToken { get; } = lessThanToken;
    public IReadOnlyList<ShapedTypeDimensionSyntax> Dimensions { get; } = dimensions;
    public IReadOnlyList<SyntaxToken> XTokens { get; } = xTokens;
    public TypeSyntax ElementType { get; } = elementType;
    public SyntaxToken GreaterThanToken { get; } = greaterThanToken;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        var parts = new List<object?> { KeywordToken, LessThanToken };
        for (var i = 0; i < Dimensions.Count; i++)
        {
            parts.Add(Dimensions[i]);
            parts.Add(XTokens[i]);
        }

        parts.Add(ElementType);
        parts.Add(GreaterThanToken);
        rawText = SyntaxTextComposer.Compose(parts.ToArray());
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(KeywordToken, defaultLeadingTrivia);
        writer.WriteToken(LessThanToken, string.Empty);
        for (var i = 0; i < Dimensions.Count; i++)
        {
            Dimensions[i].WriteTo(writer, string.Empty);
            writer.WriteToken(XTokens[i], string.Empty);
        }

        ElementType.WriteTo(writer, string.Empty);
        writer.WriteToken(GreaterThanToken, string.Empty);
    }
}

/// <summary>
/// Represents a builtin memref type.
/// </summary>
public sealed class MemRefTypeSyntax(
    SyntaxToken keywordToken,
    SyntaxToken lessThanToken,
    IReadOnlyList<ShapedTypeDimensionSyntax> dimensions,
    IReadOnlyList<SyntaxToken> xTokens,
    SyntaxToken? unrankedToken,
    TypeSyntax elementType,
    IReadOnlyList<SyntaxToken> trailingCommaTokens,
    IReadOnlyList<RawSyntaxText> trailingParameters,
    SyntaxToken greaterThanToken) : TypeSyntax
{
    public SyntaxToken KeywordToken { get; } = keywordToken;
    public SyntaxToken LessThanToken { get; } = lessThanToken;
    public IReadOnlyList<ShapedTypeDimensionSyntax> Dimensions { get; } = dimensions;
    public IReadOnlyList<SyntaxToken> XTokens { get; } = xTokens;
    public SyntaxToken? UnrankedToken { get; } = unrankedToken;
    public TypeSyntax ElementType { get; } = elementType;
    public IReadOnlyList<SyntaxToken> TrailingCommaTokens { get; } = trailingCommaTokens;
    public IReadOnlyList<RawSyntaxText> TrailingParameters { get; } = trailingParameters;
    public SyntaxToken GreaterThanToken { get; } = greaterThanToken;

    public bool IsUnranked => UnrankedToken.HasValue;

    /// <inheritdoc/>
    public override bool TryGetRawText(out RawSyntaxText? rawText)
    {
        var parts = new List<object?> { KeywordToken, LessThanToken };
        if (IsUnranked)
        {
            parts.Add(UnrankedToken);
            parts.Add(XTokens[0]);
        }
        else
        {
            for (var i = 0; i < Dimensions.Count; i++)
            {
                parts.Add(Dimensions[i]);
                parts.Add(XTokens[i]);
            }
        }

        parts.Add(ElementType);
        for (var i = 0; i < TrailingParameters.Count; i++)
        {
            parts.Add(TrailingCommaTokens[i]);
            parts.Add(TrailingParameters[i]);
        }

        parts.Add(GreaterThanToken);
        rawText = SyntaxTextComposer.Compose(parts.ToArray());
        return true;
    }

    /// <inheritdoc/>
    public override void WriteTo(Text.SyntaxWriter writer, string defaultLeadingTrivia)
    {
        writer.WriteToken(KeywordToken, defaultLeadingTrivia);
        writer.WriteToken(LessThanToken, string.Empty);
        if (IsUnranked)
        {
            writer.WriteToken(UnrankedToken!.Value, string.Empty);
            writer.WriteToken(XTokens[0], string.Empty);
        }
        else
        {
            for (var i = 0; i < Dimensions.Count; i++)
            {
                Dimensions[i].WriteTo(writer, string.Empty);
                writer.WriteToken(XTokens[i], string.Empty);
            }
        }

        ElementType.WriteTo(writer, string.Empty);
        for (var i = 0; i < TrailingParameters.Count; i++)
        {
            writer.WriteToken(TrailingCommaTokens[i], string.Empty);
            writer.WriteRaw(TrailingParameters[i], " ");
        }

        writer.WriteToken(GreaterThanToken, string.Empty);
    }
}
