namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Syntax;
using MLIR.Text;
using MLIR.Transforms;

/// <summary>
/// Parses, binds, and rewrites a dialect-specific operation assembly format.
/// </summary>
/// <remarks>
/// Generic operation syntax and custom operation bodies both preserve source text.
/// Implementations can opt into custom concrete syntax while still projecting the same
/// semantic operation shape for binding and verification.
/// </remarks>
public interface IOperationAssemblyFormat : IAssemblyFormat<OperationSyntax, Operation, OperationParsingContext>;

/// <summary>
/// Base class for operation assembly formats whose custom grammar handles only
/// the operation body after the operation header has been parsed.
/// </summary>
public abstract class BodyOnlyOperationAssemblyFormat : IOperationAssemblyFormat
{
    /// <summary>
    /// Parses the full operation custom assembly form by parsing the operation header
    /// and delegating body parsing to <see cref="TryParseBody"/>.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed operation syntax, a no-match result, or a diagnostic-producing failure.</returns>
    public ParseResult<OperationSyntax> TryParse(OperationParsingContext context)
    {
        var headerResult = context.TryParseHeader();
        if (!headerResult.IsSuccess)
        {
            return ParseResult<OperationSyntax>.Failure(headerResult.Diagnostic!);
        }

        return TryParseAfterHeader(headerResult.Value, context);
    }

    /// <summary>
    /// Parses the body of the operation after the header has been parsed, returning the full operation syntax.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <param name="header">The parsed operation header.</param>
    /// <returns>The parsed operation syntax, a no-match result, or a diagnostic-producing failure.</returns>
    public ParseResult<OperationSyntax> TryParseAfterHeader(OperationHeader header, in OperationParsingContext context)
    {
        var result = TryParseBody(header, context);
        if (!result.IsSuccess)
        {
            return result.IsNoMatch
                ? ParseResult<OperationSyntax>.NoMatch()
                : ParseResult<OperationSyntax>.Failure(result.Diagnostic!);
        }
        else
        {
            var body = result.Value;
            return ParseResult<OperationSyntax>.Success(new OperationSyntax(
                header.ResultList,
                header.EqualsToken,
                header.NameToken,
                body));
        }
    }

    /// <summary>
    /// Parses the custom operation body for the supplied already-parsed operation header.
    /// </summary>
    /// <param name="header">The already-parsed operation header.</param>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed operation body syntax, a no-match result, or a diagnostic-producing failure.</returns>
    protected abstract ParseResult<OperationBodySyntax> TryParseBody(
        in OperationHeader header,
        OperationParsingContext context);

    /// <summary>
    /// Interprets the supplied concrete syntax tree in the assembly format into semantic properties.
    /// </summary>
    public abstract Operation Bind(OperationSyntax syntax, Binder binder);

    /// <summary>
    /// Builds a custom concrete syntax tree for the supplied operation.
    /// </summary>
    public abstract OperationSyntax BuildCustomAssemblySyntax(Operation operation, ConcreteSyntaxBuilderContext context);
}
