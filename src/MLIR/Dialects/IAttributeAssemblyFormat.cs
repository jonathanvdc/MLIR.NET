namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses, binds, and rewrites a dialect-specific attribute assembly format.
/// </summary>
public interface IAttributeAssemblyFormat : IAssemblyFormat<AttributeValueSyntax, AttributeValue>;

/// <summary>
/// Base class for attribute assembly formats whose custom grammar handles only
/// the body after a self-identifying <c>#dialect.attr</c> prefix.
/// </summary>
public abstract class BodyOnlyAttributeAssemblyFormat(string attributeName) : IAttributeAssemblyFormat
{
    /// <summary>
    /// Gets the self-identifying attribute name accepted by this format.
    /// </summary>
    public string AttributeName { get; } = attributeName;

    /// <summary>
    /// Parses the full self-identifying attribute form, consuming and validating
    /// the prefix before delegating body parsing to <see cref="TryParseBody"/>.
    /// </summary>
    public virtual ParseResult<AttributeValueSyntax> TryParse(ParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Hash, out var hashToken))
        {
            return ParseResult<AttributeValueSyntax>.NoMatch();
        }

        var nameTokenResult = context.Expect(TokenKind.Identifier, "Expected an attribute name after '#'.");
        if (!nameTokenResult.IsSuccess)
        {
            return ParseResult<AttributeValueSyntax>.Failure(nameTokenResult.Diagnostic!);
        }

        var nameToken = nameTokenResult.Value;
        if (!string.Equals(nameToken.Text, AttributeName, System.StringComparison.Ordinal))
        {
            return ParseResult<AttributeValueSyntax>.Failure(
                context.CreateDiagnostic($"Expected '#{AttributeName}' but found '#{nameToken.Text}'."));
        }

        var result = TryParseBody(context, new DialectAttributePrefix(hashToken, nameToken));
        if (result.IsNoMatch)
        {
            return ParseResult<AttributeValueSyntax>.Failure(
                context.CreateDiagnostic($"Expected body for '#{AttributeName}'."));
        }

        return result;
    }

    /// <summary>
    /// Parses the custom body after <paramref name="prefix"/> has been consumed and validated.
    /// </summary>
    protected abstract ParseResult<AttributeValueSyntax> TryParseBody(
        ParsingContext context,
        DialectAttributePrefix prefix);

    /// <summary>
    /// Interprets the supplied attribute-value syntax into a semantic attribute value.
    /// </summary>
    public abstract AttributeValue Bind(AttributeValueSyntax syntax, Binder binder);

    /// <summary>
    /// Builds a custom concrete syntax tree for the supplied attribute value.
    /// </summary>
    public abstract AttributeValueSyntax BuildCustomAssemblySyntax(AttributeValue attribute, ConcreteSyntaxBuilderContext context);
}

/// <summary>
/// Base class for self-identifying attribute formats that have no body after
/// the <c>#dialect.attr</c> prefix.
/// </summary>
public abstract class BodylessSelfIdentifyingAttributeAssemblyFormat(string attributeName)
    : BodyOnlyAttributeAssemblyFormat(attributeName)
{
    /// <inheritdoc/>
    protected sealed override ParseResult<AttributeValueSyntax> TryParseBody(
        ParsingContext context,
        DialectAttributePrefix prefix)
    {
        return ParseResult<AttributeValueSyntax>.Success(CreateSelfIdentifyingSyntax(prefix));
    }

    /// <summary>
    /// Builds syntax for the already-consumed self-identifying attribute prefix.
    /// </summary>
    protected abstract AttributeValueSyntax CreateSelfIdentifyingSyntax(DialectAttributePrefix prefix);
}
