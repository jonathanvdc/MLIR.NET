namespace TableGen.Evaluation;

using System;
using System.Collections.Generic;

internal enum EvaluationDiagnosticKind
{
    InvalidOperation,
    MissingKey,
    InvalidCast,
}

internal sealed class EvaluationDiagnostic
{
    public EvaluationDiagnostic(EvaluationDiagnosticKind kind, string message)
    {
        Kind = kind;
        Message = message;
    }

    public EvaluationDiagnosticKind Kind { get; }

    public string Message { get; }

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
