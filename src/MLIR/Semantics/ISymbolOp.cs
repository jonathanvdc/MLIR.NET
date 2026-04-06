namespace MLIR.Semantics;

/// <summary>
/// Marks an operation that declares a symbol name via the MLIR <c>sym_name</c> attribute,
/// corresponding to the ODS <c>Symbol</c> trait.
/// </summary>
/// <remarks>
/// Implementing this interface allows programmatic traversal of symbol tables to identify
/// symbol-named operations without string-based attribute inspection.
/// </remarks>
public interface ISymbolOp
{
    /// <summary>
    /// Gets or sets the symbol name of this operation.
    /// </summary>
    /// <remarks>
    /// Backed by the <c>sym_name</c> attribute. Returns <see langword="null"/> if the attribute
    /// is absent or cannot be interpreted as a string.
    /// </remarks>
    string? SymbolName { get; set; }
}
