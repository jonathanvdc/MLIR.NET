using System.Net.Http.Headers;
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
/// <param name="bitWidth">The bit width of the integer representation used to encode the enum values.</param>
/// <param name="names">The mapping of enum values to their corresponding string representations.</param>
public abstract class SimpleEnumAttributeAssemblyFormat<T>(int bitWidth, IReadOnlyDictionary<ApInt, string> names) : EnumAttributeAssemblyFormat<T>(bitWidth, names) where T : AttributeValue
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

        if (Names.TryGetValue(value, out var enumName))
        {
            return new EnumAttributeValueSyntax(new SeparatedSyntaxList<Token>(
                [TokenFactory.Identifier(enumName)],
                []));
        }

        return new IntegerAttributeValueSyntax(TokenFactory.Integer(value.ToString()), value);
    }

    /// <inheritdoc/>
    public override ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (context.TryMatch(TokenKind.Integer, out var intToken))
        {
            return ParseResult<AttributeValueSyntax>.Success(new IntegerAttributeValueSyntax(intToken, ApInt.Parse(BitWidth, intToken.Text)));
        }
        else
        {
            var name = context.Expect(TokenKind.Identifier, "Expected identifier in enum attribute value");
            if (name.IsError) return ParseResult<AttributeValueSyntax>.Failure(name.Diagnostic!);

            if (!reverseNames.ContainsKey(name.Value.Text))
            {
                var location = name.Value.Location;
                return ParseResult<AttributeValueSyntax>.Failure(new Diagnostic($"Unknown enum name '{name.Value.Text}' in enum attribute value.", location));
            }

            return ParseResult<AttributeValueSyntax>.Success(new EnumAttributeValueSyntax(new SeparatedSyntaxList<Token>(
                [name.Value],
                [])));
        }
    }
}
