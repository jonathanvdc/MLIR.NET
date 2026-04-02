namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses primitive integer attribute literals used by context-directed attribute constraints such as <c>I32Attr</c>.
/// </summary>
public sealed class IntegerLiteralAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <inheritdoc/>
    public bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax)
    {
        syntax = null;
        if (!context.TryMatch(TokenKind.Integer, out var literalToken))
        {
            return false;
        }

        syntax = new RawAttributeValueSyntax(new RawSyntaxText([literalToken]));
        return true;
    }

    /// <inheritdoc/>
    public AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder)
    {
        return definition.Factory(new AttributeValueConstructionContext(syntax, definition.Name, definition, syntax.Location));
    }

    /// <inheritdoc/>
    public AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context)
    {
        return attribute.Syntax ?? throw new System.InvalidOperationException("Primitive integer attributes require syntax to rebuild their assembly form.");
    }
}
