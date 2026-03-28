namespace TableGen.Text;

using System;

/// <summary>
/// Represents a TableGen parse failure.
/// </summary>
public sealed class TableGenParseException(TableGenDiagnostic diagnostic)
    : Exception($"{diagnostic.Message} (line {diagnostic.Line}, column {diagnostic.Column})")
{
    /// <summary>
    /// Gets the parse diagnostic.
    /// </summary>
    public TableGenDiagnostic Diagnostic { get; } = diagnostic;
}
