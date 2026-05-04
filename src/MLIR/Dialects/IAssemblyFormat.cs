using MLIR.Semantics;
using MLIR.Text;
using MLIR.Transforms;

namespace MLIR.Dialects;

/// <summary>
/// Parses, binds, and rewrites a dialect-specific assembly format.
/// </summary>
public interface IAssemblyFormat<TSyntax, TValue, in TParsingContext>
{
    /// <summary>
    /// Attempts to parse a dialect-specific custom assembly form.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed type syntax, a no-match result, or a diagnostic-producing failure.</returns>
    ParseResult<TSyntax> TryParse(TParsingContext context);

    /// <summary>
    /// Interprets the supplied type syntax into a semantic value.
    /// </summary>
    /// <param name="syntax">The type syntax to interpret.</param>
    /// <param name="binder">The binding context.</param>
    /// <returns>The interpreted value.</returns>
    TValue Bind(TSyntax syntax, Binder binder);

    /// <summary>
    /// Builds a custom concrete syntax tree for the supplied value.
    /// </summary>
    /// <param name="type">The value to rewrite.</param>
    /// <param name="context">The CST transformation context.</param>
    /// <returns>The custom assembly syntax.</returns>
    TSyntax BuildCustomAssemblySyntax(TValue type, ConcreteSyntaxBuilderContext context);
}
