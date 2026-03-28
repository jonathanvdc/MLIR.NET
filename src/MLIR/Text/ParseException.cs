namespace MLIR.Text;

using System;

/// <summary>
/// Represents a fatal parse error produced while reading MLIR text.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ParseException"/> class.
/// </remarks>
/// <param name="diagnostic">The diagnostic that describes the parse failure.</param>
public sealed class ParseException(Diagnostic diagnostic) : Exception(diagnostic.ToString())
{
    /// <summary>
    /// Gets the diagnostic that describes the parse failure.
    /// </summary>
    public Diagnostic Diagnostic { get; } = diagnostic;
}
