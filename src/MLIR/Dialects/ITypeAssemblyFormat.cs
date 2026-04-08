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
/// Marker interface for type assembly format implementations that handle only the
/// body portion of the type syntax, after the <c>!dialect.type</c> prefix has been
/// consumed by the parser.
/// </summary>
/// <remarks>
/// <para>
/// When the parser encounters <c>!dialect.type body</c> and the registered format implements
/// this interface, the parser consumes the <c>!</c> and name identifier tokens before
/// delegating to <see cref="ITypeAssemblyFormat.TryParse"/>.  The returned syntax is
/// a subclass of <see cref="Syntax.DialectPrefixedTypeSyntax"/> that stores the prefix
/// so that the printer can reproduce the full <c>!name body</c> form.
/// </para>
/// <para>
/// Hand-written formats that consume <c>!name</c> themselves should implement only
/// <see cref="ITypeAssemblyFormat"/> and leave this marker absent.
/// </para>
/// </remarks>
public interface IBodyOnlyTypeAssemblyFormat : ITypeAssemblyFormat
{
}
