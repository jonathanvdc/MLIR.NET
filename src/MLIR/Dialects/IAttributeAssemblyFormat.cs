namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses, binds, and rewrites a dialect-specific attribute assembly format.
/// </summary>
public interface IAttributeAssemblyFormat
{
    /// <summary>
    /// Attempts to parse a dialect-specific custom assembly form for an attribute value.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <param name="syntax">When this method returns, contains the parsed attribute-value syntax when custom parsing succeeded.</param>
    /// <returns><see langword="true"/> when a custom assembly form was parsed; otherwise, <see langword="false"/>.</returns>
    bool TryParse(AttributeParsingContext context, out AttributeValueSyntax? syntax);

    /// <summary>
    /// Interprets the supplied attribute-value syntax into a semantic attribute value.
    /// </summary>
    /// <param name="syntax">The attribute-value syntax to interpret.</param>
    /// <param name="definition">The attribute definition.</param>
    /// <param name="binder">The binding context.</param>
    /// <returns>The interpreted attribute value.</returns>
    AttributeValue Bind(AttributeValueSyntax syntax, AttributeDefinition definition, Binder binder);

    /// <summary>
    /// Builds a custom concrete syntax tree for the supplied attribute value.
    /// </summary>
    /// <param name="attribute">The attribute value to rewrite.</param>
    /// <param name="context">The CST transformation context.</param>
    /// <returns>The custom assembly attribute syntax.</returns>
    AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context);
}
