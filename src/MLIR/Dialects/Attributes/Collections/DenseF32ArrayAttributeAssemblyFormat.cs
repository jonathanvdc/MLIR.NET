namespace MLIR.Dialects.Attributes.Collections;

using MLIR.Numerics;
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
        var text = MLIR.Semantics.Attributes.Primitives.FloatingPointLiteralParser.FormatSingle(element);
        return new FloatingPointAttributeValueSyntax(new RawSyntaxText(text), ApFloat.FromSingle(FloatSemantics.IEEESingle, element));
    }

    /// <inheritdoc/>
    protected override TypeSyntax GetElementTypeSyntax(string? constraintName)
    {
        return new RawTypeSyntax(new RawSyntaxText("f32"));
    }
}
