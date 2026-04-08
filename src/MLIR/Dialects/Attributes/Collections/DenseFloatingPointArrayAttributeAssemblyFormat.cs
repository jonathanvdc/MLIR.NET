namespace MLIR.Dialects.Attributes.Collections;

using MLIR.Numerics;
using MLIR.Semantics.Attributes.Primitives;
using MLIR.Syntax;
using MLIR.Syntax.Attributes.Primitives;

/// <summary>
/// Parses dense floating-point array attribute literals such as <c>array&lt;f32: 1.0, 2.0&gt;</c>.
/// </summary>
public sealed class DenseFloatingPointArrayAttributeAssemblyFormat : DenseArrayAttributeAssemblyFormat<ApFloat>
{
    private readonly string elementTypeText;

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseFloatingPointArrayAttributeAssemblyFormat"/> class.
    /// </summary>
    /// <param name="elementTypeText">The literal type text to emit when rebuilding syntax.</param>
    public DenseFloatingPointArrayAttributeAssemblyFormat(string elementTypeText)
    {
        this.elementTypeText = elementTypeText;
    }

    /// <inheritdoc/>
    protected override AttributeValueSyntax ElementToSyntax(ApFloat element)
    {
        var text = FloatingPointLiteralParser.Format(element);
        return new FloatingPointAttributeValueSyntax(new RawSyntaxText(text), element);
    }

    /// <inheritdoc/>
    protected override TypeSyntax GetElementTypeSyntax(string? constraintName)
    {
        return new RawTypeSyntax(new RawSyntaxText(elementTypeText));
    }
}
