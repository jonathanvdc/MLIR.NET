using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;

namespace MLIR.Dialects.Attributes;

/// <summary>
/// Provides an assembly format for attributes whose values are enums represented as bitfields.
/// </summary>
/// <typeparam name="T">The specific enum attribute type for which this assembly format is defined.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="FlagsEnumAttributeAssemblyFormat{T}"/> class with the specified bit width and mapping of enum values to their string representations.
/// </remarks>
/// <param name="names">The mapping of enum values to their corresponding string representations.</param>
public abstract class FlagsEnumAttributeAssemblyFormat<T>(IReadOnlyDictionary<ApInt, string> names) : EnumAttributeAssemblyFormat<T>(names) where T : AttributeValue
{
    /// <summary>
    /// Gets the token kind used to separate multiple enum elements in the assembly syntax. For example, if this is set to <see cref="TokenKind.Comma"/>, then multiple enum elements will be separated by commas in the assembly form of the attribute value, such as <c>EnumValue1, EnumValue2</c>.
    /// The separator token kind is used when parsing and printing enum attribute values that contain multiple flags set, allowing them to be represented in a human-readable form using their string names defined in the names mapping.
    /// </summary>
    public abstract TokenKind SeparatorTokenKind { get; }

    private readonly (string Name, ApInt Value)[] orderedNames =
        names.Select(pair => (Name: pair.Value, Value: pair.Key))
             .OrderByDescending(pair => pair.Value.PopCount()) // Sort by number of bits set, so that we match larger flags first when printing.
             .ThenByDescending(pair => pair.Value, ApInt.UnsignedComparer) // For flags with the same number of bits set, sort by value to ensure deterministic ordering.
             .ToArray();

    private AttributeValueSyntax CreateEnumSyntax(IReadOnlyList<Token> elements, bool useAngleBrackets)
    {
        if (useAngleBrackets)
        {
            return new DelimitedEnumAttributeValueSyntax(
                new DelimitedSyntaxList<Token>(
                    TokenFactory.LessThan(),
                    elements,
                    Enumerable.Repeat(CreateSeparatorToken(), elements.Count - 1).ToList(),
                    TokenFactory.GreaterThan()));
        }
        else
        {
            return new BareEnumAttributeValueSyntax(
                new SeparatedSyntaxList<Token>(
                    elements,
                    Enumerable.Repeat(CreateSeparatorToken(), elements.Count - 1).ToList()));
        }
    }

    private Token CreateSeparatorToken()
    {
        return SeparatorTokenKind switch
        {
            TokenKind.Comma => TokenFactory.Comma(),
            TokenKind.Pipe => TokenFactory.Pipe(),
            _ => throw new InvalidOperationException($"Unsupported separator token kind '{SeparatorTokenKind}' in flags enum attribute assembly format."),
        };
    }

    /// <inheritdoc/>
    public override AttributeValue Bind(AttributeValueSyntax syntax, Binder binder)
    {
        if (syntax is EnumAttributeValueSyntax enumSyntax)
        {
            var tokens = enumSyntax.Elements;
            if (tokens.Count == 0) throw new InvalidOperationException("Enum attribute value cannot be empty.");

            if (tokens.Count == 1 && tokens[0].TokenKind == TokenKind.Integer)
            {
                return EnumFromInt(ApInt.Parse(BitWidth, tokens[0].Text), enumSyntax);
            }

            var accumulator = zero;
            foreach (var token in tokens)
            {
                if (!reverseNames.TryGetValue(token.Text, out var flag))
                {
                    throw new InvalidOperationException($"Unknown enum name '{token.Text}' in enum attribute value.");
                }

                accumulator |= flag;
            }

            return EnumFromInt(accumulator, enumSyntax);
        }
        else if (syntax is IntegerAttributeValueSyntax intSyntax)
        {
            return EnumFromInt(intSyntax.Value, intSyntax);
        }
        else
        {
            throw new InvalidOperationException($"Unexpected syntax kind '{syntax.GetType().Name}' for enum attribute value.");
        }
    }

    /// <inheritdoc/>
    public override AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is T enumAttribute)
        {
            var flags = EnumToInt(enumAttribute);
            var useAngleBrackets = AngleBracketRequirement != EnumAngleBracketRequirement.Prohibited;

            var parts = new List<Token>();
            foreach (var (name, value) in orderedNames)
            {
                if (flags == value)
                {
                    parts.Add(TokenFactory.Identifier(name));
                    flags = zero;
                    break;
                }

                if (!value.IsZero && (flags & value) == value)
                {
                    parts.Add(TokenFactory.Identifier(name));
                    flags &= ~value;
                }
            }

            if (!flags.IsZero)
            {
                return CreateEnumSyntax([TokenFactory.Integer(flags.ToString())], useAngleBrackets);
            }

            if (parts.Count == 0)
            {
                // If there are no flags set, we still print something. Check if there's a name for the zero value.
                if (Names.TryGetValue(zero, out var zeroEnum))
                {
                    parts.Add(TokenFactory.Identifier(zeroEnum));
                }
                else
                {
                    return CreateEnumSyntax([TokenFactory.Integer("0")], useAngleBrackets);
                }
            }

            return CreateEnumSyntax(parts, useAngleBrackets);
        }
        else
        {
            throw new InvalidOperationException($"Unexpected attribute value type '{attribute.GetType().Name}' for enum attribute.");
        }
    }

    /// <inheritdoc/>
    public override ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        Token? open = null;
        var allowsAngleBrackets = AngleBracketRequirement != EnumAngleBracketRequirement.Prohibited;
        var requiresAngleBrackets = AngleBracketRequirement == EnumAngleBracketRequirement.Required;

        if (context.TryMatch(TokenKind.LessThan, out var openToken))
        {
            if (!allowsAngleBrackets)
            {
                return ParseResult<AttributeValueSyntax>.Failure(new Diagnostic("Unexpected '<' in enum attribute value.", openToken.Location));
            }

            open = openToken;
        }
        else if (requiresAngleBrackets)
        {
            var error = context.Expect(TokenKind.LessThan, "Expected '<' to start enum attribute value");
            return ParseResult<AttributeValueSyntax>.Failure(error.Diagnostic!);
        }

        if (context.TryMatch(TokenKind.Integer, out var intToken))
        {
            if (open.HasValue)
            {
                var closeAfterInteger = context.Expect(TokenKind.GreaterThan, "Expected '>' to end enum attribute value");
                if (closeAfterInteger.IsError) return ParseResult<AttributeValueSyntax>.Failure(closeAfterInteger.Diagnostic!);
                var close = closeAfterInteger.Value;

                return ParseResult<AttributeValueSyntax>.Success(
                    new DelimitedEnumAttributeValueSyntax(
                        new DelimitedSyntaxList<Token>(
                            open.Value,
                            [intToken],
                            Array.Empty<Token>(),
                            close)));
            }

            return ParseResult<AttributeValueSyntax>.Success(
                new BareEnumAttributeValueSyntax(
                    new SeparatedSyntaxList<Token>(
                        [intToken],
                        Array.Empty<Token>())));
        }

        var identifiers = new List<Token>();
        var separators = new List<Token>();

        while (true)
        {
            var name = context.Expect(TokenKind.Identifier, "Expected identifier in enum attribute value");
            if (name.IsError) return ParseResult<AttributeValueSyntax>.Failure(name.Diagnostic!);

            if (!reverseNames.ContainsKey(name.Value.Text))
            {
                var location = name.Value.Location;
                return ParseResult<AttributeValueSyntax>.Failure(new Diagnostic($"Unknown enum name '{name.Value.Text}' in enum attribute value.", location));
            }

            identifiers.Add(name.Value);
            if (context.TryMatch(SeparatorTokenKind, out var comma))
            {
                separators.Add(comma);
            }
            else
            {
                break;
            }
        }

        if (open.HasValue)
        {
            var closeResult = context.Expect(TokenKind.GreaterThan, "Expected '>' to end enum attribute value");
            if (closeResult.IsError) return ParseResult<AttributeValueSyntax>.Failure(closeResult.Diagnostic!);
            var close = closeResult.Value;

            return ParseResult<AttributeValueSyntax>.Success(
                new DelimitedEnumAttributeValueSyntax(
                    new DelimitedSyntaxList<Token>(
                        open.Value,
                        identifiers,
                        separators,
                        close)));
        }

        return ParseResult<AttributeValueSyntax>.Success(
            new BareEnumAttributeValueSyntax(
                new SeparatedSyntaxList<Token>(
                    identifiers,
                    separators)));
    }
}
