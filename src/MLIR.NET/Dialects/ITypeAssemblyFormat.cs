namespace MLIR.Dialects;

using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Interprets dialect-specific type assembly into semantic metadata.
/// </summary>
public interface ITypeAssemblyFormat
{
    /// <summary>
    /// Interprets the supplied type reference.
    /// </summary>
    /// <param name="type">The semantic type reference to interpret.</param>
    /// <param name="context">The binding context.</param>
    void Bind(TypeReference type, TypeAssemblyBindingContext context);
}
