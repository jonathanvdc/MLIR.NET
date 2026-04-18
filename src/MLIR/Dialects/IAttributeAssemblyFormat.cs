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
    /// <returns>The parsed attribute-value syntax, a no-match result, or a diagnostic-producing failure.</returns>
    ParseResult<AttributeValueSyntax> TryParse(AttributeParsingContext context);

    /// <summary>
    /// Interprets the supplied attribute-value syntax into a semantic attribute value.
    /// </summary>
    /// <param name="syntax">The attribute-value syntax to interpret.</param>
    /// <param name="definition">The attribute constraint definition.</param>
    /// <param name="binder">The binding context.</param>
    /// <returns>The interpreted attribute value.</returns>
    AttributeValue Bind(AttributeValueSyntax syntax, AttributeConstraintDefinition definition, Binder binder);

    /// <summary>
    /// Builds a custom concrete syntax tree for the supplied attribute value.
    /// </summary>
    /// <param name="attribute">The attribute value to rewrite.</param>
    /// <param name="context">The CST transformation context.</param>
    /// <returns>The custom assembly attribute syntax.</returns>
    AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context);
}

/// <summary>
/// Marker interface for attribute assembly format implementations that handle only the
/// body portion of the attribute syntax, after the <c>#dialect.attr</c> prefix has been
/// consumed by the parser.
/// </summary>
/// <remarks>
/// <para>
/// When the parser encounters <c>#dialect.attr body</c> and the registered format implements
/// this interface, the parser consumes the <c>#</c> and name identifier tokens before
/// delegating to <see cref="IAttributeAssemblyFormat.TryParse"/>.  The returned syntax is
/// wrapped in a <see cref="Syntax.DialectPrefixedAttributeValueSyntax"/> so that the printer
/// can reproduce the full <c>#name body</c> form.
/// </para>
/// <para>
/// Hand-written formats that consume <c>#name</c> themselves should implement only
/// <see cref="IAttributeAssemblyFormat"/> and leave this marker absent.
/// </para>
/// </remarks>
public interface IBodyOnlyAttributeAssemblyFormat : IAttributeAssemblyFormat
{
}

/// <summary>
/// Attribute assembly format capability for self-identifying syntax with no body
/// after the <c>#dialect.attr</c> prefix.
/// </summary>
/// <remarks>
/// The parser owns prefix recognition and validation.  Formats implementing this
/// interface provide the small amount of format-specific knowledge needed to decide
/// whether a consumed prefix denotes the same logical attribute and to build the
/// corresponding prefix-preserving syntax node.
/// </remarks>
public interface IBodylessSelfIdentifyingAttributeAssemblyFormat : IAttributeAssemblyFormat
{
    /// <summary>
    /// Gets the self-identifying attribute name accepted by this bodyless form.
    /// </summary>
    string SelfIdentifyingAttributeName { get; }

    /// <summary>
    /// Returns <see langword="true"/> when this format can parse the supplied
    /// self-identifying attribute name as its bodyless form.
    /// </summary>
    bool CanParseSelfIdentifyingAttribute(string name);

    /// <summary>
    /// Builds syntax for the already-consumed self-identifying attribute prefix.
    /// </summary>
    AttributeValueSyntax CreateSelfIdentifyingSyntax(DialectAttributePrefix prefix);
}
