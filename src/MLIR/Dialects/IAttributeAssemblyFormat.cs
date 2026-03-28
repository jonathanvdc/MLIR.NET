namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Interprets dialect-specific attribute assembly into semantic metadata.
/// </summary>
public interface IAttributeAssemblyFormat
{
    /// <summary>
    /// Interprets the supplied attribute value.
    /// </summary>
    /// <param name="attribute">The semantic attribute value to interpret.</param>
    /// <param name="context">The binding context.</param>
    void Bind(AttributeValue attribute, AttributeAssemblyBindingContext context);
}
