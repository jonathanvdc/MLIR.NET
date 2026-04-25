namespace TableGen.Evaluation;

using MLIR.Text;
using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Centralizes common runtime value operations used by the evaluator.
/// </summary>
internal static class ValueUtilities
{
    /// <summary>
    /// Converts a runtime value into TableGen truthiness.
    /// </summary>
    /// <param name="value">The value to interpret as a condition.</param>
    /// <param name="location">The source location to attach to any produced diagnostic.</param>
    /// <returns>The boolean interpretation or a diagnostic.</returns>
    public static ParseResult<bool> TryIsTruthy(Value value, SourceLocation location)
    {
        return value switch
        {
            IntegerValue integer => ParseResult<bool>.Success(integer.Value != 0),
            BitValue bit => ParseResult<bool>.Success(bit.Value),
            _ => ParseResult<bool>.Failure(InvalidOperation($"Expected a boolean-like condition, got {value.GetType().Name}.", location)),
        };
    }

    /// <summary>
    /// Converts a runtime value into the string form used by TableGen concatenation and string-oriented builtins.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="location">The source location to attach to any produced diagnostic.</param>
    /// <returns>The converted string or a diagnostic.</returns>
    public static ParseResult<string> TryValueToString(Value value, SourceLocation location)
    {
        switch (value)
        {
            case StringValue str:
                return ParseResult<string>.Success(str.Value);
            case IntegerValue integer:
                return ParseResult<string>.Success(integer.Value.ToString(CultureInfo.InvariantCulture));
            case BitValue bit:
                return ParseResult<string>.Success(bit.Value ? "1" : "0");
            case ListValue list:
            {
                var pieces = new List<string>(list.Items.Count);
                foreach (var item in list.Items)
                {
                    var itemString = TryValueToString(item, location);
                    if (!itemString.IsSuccess)
                    {
                        return ParseResult<string>.Failure(itemString.Diagnostic!);
                    }

                    pieces.Add(itemString.Value);
                }

                return ParseResult<string>.Success(string.Concat(pieces));
            }
            case SymbolReferenceValue symbol:
                return ParseResult<string>.Success(symbol.SymbolName);
            case RecordReferenceValue record:
                return ParseResult<string>.Success(record.RecordName);
            case UnsetValue:
                return ParseResult<string>.Success(string.Empty);
            case RecordLikeValue recordLike:
                return ParseResult<string>.Success(recordLike.DisplayName);
            default:
                return ParseResult<string>.Failure(InvalidOperation($"Cannot convert {value.GetType().Name} to string for concatenation.", location));
        }
    }

    /// <summary>
    /// Coerces a value to the declared TableGen field type when the interpreter needs to enforce field types.
    /// </summary>
    /// <param name="typeName">The declared TableGen type name.</param>
    /// <param name="value">The runtime value to coerce.</param>
    /// <param name="location">The source location to attach to any produced diagnostic.</param>
    /// <returns>The coerced value or a diagnostic.</returns>
    public static ParseResult<Value> TryCoerceValue(string typeName, Value value, SourceLocation location)
    {
        if (value is UnsetValue)
        {
            return Success(value);
        }

        switch (typeName)
        {
            case "int" when value is not IntegerValue:
                return Failure(InvalidOperation($"Expected an integer value for '{typeName}'.", location));
            case "string" when value is not StringValue:
            {
                var stringResult = TryValueToString(value, location);
                return stringResult.IsSuccess
                    ? Success(new StringValue(stringResult.Value))
                    : Failure(stringResult.Diagnostic!);
            }
            case "code":
                return Success(value);
            case "bit" when value is IntegerValue integer:
                return Success(new BitValue(integer.Value != 0));
            case "bit" when value is BitValue:
                return Success(value);
            case "bit":
                return Failure(InvalidOperation($"Expected a bit value for '{typeName}'.", location));
            case "dag" when value is not DagValue:
                return Failure(InvalidOperation($"Expected a dag value for '{typeName}'.", location));
            default:
                return Success(value);
        }
    }

    /// <summary>
    /// Adapts a replacement value to the runtime shape of an existing value when overriding already-resolved fields.
    /// </summary>
    /// <param name="existingValue">The existing runtime value.</param>
    /// <param name="replacementValue">The replacement runtime value.</param>
    /// <returns>The adapted replacement value or a diagnostic.</returns>
    public static ParseResult<Value> TryCoerceExistingValue(Value existingValue, Value replacementValue)
    {
        return existingValue switch
        {
            BitValue when replacementValue is IntegerValue integer => Success(new BitValue(integer.Value != 0)),
            BitValue when replacementValue is BitValue => Success(replacementValue),
            _ => Success(replacementValue),
        };
    }

    /// <summary>
    /// Requires that a runtime value be an integer.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <param name="contextName">A human-readable operator name for diagnostics.</param>
    /// <param name="location">The source location to attach to any produced diagnostic.</param>
    /// <returns>The integer value or a diagnostic.</returns>
    public static ParseResult<int> TryToInteger(Value value, string contextName, SourceLocation location)
    {
        return value is IntegerValue integer
            ? ParseResult<int>.Success(integer.Value)
            : ParseResult<int>.Failure(InvalidOperation($"{contextName} requires an integer argument, got {value.GetType().Name}.", location));
    }

    /// <summary>
    /// Requires that a runtime value be a string.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <param name="contextName">A human-readable operator name for diagnostics.</param>
    /// <param name="location">The source location to attach to any produced diagnostic.</param>
    /// <returns>The string value or a diagnostic.</returns>
    public static ParseResult<string> TryToString(Value value, string contextName, SourceLocation location)
    {
        return value is StringValue str
            ? ParseResult<string>.Success(str.Value)
            : ParseResult<string>.Failure(InvalidOperation($"{contextName} requires a string argument, got {value.GetType().Name}.", location));
    }

    /// <summary>
    /// Normalizes negative indices and checks bounds for list and string subscripts.
    /// </summary>
    /// <param name="index">The raw index written in source.</param>
    /// <param name="length">The collection length.</param>
    /// <param name="contextName">A human-readable operator name for diagnostics.</param>
    /// <param name="location">The source location to attach to any produced diagnostic.</param>
    /// <returns>The normalized index or a diagnostic.</returns>
    public static ParseResult<int> TryNormalizeIndex(int index, int length, string contextName, SourceLocation location)
    {
        var normalized = index < 0 ? length + index : index;
        return normalized < 0 || normalized >= length
            ? ParseResult<int>.Failure(InvalidOperation($"{contextName} index {index} is out of range.", location))
            : ParseResult<int>.Success(normalized);
    }

    /// <summary>
    /// Compares two values for the subset of equality currently supported by the interpreter.
    /// </summary>
    /// <param name="a">The left value.</param>
    /// <param name="b">The right value.</param>
    /// <returns><see langword="true"/> when the values compare equal; otherwise <see langword="false"/>.</returns>
    public static bool ValuesEqual(Value a, Value b)
    {
        return (a, b) switch
        {
            (IntegerValue ia, IntegerValue ib) => ia.Value == ib.Value,
            (StringValue sa, StringValue sb) => sa.Value == sb.Value,
            (BitValue ba, BitValue bb) => ba.Value == bb.Value,
            _ => false,
        };
    }

    /// <summary>
    /// Creates an invalid-operation diagnostic with a consistent helper call site.
    /// </summary>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="location">The source location to attach to the diagnostic.</param>
    /// <returns>The constructed diagnostic.</returns>
    private static Diagnostic InvalidOperation(string message, SourceLocation location)
    {
        return new Diagnostic(message, location);
    }

    /// <summary>
    /// Creates a successful value result.
    /// </summary>
    /// <param name="value">The computed value.</param>
    /// <returns>A successful evaluation result.</returns>
    private static ParseResult<Value> Success(Value value)
    {
        return ParseResult<Value>.Success(value);
    }

    /// <summary>
    /// Creates a failed value result.
    /// </summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    /// <returns>A failed evaluation result.</returns>
    private static ParseResult<Value> Failure(Diagnostic diagnostic)
    {
        return ParseResult<Value>.Failure(diagnostic);
    }
}
