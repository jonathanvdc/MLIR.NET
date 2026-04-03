namespace TableGen.Evaluation;

using System;
using System.Collections.Generic;

/// <summary>
/// Classifies interpreter failures so they can be turned into conventional CLR exceptions.
/// </summary>
internal enum EvaluationDiagnosticKind
{
    /// <summary>
    /// Indicates that the program is structurally valid but requested an unsupported or invalid operation.
    /// </summary>
    InvalidOperation,

    /// <summary>
    /// Indicates that a named lookup failed.
    /// </summary>
    MissingKey,

    /// <summary>
    /// Indicates that a value had the wrong runtime shape for the requested operation.
    /// </summary>
    InvalidCast,
}

/// <summary>
/// Represents a single evaluation failure before it is re-thrown as an exception.
/// </summary>
internal sealed class EvaluationDiagnostic
{
    /// <summary>
    /// Initializes a new diagnostic.
    /// </summary>
    /// <param name="kind">The broad category of failure.</param>
    /// <param name="message">The user-facing error message.</param>
    public EvaluationDiagnostic(EvaluationDiagnosticKind kind, string message)
    {
        Kind = kind;
        Message = message;
    }

    /// <summary>
    /// Gets the failure category.
    /// </summary>
    public EvaluationDiagnosticKind Kind { get; }

    /// <summary>
    /// Gets the user-facing error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Converts this diagnostic into the closest matching CLR exception type.
    /// </summary>
    /// <returns>An exception with the diagnostic message.</returns>
    public Exception ToException()
    {
        return Kind switch
        {
            EvaluationDiagnosticKind.InvalidOperation => new InvalidOperationException(Message),
            EvaluationDiagnosticKind.MissingKey => new KeyNotFoundException(Message),
            EvaluationDiagnosticKind.InvalidCast => new InvalidCastException(Message),
            _ => new InvalidOperationException(Message),
        };
    }
}
