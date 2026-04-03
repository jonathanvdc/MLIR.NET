namespace MLIR.Text;

/// <summary>
/// Describes the outcome of a parsing step.
/// </summary>
internal enum ParseOutcome
{
    /// <summary>
    /// The parser matched the production and produced a value.
    /// </summary>
    Success,

    /// <summary>
    /// The parser did not match the production, but no diagnostic should be emitted.
    /// </summary>
    NoMatch,

    /// <summary>
    /// The parser encountered malformed syntax and produced a diagnostic.
    /// </summary>
    Error,
}

/// <summary>
/// Represents the outcome of a parsing step that may either succeed, fail to match, or report a diagnostic.
/// </summary>
/// <typeparam name="T">The parsed value type.</typeparam>
internal readonly struct ParseResult<T>
{
    private ParseResult(ParseOutcome outcome, T value, Diagnostic? diagnostic)
    {
        Outcome = outcome;
        Value = value;
        Diagnostic = diagnostic;
    }

    /// <summary>
    /// Gets the parse outcome.
    /// </summary>
    public ParseOutcome Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether parsing succeeded.
    /// </summary>
    public bool IsSuccess => Outcome == ParseOutcome.Success;

    /// <summary>
    /// Gets a value indicating whether parsing did not match the requested production.
    /// </summary>
    public bool IsNoMatch => Outcome == ParseOutcome.NoMatch;

    /// <summary>
    /// Gets a value indicating whether parsing failed with a diagnostic.
    /// </summary>
    public bool IsError => Outcome == ParseOutcome.Error;

    /// <summary>
    /// Gets the parsed value when <see cref="IsSuccess"/> is <see langword="true"/>.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets the diagnostic when <see cref="IsError"/> is <see langword="true"/>.
    /// </summary>
    public Diagnostic? Diagnostic { get; }

    /// <summary>
    /// Creates a successful parse result.
    /// </summary>
    public static ParseResult<T> Success(T value)
    {
        return new ParseResult<T>(ParseOutcome.Success, value, null);
    }

    /// <summary>
    /// Creates a no-match parse result.
    /// </summary>
    public static ParseResult<T> NoMatch()
    {
        return new ParseResult<T>(ParseOutcome.NoMatch, default!, null);
    }

    /// <summary>
    /// Creates a failed parse result.
    /// </summary>
    public static ParseResult<T> Failure(Diagnostic diagnostic)
    {
        return new ParseResult<T>(ParseOutcome.Error, default!, diagnostic);
    }
}
