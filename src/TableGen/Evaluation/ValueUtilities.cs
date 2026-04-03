namespace TableGen.Evaluation;

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
    /// <returns>The boolean interpretation or a diagnostic.</returns>
    public static EvaluationResult<bool> TryIsTruthy(Value value)
    {
        return value switch
        {
            IntegerValue integer => EvaluationResult<bool>.Success(integer.Value != 0),
            BitValue bit => EvaluationResult<bool>.Success(bit.Value),
            _ => EvaluationResult<bool>.Failure(InvalidOperation($"Expected a boolean-like condition, got {value.GetType().Name}.")),
        };
    }

    /// <summary>
    /// Converts a runtime value into TableGen truthiness and throws on failure.
    /// </summary>
    /// <param name="value">The value to interpret as a condition.</param>
    /// <returns>The boolean interpretation.</returns>
    public static bool IsTruthy(Value value)
    {
        var result = TryIsTruthy(value);
        if (!result.IsSuccess)
        {
            throw result.Diagnostic!.ToException();
        }

        return result.Value;
    }

    /// <summary>
    /// Converts a runtime value into the string form used by TableGen concatenation and string-oriented builtins.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted string or a diagnostic.</returns>
    public static EvaluationResult<string> TryValueToString(Value value)
    {
        switch (value)
        {
            case StringValue str:
                return EvaluationResult<string>.Success(str.Value);
            case IntegerValue integer:
                return EvaluationResult<string>.Success(integer.Value.ToString(CultureInfo.InvariantCulture));
            case BitValue bit:
                return EvaluationResult<string>.Success(bit.Value ? "1" : "0");
            case ListValue list:
            {
                var pieces = new List<string>(list.Items.Count);
                foreach (var item in list.Items)
                {
                    var itemString = TryValueToString(item);
                    if (!itemString.IsSuccess)
                    {
                        return EvaluationResult<string>.Failure(itemString.Diagnostic!);
                    }

                    pieces.Add(itemString.Value);
                }

                return EvaluationResult<string>.Success(string.Concat(pieces));
            }
            case SymbolReferenceValue symbol:
                return EvaluationResult<string>.Success(symbol.SymbolName);
            case RecordReferenceValue record:
                return EvaluationResult<string>.Success(record.RecordName);
            case UnsetValue:
                return EvaluationResult<string>.Success(string.Empty);
            case AnonymousRecordValue rec:
                return EvaluationResult<string>.Success(rec.ClassName);
            default:
                return EvaluationResult<string>.Failure(InvalidOperation($"Cannot convert {value.GetType().Name} to string for concatenation."));
        }
    }

    /// <summary>
    /// Converts a runtime value into the string form used by TableGen and throws on failure.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted string.</returns>
    public static string ValueToString(Value value)
    {
        var result = TryValueToString(value);
        if (!result.IsSuccess)
        {
            throw result.Diagnostic!.ToException();
        }

        return result.Value;
    }

    /// <summary>
    /// Coerces a value to the declared TableGen field type when the interpreter needs to enforce field types.
    /// </summary>
    /// <param name="typeName">The declared TableGen type name.</param>
    /// <param name="value">The runtime value to coerce.</param>
    /// <returns>The coerced value or a diagnostic.</returns>
    public static EvaluationResult<Value> TryCoerceValue(string typeName, Value value)
    {
        if (value is UnsetValue)
        {
            return Success(value);
        }

        switch (typeName)
        {
            case "int" when value is not IntegerValue:
                return Failure(InvalidOperation($"Expected an integer value for '{typeName}'."));
            case "string" when value is not StringValue:
            {
                var stringResult = TryValueToString(value);
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
                return Failure(InvalidOperation($"Expected a bit value for '{typeName}'."));
            case "dag" when value is not DagValue:
                return Failure(InvalidOperation($"Expected a dag value for '{typeName}'."));
            default:
                return Success(value);
        }
    }

    /// <summary>
    /// Coerces a value to the declared TableGen field type and throws on failure.
    /// </summary>
    /// <param name="typeName">The declared TableGen type name.</param>
    /// <param name="value">The runtime value to coerce.</param>
    /// <returns>The coerced value.</returns>
    public static Value CoerceValue(string typeName, Value value)
    {
        var result = TryCoerceValue(typeName, value);
        if (!result.IsSuccess)
        {
            throw result.Diagnostic!.ToException();
        }

        return result.Value;
    }

    /// <summary>
    /// Adapts a replacement value to the runtime shape of an existing value when overriding already-resolved fields.
    /// </summary>
    /// <param name="existingValue">The existing runtime value.</param>
    /// <param name="replacementValue">The replacement runtime value.</param>
    /// <returns>The adapted replacement value or a diagnostic.</returns>
    public static EvaluationResult<Value> TryCoerceExistingValue(Value existingValue, Value replacementValue)
    {
        return existingValue switch
        {
            BitValue when replacementValue is IntegerValue integer => Success(new BitValue(integer.Value != 0)),
            BitValue when replacementValue is BitValue => Success(replacementValue),
            _ => Success(replacementValue),
        };
    }

    /// <summary>
    /// Adapts a replacement value to the runtime shape of an existing value and throws on failure.
    /// </summary>
    /// <param name="existingValue">The existing runtime value.</param>
    /// <param name="replacementValue">The replacement runtime value.</param>
    /// <returns>The adapted replacement value.</returns>
    public static Value CoerceExistingValue(Value existingValue, Value replacementValue)
    {
        var result = TryCoerceExistingValue(existingValue, replacementValue);
        if (!result.IsSuccess)
        {
            throw result.Diagnostic!.ToException();
        }

        return result.Value;
    }

    /// <summary>
    /// Requires that a runtime value be an integer.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <param name="contextName">A human-readable operator name for diagnostics.</param>
    /// <returns>The integer value or a diagnostic.</returns>
    public static EvaluationResult<int> TryToInteger(Value value, string contextName)
    {
        return value is IntegerValue integer
            ? EvaluationResult<int>.Success(integer.Value)
            : EvaluationResult<int>.Failure(InvalidOperation($"{contextName} requires an integer argument, got {value.GetType().Name}."));
    }

    /// <summary>
    /// Requires that a runtime value be a string.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <param name="contextName">A human-readable operator name for diagnostics.</param>
    /// <returns>The string value or a diagnostic.</returns>
    public static EvaluationResult<string> TryToString(Value value, string contextName)
    {
        return value is StringValue str
            ? EvaluationResult<string>.Success(str.Value)
            : EvaluationResult<string>.Failure(InvalidOperation($"{contextName} requires a string argument, got {value.GetType().Name}."));
    }

    /// <summary>
    /// Normalizes negative indices and checks bounds for list and string subscripts.
    /// </summary>
    /// <param name="index">The raw index written in source.</param>
    /// <param name="length">The collection length.</param>
    /// <param name="contextName">A human-readable operator name for diagnostics.</param>
    /// <returns>The normalized index or a diagnostic.</returns>
    public static EvaluationResult<int> TryNormalizeIndex(int index, int length, string contextName)
    {
        var normalized = index < 0 ? length + index : index;
        return normalized < 0 || normalized >= length
            ? EvaluationResult<int>.Failure(InvalidOperation($"{contextName} index {index} is out of range."))
            : EvaluationResult<int>.Success(normalized);
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
    /// <returns>The constructed diagnostic.</returns>
    private static EvaluationDiagnostic InvalidOperation(string message)
    {
        return new EvaluationDiagnostic(EvaluationDiagnosticKind.InvalidOperation, message);
    }

    /// <summary>
    /// Creates a successful value result.
    /// </summary>
    /// <param name="value">The computed value.</param>
    /// <returns>A successful evaluation result.</returns>
    private static EvaluationResult<Value> Success(Value value)
    {
        return EvaluationResult<Value>.Success(value);
    }

    /// <summary>
    /// Creates a failed value result.
    /// </summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    /// <returns>A failed evaluation result.</returns>
    private static EvaluationResult<Value> Failure(EvaluationDiagnostic diagnostic)
    {
        return EvaluationResult<Value>.Failure(diagnostic);
    }
}
