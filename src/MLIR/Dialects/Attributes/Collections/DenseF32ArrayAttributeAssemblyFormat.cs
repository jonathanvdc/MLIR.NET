namespace MLIR.Dialects.Attributes.Collections;

using System.Globalization;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Parses dense single-precision floating-point array attribute literals such as <c>array&lt;f32: 1.0, 2.0&gt;</c>.
/// </summary>
public sealed class DenseF32ArrayAttributeAssemblyFormat : DenseArrayAttributeAssemblyFormat<float>
{
    /// <inheritdoc/>
    protected override AttributeValueSyntax ElementToSyntax(float element)
    {
        var text = element.ToString("G", CultureInfo.InvariantCulture);
        return new FloatingPointAttributeValueSyntax(new RawSyntaxText(text), text);
    }

    /// <inheritdoc/>
    protected override TypeSyntax GetElementTypeSyntax(string? constraintName)
    {
        return new RawTypeSyntax(new RawSyntaxText("f32"));
    }
}
