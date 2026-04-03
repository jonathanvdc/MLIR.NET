namespace MLIR.Dialects.Attributes.Collections;

using System.Globalization;
using System.Numerics;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Parses dense integer-array attribute literals such as <c>array&lt;i32: 1, 2&gt;</c>.
/// </summary>
public sealed class DenseIntegerArrayAttributeAssemblyFormat : DenseArrayAttributeAssemblyFormat<BigInteger>
{
    /// <inheritdoc/>
    protected override AttributeValueSyntax ElementToSyntax(BigInteger element)
    {
        var text = element.ToString(CultureInfo.InvariantCulture);
        return new IntegerAttributeValueSyntax(new SyntaxToken(text), element);
    }

    /// <inheritdoc/>
    protected override TypeSyntax GetElementTypeSyntax(string? constraintName)
    {
        return constraintName switch
        {
            "DenseI8ArrayAttr" => new RawTypeSyntax(new RawSyntaxText("i8")),
            "DenseI16ArrayAttr" => new RawTypeSyntax(new RawSyntaxText("i16")),
            "DenseI64ArrayAttr" => new RawTypeSyntax(new RawSyntaxText("i64")),
            _ => new RawTypeSyntax(new RawSyntaxText("i32")),
        };
    }
}
