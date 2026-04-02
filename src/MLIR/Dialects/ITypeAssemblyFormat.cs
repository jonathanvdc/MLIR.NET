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
    /// <param name="syntax">When this method returns, contains the parsed type syntax when custom parsing succeeded.</param>
    /// <returns><see langword="true"/> when a custom assembly form was parsed; otherwise, <see langword="false"/>.</returns>
    bool TryParse(TypeParsingContext context, out TypeSyntax? syntax);

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
