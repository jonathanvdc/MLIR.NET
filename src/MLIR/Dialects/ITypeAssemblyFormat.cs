namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses, binds, and rewrites a dialect-specific type assembly format.
/// </summary>
public interface ITypeAssemblyFormat : IAssemblyFormat<TypeSyntax, TypeReference>;

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
    public ParseResult<TypeSyntax> TryParse(ParsingContext context)
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
        ParsingContext context,
        DialectTypePrefix prefix);

    /// <summary>
    /// Interprets the supplied type syntax into a semantic type reference.
    /// </summary>
    public abstract TypeReference Bind(TypeSyntax syntax, Binder binder);

    /// <summary>
    /// Builds a custom concrete syntax tree for the supplied type reference.
    /// </summary>
    public abstract TypeSyntax BuildCustomAssemblySyntax(TypeReference type, ConcreteSyntaxBuilderContext context);
}
