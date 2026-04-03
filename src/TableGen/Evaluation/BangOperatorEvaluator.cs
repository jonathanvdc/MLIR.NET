namespace TableGen.Evaluation;

using System;
using System.Collections.Generic;
using System.Linq;

using TableGen.Syntax;

internal sealed class BangOperatorEvaluator
{
    private readonly EvaluationContext context;
    private readonly IdentifierResolver identifierResolver;
    private readonly Func<ExpressionSyntax, Scope, ExpressionEvaluator.TryResolveValue?, EvaluationResult<Value>> tryEvaluate;

    public BangOperatorEvaluator(
        EvaluationContext context,
        IdentifierResolver identifierResolver,
        Func<ExpressionSyntax, Scope, ExpressionEvaluator.TryResolveValue?, EvaluationResult<Value>> tryEvaluate)
    {
        this.context = context;
        this.identifierResolver = identifierResolver;
        this.tryEvaluate = tryEvaluate;
    }

    public EvaluationResult<Value> Evaluate(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        switch (bangCall.OperatorName)
        {
            case "if":
            {
                var cond = tryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
                if (!cond.IsSuccess)
                {
                    return Failure(cond.Diagnostic!);
                }

                var truthy = ValueUtilities.TryIsTruthy(cond.Value);
                if (!truthy.IsSuccess)
                {
                    return Failure(truthy.Diagnostic!);
                }

                return tryEvaluate(truthy.Value ? bangCall.Arguments[1] : bangCall.Arguments[2], scope, tryResolveValue);
            }
            case "gt":
                return TryEvaluateIntegerComparison(bangCall, scope, tryResolveValue, static (a, b) => a > b);
            case "ge":
                return TryEvaluateIntegerComparison(bangCall, scope, tryResolveValue, static (a, b) => a >= b);
            case "lt":
                return TryEvaluateIntegerComparison(bangCall, scope, tryResolveValue, static (a, b) => a < b);
            case "le":
                return TryEvaluateIntegerComparison(bangCall, scope, tryResolveValue, static (a, b) => a <= b);
            case "eq":
                return TryEvaluateEquality(bangCall, scope, tryResolveValue, expectedEqual: true);
            case "ne":
                return TryEvaluateEquality(bangCall, scope, tryResolveValue, expectedEqual: false);
            case "add":
                return TryEvaluateIntegerBinary(bangCall, scope, tryResolveValue, "!add", static (a, b) => a + b);
            case "sub":
                return TryEvaluateIntegerBinary(bangCall, scope, tryResolveValue, "!sub", static (a, b) => a - b);
            case "mul":
                return TryEvaluateIntegerBinary(bangCall, scope, tryResolveValue, "!mul", static (a, b) => a * b);
            case "and":
                return TryEvaluateIntegerBinary(bangCall, scope, tryResolveValue, "!and", static (a, b) => a & b);
            case "or":
                return TryEvaluateIntegerBinary(bangCall, scope, tryResolveValue, "!or", static (a, b) => a | b);
            case "not":
            {
                var val = tryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
                if (!val.IsSuccess)
                {
                    return Failure(val.Diagnostic!);
                }

                var truthy = ValueUtilities.TryIsTruthy(val.Value);
                return truthy.IsSuccess
                    ? Success(new IntegerValue(truthy.Value ? 0 : 1))
                    : Failure(truthy.Diagnostic!);
            }
            case "size":
            {
                var val = tryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
                if (!val.IsSuccess)
                {
                    return Failure(val.Diagnostic!);
                }

                return val.Value switch
                {
                    StringValue str => Success(new IntegerValue(str.Value.Length)),
                    ListValue list => Success(new IntegerValue(list.Items.Count)),
                    _ => Failure(InvalidOperation($"!size requires a string or list argument, got {val.Value.GetType().Name}.")),
                };
            }
            case "toupper":
                return TryEvaluateUnaryString(bangCall, scope, tryResolveValue, "!toupper", static str => str.ToUpperInvariant());
            case "tolower":
                return TryEvaluateUnaryString(bangCall, scope, tryResolveValue, "!tolower", static str => str.ToLowerInvariant());
            case "substr":
                return TryEvaluateSubstr(bangCall, scope, tryResolveValue);
            case "find":
                return TryEvaluateFind(bangCall, scope, tryResolveValue);
            case "range":
                return TryEvaluateRange(bangCall, scope, tryResolveValue);
            case "listconcat":
                return TryEvaluateListConcat(bangCall, scope, tryResolveValue);
            case "strconcat":
                return TryEvaluateStrConcat(bangCall, scope, tryResolveValue);
            case "shl":
                return TryEvaluateIntegerBinary(bangCall, scope, tryResolveValue, "!shl", static (a, b) => a << b);
            case "cast":
                return tryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
            case "isa":
            {
                var val = tryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
                return val.IsSuccess
                    ? Success(new IntegerValue(identifierResolver.IsValueOfType(val.Value, bangCall.TypeArgument) ? 1 : 0))
                    : Failure(val.Diagnostic!);
            }
            case "cond":
                return TryEvaluateCond(bangCall, scope, tryResolveValue);
            case "interleave":
                return TryEvaluateInterleave(bangCall, scope, tryResolveValue);
            case "subst":
                return TryEvaluateSubst(bangCall, scope, tryResolveValue);
            case "head":
                return TryEvaluateHead(bangCall, scope, tryResolveValue);
            case "tail":
                return TryEvaluateTail(bangCall, scope, tryResolveValue);
            case "empty":
                return TryEvaluateEmpty(bangCall, scope, tryResolveValue);
            case "filter":
                return TryEvaluateFilter(bangCall, scope, tryResolveValue);
            default:
                return Failure(InvalidOperation($"Unknown bang operator '!{bangCall.OperatorName}'."));
        }
    }

    private EvaluationResult<Value> TryEvaluateIntegerComparison(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue,
        Func<int, int, bool> predicate)
    {
        var a = TryEvaluateInteger(bangCall.Arguments[0], scope, tryResolveValue, $"!{bangCall.OperatorName}");
        if (!a.IsSuccess)
        {
            return Failure(a.Diagnostic!);
        }

        var b = TryEvaluateInteger(bangCall.Arguments[1], scope, tryResolveValue, $"!{bangCall.OperatorName}");
        return b.IsSuccess
            ? Success(new IntegerValue(predicate(a.Value, b.Value) ? 1 : 0))
            : Failure(b.Diagnostic!);
    }

    private EvaluationResult<Value> TryEvaluateEquality(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue,
        bool expectedEqual)
    {
        var a = tryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!a.IsSuccess)
        {
            return Failure(a.Diagnostic!);
        }

        var b = tryEvaluate(bangCall.Arguments[1], scope, tryResolveValue);
        if (!b.IsSuccess)
        {
            return Failure(b.Diagnostic!);
        }

        var isEqual = ValueUtilities.ValuesEqual(a.Value, b.Value);
        return Success(new IntegerValue(isEqual == expectedEqual ? 1 : 0));
    }

    private EvaluationResult<Value> TryEvaluateIntegerBinary(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue,
        string contextName,
        Func<int, int, int> operation)
    {
        var a = TryEvaluateInteger(bangCall.Arguments[0], scope, tryResolveValue, contextName);
        if (!a.IsSuccess)
        {
            return Failure(a.Diagnostic!);
        }

        var b = TryEvaluateInteger(bangCall.Arguments[1], scope, tryResolveValue, contextName);
        return b.IsSuccess
            ? Success(new IntegerValue(operation(a.Value, b.Value)))
            : Failure(b.Diagnostic!);
    }

    private EvaluationResult<Value> TryEvaluateUnaryString(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue,
        string contextName,
        Func<string, string> operation)
    {
        var text = TryEvaluateString(bangCall.Arguments[0], scope, tryResolveValue, contextName);
        return text.IsSuccess
            ? Success(new StringValue(operation(text.Value)))
            : Failure(text.Diagnostic!);
    }

    private EvaluationResult<Value> TryEvaluateSubstr(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        var str = TryEvaluateString(bangCall.Arguments[0], scope, tryResolveValue, "!substr");
        if (!str.IsSuccess)
        {
            return Failure(str.Diagnostic!);
        }

        var start = TryEvaluateInteger(bangCall.Arguments[1], scope, tryResolveValue, "!substr");
        if (!start.IsSuccess)
        {
            return Failure(start.Diagnostic!);
        }

        var clampedStart = Math.Max(0, Math.Min(start.Value, str.Value.Length));
        if (bangCall.Arguments.Count >= 3)
        {
            var len = TryEvaluateInteger(bangCall.Arguments[2], scope, tryResolveValue, "!substr");
            if (!len.IsSuccess)
            {
                return Failure(len.Diagnostic!);
            }

            var clampedLen = Math.Max(0, Math.Min(len.Value, str.Value.Length - clampedStart));
            return Success(new StringValue(str.Value.Substring(clampedStart, clampedLen)));
        }

        return Success(new StringValue(str.Value.Substring(clampedStart)));
    }

    private EvaluationResult<Value> TryEvaluateFind(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        var str = TryEvaluateString(bangCall.Arguments[0], scope, tryResolveValue, "!find");
        if (!str.IsSuccess)
        {
            return Failure(str.Diagnostic!);
        }

        var sub = TryEvaluateString(bangCall.Arguments[1], scope, tryResolveValue, "!find");
        if (!sub.IsSuccess)
        {
            return Failure(sub.Diagnostic!);
        }

        var startIndex = bangCall.Arguments.Count >= 3
            ? TryEvaluateInteger(bangCall.Arguments[2], scope, tryResolveValue, "!find")
            : EvaluationResult<int>.Success(0);
        if (!startIndex.IsSuccess)
        {
            return Failure(startIndex.Diagnostic!);
        }

        return Success(new IntegerValue(str.Value.IndexOf(sub.Value, startIndex.Value, StringComparison.Ordinal)));
    }

    private EvaluationResult<Value> TryEvaluateRange(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        var start = TryEvaluateInteger(bangCall.Arguments[0], scope, tryResolveValue, "!range");
        if (!start.IsSuccess)
        {
            return Failure(start.Diagnostic!);
        }

        var end = TryEvaluateInteger(bangCall.Arguments[1], scope, tryResolveValue, "!range");
        if (!end.IsSuccess)
        {
            return Failure(end.Diagnostic!);
        }

        var items = new List<Value>(Math.Max(0, end.Value - start.Value));
        for (var i = start.Value; i < end.Value; i++)
        {
            items.Add(new IntegerValue(i));
        }

        return Success(new ListValue(items));
    }

    private EvaluationResult<Value> TryEvaluateListConcat(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        var a = tryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!a.IsSuccess)
        {
            return Failure(a.Diagnostic!);
        }

        var b = tryEvaluate(bangCall.Arguments[1], scope, tryResolveValue);
        if (!b.IsSuccess)
        {
            return Failure(b.Diagnostic!);
        }

        if (a.Value is not ListValue left || b.Value is not ListValue right)
        {
            return Failure(InvalidCast("Expected list arguments."));
        }

        var items = new List<Value>(left.Items.Count + right.Items.Count);
        items.AddRange(left.Items);
        items.AddRange(right.Items);
        return Success(new ListValue(items));
    }

    private EvaluationResult<Value> TryEvaluateStrConcat(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        var parts = new List<string>(bangCall.Arguments.Count);
        foreach (var arg in bangCall.Arguments)
        {
            var value = tryEvaluate(arg, scope, tryResolveValue);
            if (!value.IsSuccess)
            {
                return Failure(value.Diagnostic!);
            }

            var text = ValueUtilities.TryToString(value.Value, "!strconcat");
            if (!text.IsSuccess)
            {
                return Failure(text.Diagnostic!);
            }

            parts.Add(text.Value);
        }

        return Success(new StringValue(string.Concat(parts)));
    }

    private EvaluationResult<Value> TryEvaluateCond(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        for (var i = 0; i + 1 < bangCall.Arguments.Count; i += 2)
        {
            var condition = tryEvaluate(bangCall.Arguments[i], scope, tryResolveValue);
            if (!condition.IsSuccess)
            {
                return Failure(condition.Diagnostic!);
            }

            var truthy = ValueUtilities.TryIsTruthy(condition.Value);
            if (!truthy.IsSuccess)
            {
                return Failure(truthy.Diagnostic!);
            }

            if (truthy.Value)
            {
                return tryEvaluate(bangCall.Arguments[i + 1], scope, tryResolveValue);
            }
        }

        return Failure(InvalidOperation("!cond requires at least one true condition."));
    }

    private EvaluationResult<Value> TryEvaluateInterleave(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        var listVal = tryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!listVal.IsSuccess)
        {
            return Failure(listVal.Diagnostic!);
        }

        var sep = TryEvaluateString(bangCall.Arguments[1], scope, tryResolveValue, "!interleave");
        if (!sep.IsSuccess)
        {
            return Failure(sep.Diagnostic!);
        }

        if (listVal.Value is not ListValue list)
        {
            return Failure(InvalidCast("Expected a list value."));
        }

        var items = new List<string>(list.Items.Count);
        foreach (var item in list.Items)
        {
            var itemString = ValueUtilities.TryValueToString(item);
            if (!itemString.IsSuccess)
            {
                return Failure(itemString.Diagnostic!);
            }

            items.Add(itemString.Value);
        }

        return Success(new StringValue(string.Join(sep.Value, items)));
    }

    private EvaluationResult<Value> TryEvaluateSubst(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        var from = TryEvaluateString(bangCall.Arguments[0], scope, tryResolveValue, "!subst");
        if (!from.IsSuccess)
        {
            return Failure(from.Diagnostic!);
        }

        var to = TryEvaluateString(bangCall.Arguments[1], scope, tryResolveValue, "!subst");
        if (!to.IsSuccess)
        {
            return Failure(to.Diagnostic!);
        }

        var text = TryEvaluateString(bangCall.Arguments[2], scope, tryResolveValue, "!subst");
        return text.IsSuccess
            ? Success(new StringValue(text.Value.Replace(from.Value, to.Value)))
            : Failure(text.Diagnostic!);
    }

    private EvaluationResult<Value> TryEvaluateHead(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        var list = tryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!list.IsSuccess)
        {
            return Failure(list.Diagnostic!);
        }

        if (list.Value is not ListValue values)
        {
            return Failure(InvalidCast("Expected a list value."));
        }

        return values.Items.Count == 0
            ? Failure(InvalidOperation("!head requires a non-empty list."))
            : Success(values.Items[0]);
    }

    private EvaluationResult<Value> TryEvaluateTail(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        var list = tryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!list.IsSuccess)
        {
            return Failure(list.Diagnostic!);
        }

        if (list.Value is not ListValue values)
        {
            return Failure(InvalidCast("Expected a list value."));
        }

        return values.Items.Count == 0
            ? Failure(InvalidOperation("!tail requires a non-empty list."))
            : Success(new ListValue(values.Items.Skip(1).ToList()));
    }

    private EvaluationResult<Value> TryEvaluateEmpty(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        var val = tryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!val.IsSuccess)
        {
            return Failure(val.Diagnostic!);
        }

        return val.Value switch
        {
            StringValue str => Success(new IntegerValue(string.IsNullOrEmpty(str.Value) ? 1 : 0)),
            ListValue list => Success(new IntegerValue(list.Items.Count == 0 ? 1 : 0)),
            UnsetValue => Success(new IntegerValue(1)),
            _ => Success(new IntegerValue(0)),
        };
    }

    private EvaluationResult<Value> TryEvaluateFilter(
        BangCallSyntax bangCall,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        var variable = ((IdentifierSyntax)bangCall.Arguments[0]).Name;
        var listValue = tryEvaluate(bangCall.Arguments[1], scope, tryResolveValue);
        if (!listValue.IsSuccess)
        {
            return Failure(listValue.Diagnostic!);
        }

        if (listValue.Value is not ListValue list)
        {
            return Failure(InvalidCast("Expected a list value."));
        }

        var results = new List<Value>();
        foreach (var item in list.Items)
        {
            var innerScope = scope.With(variable, item);
            var condition = tryEvaluate(bangCall.Arguments[2], innerScope, tryResolveValue);
            if (!condition.IsSuccess)
            {
                return Failure(condition.Diagnostic!);
            }

            var truthy = ValueUtilities.TryIsTruthy(condition.Value);
            if (!truthy.IsSuccess)
            {
                return Failure(truthy.Diagnostic!);
            }

            if (truthy.Value)
            {
                results.Add(item);
            }
        }

        return Success(new ListValue(results));
    }

    private EvaluationResult<int> TryEvaluateInteger(
        ExpressionSyntax expression,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue,
        string contextName)
    {
        var value = tryEvaluate(expression, scope, tryResolveValue);
        return !value.IsSuccess
            ? EvaluationResult<int>.Failure(value.Diagnostic!)
            : ValueUtilities.TryToInteger(value.Value, contextName);
    }

    private EvaluationResult<string> TryEvaluateString(
        ExpressionSyntax expression,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue,
        string contextName)
    {
        var value = tryEvaluate(expression, scope, tryResolveValue);
        return !value.IsSuccess
            ? EvaluationResult<string>.Failure(value.Diagnostic!)
            : ValueUtilities.TryToString(value.Value, contextName);
    }

    private static EvaluationDiagnostic InvalidOperation(string message)
    {
        return new EvaluationDiagnostic(EvaluationDiagnosticKind.InvalidOperation, message);
    }

    private static EvaluationDiagnostic InvalidCast(string message)
    {
        return new EvaluationDiagnostic(EvaluationDiagnosticKind.InvalidCast, message);
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
