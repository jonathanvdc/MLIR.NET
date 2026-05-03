namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses, binds, and rewrites a dialect-specific type assembly format.
/// </summary>
public interface ITypeAssemblyFormat
{
    /// <summary>
    /// Attempts to parse a dialect-specific custom assembly form for a type.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed type syntax, a no-match result, or a diagnostic-producing failure.</returns>
    ParseResult<TypeSyntax> TryParse(TypeParsingContext context);

    /// <summary>
    /// Interprets the supplied type syntax into a semantic type reference.
    /// </summary>
    /// <param name="syntax">The type syntax to interpret.</param>
    /// <param name="definition">The type definition.</param>
    /// <param name="binder">The binding context.</param>
    /// <returns>The interpreted type reference.</returns>
    TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder);

    /// <summary>
    /// Builds a custom concrete syntax tree for the supplied type reference.
    /// </summary>
    /// <param name="type">The type reference to rewrite.</param>
    /// <param name="context">The CST transformation context.</param>
    /// <returns>The custom assembly type syntax.</returns>
    TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context);
}

/// <summary>
/// Base class for type assembly formats whose custom grammar handles only
/// the body after a self-identifying <c>!dialect.type</c> prefix.
/// </summary>
public abstract class BodyOnlyTypeAssemblyFormat(string typeName) : ITypeAssemblyFormat
{
    /// <summary>
    /// Gets the self-identifying type name accepted by this format.
    /// </summary>
    public string TypeName { get; } = typeName;

    /// <summary>
    /// Parses the full self-identifying type form, consuming and validating the
    /// prefix before delegating body parsing to <see cref="TryParseBody"/>.
    /// </summary>
    public virtual ParseResult<TypeSyntax> TryParse(TypeParsingContext context)
    {
        if (!context.TryMatch(TokenKind.Bang, out var bangToken))
        {
            return ParseResult<TypeSyntax>.NoMatch();
        }

        var nameTokenResult = context.Expect(TokenKind.Identifier, "Expected a type name after '!'.");
        if (!nameTokenResult.IsSuccess)
        {
            return ParseResult<TypeSyntax>.Failure(nameTokenResult.Diagnostic!);
        }

        var nameToken = nameTokenResult.Value;
        if (!string.Equals(nameToken.Text, TypeName, System.StringComparison.Ordinal))
        {
            return ParseResult<TypeSyntax>.Failure(
                context.CreateDiagnostic($"Expected '!{TypeName}' but found '!{nameToken.Text}'."));
        }

        var result = TryParseBody(context, new DialectTypePrefix(bangToken, nameToken));
        if (result.IsNoMatch)
        {
            return ParseResult<TypeSyntax>.Failure(
                context.CreateDiagnostic($"Expected body for '!{TypeName}'."));
        }

        return result;
    }

    /// <summary>
    /// Parses the custom body after <paramref name="prefix"/> has been consumed and validated.
    /// </summary>
    protected abstract ParseResult<TypeSyntax> TryParseBody(
        TypeParsingContext context,
        DialectTypePrefix prefix);

    /// <summary>
    /// Interprets the supplied type syntax into a semantic type reference.
    /// </summary>
    public abstract TypeReference Bind(TypeSyntax syntax, TypeDefinition definition, Binder binder);

    /// <summary>
    /// Builds a custom concrete syntax tree for the supplied type reference.
    /// </summary>
    public abstract TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context);
}
