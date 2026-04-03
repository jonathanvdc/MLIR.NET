namespace TableGen.Evaluation;

using System;

internal sealed class EvaluationResult<T>
{
    private EvaluationResult(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = null;
    }

    private EvaluationResult(Exception error)
    {
        IsSuccess = false;
        Value = default!;
        Error = error;
    }

    public bool IsSuccess { get; }

    public T Value { get; }

    public Exception? Error { get; }

    public static EvaluationResult<T> Success(T value)
    {
        return new EvaluationResult<T>(value);
    }

    public static EvaluationResult<T> Failure(Exception error)
    {
        return new EvaluationResult<T>(error);
    }
}
