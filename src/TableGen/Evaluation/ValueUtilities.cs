namespace TableGen.Evaluation;

using System;
using System.Collections.Generic;
using System.Globalization;

internal static class ValueUtilities
{
    public static EvaluationResult<bool> TryIsTruthy(Value value)
    {
        return value switch
        {
            IntegerValue integer => EvaluationResult<bool>.Success(integer.Value != 0),
            BitValue bit => EvaluationResult<bool>.Success(bit.Value),
            _ => EvaluationResult<bool>.Failure(InvalidOperation($"Expected a boolean-like condition, got {value.GetType().Name}.")),
        };
    }

    public static bool IsTruthy(Value value)
    {
        var result = TryIsTruthy(value);
        if (!result.IsSuccess)
        {
            throw result.Diagnostic!.ToException();
        }

        return result.Value;
    }

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

    public static string ValueToString(Value value)
    {
        var result = TryValueToString(value);
        if (!result.IsSuccess)
        {
            throw result.Diagnostic!.ToException();
        }

        return result.Value;
    }

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

    public static Value CoerceValue(string typeName, Value value)
    {
        var result = TryCoerceValue(typeName, value);
        if (!result.IsSuccess)
        {
            throw result.Diagnostic!.ToException();
        }

        return result.Value;
    }

    public static EvaluationResult<Value> TryCoerceExistingValue(Value existingValue, Value replacementValue)
    {
        return existingValue switch
        {
            BitValue when replacementValue is IntegerValue integer => Success(new BitValue(integer.Value != 0)),
            BitValue when replacementValue is BitValue => Success(replacementValue),
            _ => Success(replacementValue),
        };
    }

    public static Value CoerceExistingValue(Value existingValue, Value replacementValue)
    {
        var result = TryCoerceExistingValue(existingValue, replacementValue);
        if (!result.IsSuccess)
        {
            throw result.Diagnostic!.ToException();
        }

        return result.Value;
    }

    public static EvaluationResult<int> TryToInteger(Value value, string contextName)
    {
        return value is IntegerValue integer
            ? EvaluationResult<int>.Success(integer.Value)
            : EvaluationResult<int>.Failure(InvalidOperation($"{contextName} requires an integer argument, got {value.GetType().Name}."));
    }

    public static EvaluationResult<string> TryToString(Value value, string contextName)
    {
        return value is StringValue str
            ? EvaluationResult<string>.Success(str.Value)
            : EvaluationResult<string>.Failure(InvalidOperation($"{contextName} requires a string argument, got {value.GetType().Name}."));
    }

    public static EvaluationResult<int> TryNormalizeIndex(int index, int length, string contextName)
    {
        var normalized = index < 0 ? length + index : index;
        return normalized < 0 || normalized >= length
            ? EvaluationResult<int>.Failure(InvalidOperation($"{contextName} index {index} is out of range."))
            : EvaluationResult<int>.Success(normalized);
    }

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

    private static EvaluationDiagnostic InvalidOperation(string message)
    {
        return new EvaluationDiagnostic(EvaluationDiagnosticKind.InvalidOperation, message);
    }

    private static EvaluationResult<Value> Success(Value value)
    {
        return EvaluationResult<Value>.Success(value);
    }

    private static EvaluationResult<Value> Failure(EvaluationDiagnostic diagnostic)
    {
        return EvaluationResult<Value>.Failure(diagnostic);
    }
}
