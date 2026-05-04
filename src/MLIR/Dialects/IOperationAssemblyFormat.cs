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
    /// Parses the full operation custom assembly form by using the already-parsed header
    /// from <paramref name="context"/> and delegating body parsing to <see cref="TryParseBody"/>.
    /// </summary>
    public ParseResult<OperationSyntax> TryParse(OperationParsingContext context)
    {
        var header = context.Header;
        var bodyResult = TryParseBody(header, context);
        return bodyResult.Map(body => new OperationSyntax(
            header.ResultList,
            header.EqualsToken,
            header.NameToken,
            body));
    }

    /// <summary>
    /// Parses the custom operation body for the supplied already-parsed operation header.
    /// </summary>
    protected abstract ParseResult<OperationBodySyntax> TryParseBody(
        OperationParseHeader header,
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
