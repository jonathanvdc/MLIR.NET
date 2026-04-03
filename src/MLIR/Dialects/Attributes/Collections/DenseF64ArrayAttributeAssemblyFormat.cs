namespace MLIR.Dialects.Attributes.Collections;

using System.Globalization;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Parses dense double-precision floating-point array attribute literals such as <c>array&lt;f64: 1.0, 2.0&gt;</c>.
/// </summary>
public sealed class DenseF64ArrayAttributeAssemblyFormat : DenseArrayAttributeAssemblyFormat<double>
{
    /// <inheritdoc/>
    protected override AttributeValueSyntax ElementToSyntax(double element)
    {
        var text = element.ToString("G", CultureInfo.InvariantCulture);
        return new FloatingPointAttributeValueSyntax(new RawSyntaxText(text), text);
    }

    /// <inheritdoc/>
    protected override TypeSyntax GetElementTypeSyntax(string? constraintName)
    {
        return new RawTypeSyntax(new RawSyntaxText("f64"));
    }
}
