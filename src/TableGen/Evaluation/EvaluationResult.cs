namespace TableGen.Evaluation;

/// <summary>
/// Represents either a successful evaluation result or an evaluation diagnostic.
/// </summary>
/// <typeparam name="T">The successful result type.</typeparam>
internal sealed class EvaluationResult<T>
{
    /// <summary>
    /// Initializes a successful result.
    /// </summary>
    /// <param name="value">The computed value.</param>
    private EvaluationResult(T value)
    {
        IsSuccess = true;
        Value = value;
        Diagnostic = null;
    }

    /// <summary>
    /// Initializes a failed result.
    /// </summary>
    /// <param name="diagnostic">The reason evaluation failed.</param>
    private EvaluationResult(EvaluationDiagnostic diagnostic)
    {
        IsSuccess = false;
        Value = default!;
        Diagnostic = diagnostic;
    }

    /// <summary>
    /// Gets a value indicating whether evaluation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the computed value when <see cref="IsSuccess"/> is <see langword="true"/>.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets the evaluation diagnostic when <see cref="IsSuccess"/> is <see langword="false"/>.
    /// </summary>
    public EvaluationDiagnostic? Diagnostic { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The computed value.</param>
    /// <returns>A successful evaluation result.</returns>
    public static EvaluationResult<T> Success(T value)
    {
        return new EvaluationResult<T>(value);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    /// <returns>A failed evaluation result.</returns>
    public static EvaluationResult<T> Failure(EvaluationDiagnostic diagnostic)
    {
        return new EvaluationResult<T>(diagnostic);
    }
}
