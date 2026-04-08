namespace MLIR.Dialects.Attributes.Collections;

using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Parses dense boolean-array attribute literals such as <c>array&lt;i1: true, false&gt;</c>.
/// </summary>
public sealed class DenseBooleanArrayAttributeAssemblyFormat : DenseArrayAttributeAssemblyFormat<bool>
{
    /// <inheritdoc/>
    protected override AttributeValueSyntax ElementToSyntax(bool element)
    {
        var text = element ? "true" : "false";
        return new BooleanAttributeValueSyntax(TokenFactory.Identifier(text), element);
    }

    /// <inheritdoc/>
    protected override TypeSyntax GetElementTypeSyntax(string? constraintName)
    {
        return new RawTypeSyntax(new RawSyntaxText("i1"));
    }
}
