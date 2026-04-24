namespace MLIR.Text;

using System;

/// <summary>
/// Describes the outcome of a parsing step.
/// </summary>
public enum ParseOutcome
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
/// Exposes the common outcome and diagnostic data shared by all typed parse results.
/// </summary>
public interface IParseResult
{
    /// <summary>
    /// Gets the parse outcome.
    /// </summary>
    ParseOutcome Outcome { get; }

    /// <summary>
    /// Gets the diagnostic when the outcome is <see cref="ParseOutcome.Error"/>.
    /// </summary>
    Diagnostic? Diagnostic { get; }
}

/// <summary>
/// Represents the outcome of a parsing step that may either succeed, fail to match, or report a diagnostic.
/// </summary>
/// <typeparam name="T">The parsed value type.</typeparam>
public readonly struct ParseResult<T> : IParseResult
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
    /// Projects the successful parse value into a different shape while preserving failure and no-match outcomes.
    /// </summary>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="map">The projection to apply on success.</param>
    /// <returns>The projected parse result.</returns>
    public ParseResult<TResult> Map<TResult>(Func<T, TResult> map)
    {
        return Outcome switch
        {
            ParseOutcome.Success => ParseResult<TResult>.Success(map(Value)),
            ParseOutcome.NoMatch => ParseResult<TResult>.NoMatch(),
            _ => ParseResult<TResult>.Failure(Diagnostic!),
        };
    }

    /// <summary>
    /// Chains another parse step after a successful result while preserving failure and no-match outcomes.
    /// </summary>
    /// <typeparam name="TResult">The chained result type.</typeparam>
    /// <param name="bind">The chained parse step.</param>
    /// <returns>The chained parse result.</returns>
    public ParseResult<TResult> Bind<TResult>(Func<T, ParseResult<TResult>> bind)
    {
        return Outcome switch
        {
            ParseOutcome.Success => bind(Value),
            ParseOutcome.NoMatch => ParseResult<TResult>.NoMatch(),
            _ => ParseResult<TResult>.Failure(Diagnostic!),
        };
    }

    /// <summary>
    /// Converts the parse result to the legacy boolean-style contract used by public custom assembly hooks.
    /// </summary>
    /// <param name="value">Receives the parsed value on success.</param>
    /// <returns><see langword="true"/> when parsing succeeded; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(out T value)
    {
        value = IsSuccess ? Value : default!;
        return IsSuccess;
    }

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
