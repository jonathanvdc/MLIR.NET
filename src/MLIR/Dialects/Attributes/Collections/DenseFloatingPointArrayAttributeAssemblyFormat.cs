namespace MLIR.Dialects.Attributes.Collections;

using System.Globalization;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Parses dense floating-point-array attribute literals such as <c>array&lt;f32: 1.0, 2.0&gt;</c>.
/// </summary>
public sealed class DenseFloatingPointArrayAttributeAssemblyFormat : DenseArrayAttributeAssemblyFormat<double>
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
        return constraintName switch
        {
            "DenseF64ArrayAttr" => new RawTypeSyntax(new RawSyntaxText("f64")),
            _ => new RawTypeSyntax(new RawSyntaxText("f32")),
        };
    }
}
