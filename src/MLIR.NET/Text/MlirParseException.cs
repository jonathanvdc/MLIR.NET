namespace MLIR.Text;

using System;

/// <summary>
/// Represents a fatal parse error produced while reading MLIR text.
/// </summary>
public sealed class MlirParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MlirParseException"/> class.
    /// </summary>
    /// <param name="diagnostic">The diagnostic that describes the parse failure.</param>
    public MlirParseException(MlirDiagnostic diagnostic)
        : base(diagnostic.ToString())
    {
        Diagnostic = diagnostic;
    }

    /// <summary>
    /// Gets the diagnostic that describes the parse failure.
    /// </summary>
    public MlirDiagnostic Diagnostic { get; }
}
