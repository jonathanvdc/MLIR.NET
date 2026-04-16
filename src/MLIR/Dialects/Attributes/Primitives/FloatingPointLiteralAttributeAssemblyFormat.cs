namespace MLIR.Dialects.Attributes.Primitives;

using MLIR;
using MLIR.Dialects;
using MLIR.Dialects.Builtin;
using MLIR.Numerics;
using MLIR.Semantics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Syntax.Attributes.Primitives;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses primitive floating-point attribute literals.
/// </summary>
public sealed class FloatingPointLiteralAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    private readonly FloatSemantics semantics;

    /// <summary>
    /// Initializes a new instance of the <see cref="FloatingPointLiteralAttributeAssemblyFormat"/> class.
    /// </summary>
    public FloatingPointLiteralAttributeAssemblyFormat()
        : this(FloatSemantics.IEEEDouble)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FloatingPointLiteralAttributeAssemblyFormat"/> class.
    /// </summary>
    /// <param name="semantics">
    /// The floating-point semantics to use when parsing and reconstructing integer literals as floats.
    /// </param>
    public FloatingPointLiteralAttributeAssemblyFormat(FloatSemantics semantics)
    {
        this.semantics = semantics;
    }

    /// <inheritdoc/>
    public ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        return FloatingPointAssemblyFormatHelper.TryParseDecimalLiteral(context, semantics);
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        if (syntax is TypedAttributeValueSyntax typedSyntax)
        {
            syntax = typedSyntax.AttributeSyntax;
        }

        if (syntax is FloatingPointAttributeValueSyntax floatingPointSyntax)
        {
            return new FloatAttr(GetResultType(), floatingPointSyntax.Value, floatingPointSyntax);
        }
        else if (syntax is IntegerAttributeValueSyntax integerSyntax)
        {
            // Allow integer literals to be used as floating-point attributes, by treating them as their decimal representation.
            var convertedSyntax = FloatingPointAssemblyFormatHelper.BuildSyntax(
                new RawSyntaxText(integerSyntax.SignToken.HasValue
                    ? [integerSyntax.SignToken.Value, integerSyntax.IntegerToken]
                    : [integerSyntax.IntegerToken]),
                FloatingPointLiteralParser.Parse(
                    semantics,
                    integerSyntax.SignToken.HasValue
                        ? integerSyntax.SignToken.Value.Text + integerSyntax.IntegerToken.Text
                        : integerSyntax.IntegerToken.Text));
            return new FloatAttr(GetResultType(), convertedSyntax.Value, convertedSyntax);
        }
        else
        {
            throw new InvalidOperationException("Expected a floating-point or integer literal syntax for a primitive floating-point attribute.");
        }
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        if (attribute is FloatAttr floatAttr)
        {
            var value = floatAttr.Value;
            return FloatingPointAssemblyFormatHelper.BuildSyntax(new RawSyntaxText(FloatingPointLiteralParser.Format(value)), value);
        }

        // Fallback: normalize via the attribute's existing syntax when the attribute is not a
        // FloatAttr (e.g., a user-defined test attribute value backed by the parsed form).
        if (attribute.Syntax is FloatingPointAttributeValueSyntax fpSyntax)
        {
            var value = fpSyntax.Value;
            return FloatingPointAssemblyFormatHelper.BuildSyntax(new RawSyntaxText(FloatingPointLiteralParser.Format(value)), value);
        }

        return attribute.Syntax ?? throw new System.InvalidOperationException("Primitive floating-point attributes require syntax to rebuild their assembly form.");
    }

    private TypeReference GetResultType()
    {
        return semantics switch
        {
            var s when s.Equals(FloatSemantics.IEEEHalf) => TypeFactory.F16,
            var s when s.Equals(FloatSemantics.BFloat16) => TypeFactory.BF16,
            var s when s.Equals(FloatSemantics.IEEESingle) => TypeFactory.F32,
            var s when s.Equals(FloatSemantics.TF32) => TypeFactory.TF32,
            _ => TypeFactory.F64,
        };
    }
}
