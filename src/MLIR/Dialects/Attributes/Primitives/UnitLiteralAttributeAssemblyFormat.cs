namespace MLIR.Dialects.Attributes.Primitives;

using MLIR.Dialects;
using MLIR.Dialects.Builtin;
using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses unit attribute literals.
/// </summary>
public sealed class UnitLiteralAttributeAssemblyFormat : BodylessSelfIdentifyingAttributeAssemblyFormat
{
    private const string BuiltinUnitAttributeName = "builtin.unit";
    private readonly bool parseSelfIdentifyingSyntax;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitLiteralAttributeAssemblyFormat"/> class.
    /// </summary>
    public UnitLiteralAttributeAssemblyFormat(bool parseSelfIdentifyingSyntax = true)
        : base(BuiltinUnitAttributeName)
    {
        this.parseSelfIdentifyingSyntax = parseSelfIdentifyingSyntax;
    }

    /// <inheritdoc/>
    public override ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context)
    {
        if (context.TryMatch(TokenKind.Identifier, out var token) && token.Text == "unit")
        {
            return ParseResult<AttributeValueSyntax>.Success(new UnitAttributeValueSyntax(token));
        }

        return parseSelfIdentifyingSyntax
            ? base.TryParse(context)
            : ParseResult<AttributeValueSyntax>.NoMatch();
    }

    /// <inheritdoc/>
    protected override AttributeValueSyntax CreateSelfIdentifyingSyntax(DialectAttributePrefix prefix)
    {
        return new PrefixedUnitAttributeValueSyntax(prefix);
    }

    /// <inheritdoc/>
    public override AttributeValue Bind(AttributeValueSyntax syntax, Binder binder)
    {
        return new UnitAttr(syntax);
    }

    /// <inheritdoc/>
    public override AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        return attribute.Syntax ?? new UnitAttributeValueSyntax(TokenFactory.Identifier("unit"));
    }
}
