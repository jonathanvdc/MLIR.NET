namespace TableGen.Text;

using System;

/// <summary>
/// Represents a TableGen parse failure.
/// </summary>
public sealed class ParseException(Diagnostic diagnostic)
    : Exception($"{diagnostic.Message} (line {diagnostic.Line}, column {diagnostic.Column})")
{
    /// <summary>
    /// Gets the parse diagnostic.
    /// </summary>
    public Diagnostic Diagnostic { get; } = diagnostic;
}
