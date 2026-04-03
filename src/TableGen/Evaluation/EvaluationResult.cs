namespace TableGen.Evaluation;

internal sealed class EvaluationResult<T>
{
    private EvaluationResult(T value)
    {
        IsSuccess = true;
        Value = value;
        Diagnostic = null;
    }

    private EvaluationResult(EvaluationDiagnostic diagnostic)
    {
        IsSuccess = false;
        Value = default!;
        Diagnostic = diagnostic;
    }

    public bool IsSuccess { get; }

    public T Value { get; }

    public EvaluationDiagnostic? Diagnostic { get; }

    public static EvaluationResult<T> Success(T value)
    {
        return new EvaluationResult<T>(value);
    }

    public static EvaluationResult<T> Failure(EvaluationDiagnostic diagnostic)
    {
        return new EvaluationResult<T>(diagnostic);
    }
}
