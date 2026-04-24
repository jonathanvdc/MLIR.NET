namespace TableGen.Syntax;

using MLIR.Text;

/// <summary>
/// Represents the TableGen unset value literal '?'.
/// </summary>
public sealed class UnsetSyntax(SourceLocation location = default) : ExpressionSyntax(location)
{
}
