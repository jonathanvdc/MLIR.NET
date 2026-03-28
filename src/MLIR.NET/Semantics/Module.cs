namespace MLIR.Semantics;

using System.Collections.Generic;
using MLIR.Syntax;
using MLIR.Text;

/// <summary>
/// Represents a semantic MLIR module bound from generic syntax.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Module"/> class.
/// </remarks>
/// <param name="syntax">The concrete syntax tree that produced the semantic module.</param>
/// <param name="operations">The top-level operations contained in the module.</param>
/// <param name="assemblyDiagnostics">The diagnostics reported while interpreting custom assembly.</param>
public sealed class Module(ModuleSyntax syntax, IReadOnlyList<Operation> operations, IReadOnlyList<AssemblyDiagnostic> assemblyDiagnostics)
{
    /// <summary>
    /// Gets the concrete syntax tree that produced the semantic module.
    /// </summary>
    public ModuleSyntax Syntax { get; } = syntax;

    /// <summary>
    /// Gets the top-level operations contained in the module.
    /// </summary>
    public IReadOnlyList<Operation> Operations { get; } = operations;

    /// <summary>
    /// Gets the diagnostics reported while interpreting custom assembly.
    /// </summary>
    public IReadOnlyList<AssemblyDiagnostic> AssemblyDiagnostics { get; } = assemblyDiagnostics;

    /// <summary>
    /// Converts the semantic module to MLIR text, using custom assembly formats when available.
    /// </summary>
    /// <returns>The printed MLIR text.</returns>
    public string ToText()
    {
        return SemanticPrinter.Print(this);
    }
}
