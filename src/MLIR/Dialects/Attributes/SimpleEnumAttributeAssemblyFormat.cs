using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;

namespace MLIR.Dialects.Attributes;

/// <summary>
/// Provides an assembly format for attributes whose values are simple enums.
/// </summary>
/// <typeparam name="T">The specific enum attribute type for which this assembly format is defined.</typeparam>
/// <param name="names">The mapping of enum values to their corresponding string representations.</param>
public abstract class SimpleEnumAttributeAssemblyFormat<T>(IReadOnlyDictionary<ApInt, string> names) : EnumAttributeAssemblyFormat<T>(names) where T : AttributeValue
{
    /// <inheritdoc/>
    public override AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is EnumAttributeValueSyntax enumSyntax)
        {
            var tokens = enumSyntax.Elements;
            if (tokens.Count == 0) throw new InvalidOperationException("Enum attribute value cannot be empty.");
            else if (tokens.Count > 1) throw new InvalidOperationException("Simple enum attribute value cannot contain multiple elements.");

            var token = tokens[0];
            if (token.TokenKind == TokenKind.Integer)
            {
                return EnumFromInt(ApInt.Parse(BitWidth, token.Text), enumSyntax);
            }

            if (!reverseNames.TryGetValue(token.Text, out var flag))
            {
                throw new InvalidOperationException($"Unknown enum name '{token.Text}' in enum attribute value.");
            }

            return EnumFromInt(flag, enumSyntax);
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
        if (attribute is not T enumAttribute)
        {
            throw new InvalidOperationException($"Unexpected attribute value type '{attribute.GetType().Name}' for enum attribute.");
        }

        var value = EnumToInt(enumAttribute);
        var useAngleBrackets = AngleBracketRequirement != EnumAngleBracketRequirement.Prohibited;

        if (Names.TryGetValue(value, out var enumName))
        {
            if (useAngleBrackets)
            {
                return new DelimitedEnumAttributeValueSyntax(new DelimitedSyntaxList<Token>(
                    TokenFactory.LessThan(),
                    [TokenFactory.Identifier(enumName)],
                    [],
                    TokenFactory.GreaterThan()));
            }

            return new UndelimitedEnumAttributeValueSyntax(new SeparatedSyntaxList<Token>(
                [TokenFactory.Identifier(enumName)],
                []));
        }

        if (useAngleBrackets)
        {
            return new DelimitedEnumAttributeValueSyntax(new DelimitedSyntaxList<Token>(
                TokenFactory.LessThan(),
                [TokenFactory.Integer(value.ToString())],
                [],
                TokenFactory.GreaterThan()));
        }

        return new UndelimitedEnumAttributeValueSyntax(new SeparatedSyntaxList<Token>(
            [TokenFactory.Integer(value.ToString())],
            []));
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

                return ParseResult<AttributeValueSyntax>.Success(new DelimitedEnumAttributeValueSyntax(new DelimitedSyntaxList<Token>(
                    open.Value,
                    [intToken],
                    [],
                    close)));
            }

            return ParseResult<AttributeValueSyntax>.Success(new UndelimitedEnumAttributeValueSyntax(new SeparatedSyntaxList<Token>(
                [intToken],
                [])));
        }

        var name = context.Expect(TokenKind.Identifier, "Expected identifier in enum attribute value");
        if (name.IsError) return ParseResult<AttributeValueSyntax>.Failure(name.Diagnostic!);

        if (!reverseNames.ContainsKey(name.Value.Text))
        {
            var location = name.Value.Location;
            return ParseResult<AttributeValueSyntax>.Failure(new Diagnostic($"Unknown enum name '{name.Value.Text}' in enum attribute value.", location));
        }

        if (open.HasValue)
        {
            var closeResult = context.Expect(TokenKind.GreaterThan, "Expected '>' to end enum attribute value");
            if (closeResult.IsError) return ParseResult<AttributeValueSyntax>.Failure(closeResult.Diagnostic!);
            var close = closeResult.Value;

            return ParseResult<AttributeValueSyntax>.Success(new DelimitedEnumAttributeValueSyntax(new DelimitedSyntaxList<Token>(
                open.Value,
                [name.Value],
                [],
                close)));
        }

        return ParseResult<AttributeValueSyntax>.Success(new UndelimitedEnumAttributeValueSyntax(new SeparatedSyntaxList<Token>(
            [name.Value],
            [])));
    }
}
