namespace TableGen.Evaluation;

using System.Collections.Generic;

/// <summary>
/// Represents an interpreted TableGen document.
/// </summary>
public sealed class InterpretedDocument(IReadOnlyList<TableGenRecord> records)
{
    /// <summary>
    /// Gets the expanded top-level definitions in the document.
    /// </summary>
    public IReadOnlyList<TableGenRecord> Records { get; } = records;
}
