namespace MLIR.Syntax;

using System.Collections.Generic;

/// <summary>
/// Represents the body of an MLIR operation.
/// </summary>
public abstract class OperationBodySyntax
{
    /// <summary>
    /// Gets the operand tokens referenced by the operation body.
    /// </summary>
    public abstract IReadOnlyList<SyntaxToken> OperandTokens { get; }

    /// <summary>
    /// Gets the successor block label tokens referenced by the operation body.
    /// </summary>
    public abstract IReadOnlyList<SyntaxToken> SuccessorTokens { get; }

    /// <summary>
    /// Gets the regions nested under the operation.
    /// </summary>
    public abstract IReadOnlyList<RegionSyntax> Regions { get; }

    /// <summary>
    /// Gets the attribute dictionary projected by the operation body.
    /// </summary>
    public abstract DelimitedSyntaxList<NamedAttributeSyntax> Attributes { get; }

    /// <summary>
    /// Gets the colon token that introduces the type signature, if present.
    /// </summary>
    public abstract SyntaxToken? TypeSignatureColonToken { get; }

    /// <summary>
    /// Gets the raw trailing type signature, if present.
    /// </summary>
    public abstract RawSyntaxText? TypeSignature { get; }
}
