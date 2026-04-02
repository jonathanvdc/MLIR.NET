namespace TableGen.Text;

using System;

/// <summary>
/// Represents a TableGen parse failure.
/// </summary>
public sealed class ParseException(Diagnostic diagnostic)
    : Exception(FormatMessage(diagnostic))
{
    /// <summary>
    /// Gets the parse diagnostic.
    /// </summary>
    public Diagnostic Diagnostic { get; } = diagnostic;

    private static string FormatMessage(Diagnostic diagnostic)
    {
        var location = diagnostic.SourceFilePath != null
            ? $" in '{diagnostic.SourceFilePath}'"
            : string.Empty;
        return $"{diagnostic.Message}{location} (line {diagnostic.Line}, column {diagnostic.Column})";
    }
}
