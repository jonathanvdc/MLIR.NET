namespace TableGen.Evaluation;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TableGen.Syntax;

/// <summary>
/// Evaluates TableGen expressions against a scope and document-level context.
/// </summary>
internal sealed class ExpressionEvaluator
{
    private readonly EvaluationContext context;
    private readonly Func<ClassSyntax, IReadOnlyList<ExpressionSyntax>, Scope, TryResolveValue?, EvaluationResult<Dictionary<string, Value>>> instantiateClass;
    private readonly Func<DefSyntax, EvaluationResult<Record>> buildDefinition;

    internal delegate EvaluationResult<Value> TryResolveValue(string name);

    public ExpressionEvaluator(
        EvaluationContext context,
        Func<ClassSyntax, IReadOnlyList<ExpressionSyntax>, Scope, TryResolveValue?, EvaluationResult<Dictionary<string, Value>>> instantiateClass,
        Func<DefSyntax, EvaluationResult<Record>> buildDefinition)
    {
        this.context = context;
        this.instantiateClass = instantiateClass;
        this.buildDefinition = buildDefinition;
    }

    public EvaluationResult<Value> TryEvaluate(
        ExpressionSyntax expression,
        Scope scope,
        TryResolveValue? tryResolveValue = null)
    {
        switch (expression)
        {
            case IntegerSyntax integer:
                return Success(new IntegerValue(integer.Value));
            case StringSyntax str:
                return Success(new StringValue(str.Value));
            case UnsetSyntax:
                return Success(new UnsetValue());
            case IdentifierSyntax identifier:
                return ResolveIdentifier(identifier.Name, scope, tryResolveValue);
            case ListSyntax list:
                return TryEvaluateList(list, scope, tryResolveValue);
            case DagSyntax dag:
                return TryEvaluateDag(dag, scope, tryResolveValue);
            case ConcatSyntax concat:
                return EvaluateConcatenation(concat, scope, tryResolveValue);
            case BangCallSyntax bangCall:
                return EvaluateBangCall(bangCall, scope, tryResolveValue);
            case FoldlSyntax foldl:
                return EvaluateFoldl(foldl, scope, tryResolveValue);
            case ForeachSyntax forEach:
                return EvaluateForeach(forEach, scope, tryResolveValue);
            case AnonymousClassInstantiationSyntax anonInst:
                return EvaluateAnonymousClassInstantiation(anonInst, scope, tryResolveValue);
            case FieldAccessSyntax fieldAccess:
                return EvaluateFieldAccess(fieldAccess, scope, tryResolveValue);
            case SubscriptSyntax subscript:
                return EvaluateSubscript(subscript, scope, tryResolveValue);
            case ClassInstantiationSyntax instantiation:
                return EvaluateClassInstantiation(instantiation, scope, tryResolveValue);
            default:
                return Failure(new InvalidOperationException("Unknown TableGen expression."));
        }
    }

    public Value Evaluate(
        ExpressionSyntax expression,
        Scope scope,
        TryResolveValue? tryResolveValue = null)
    {
        var result = TryEvaluate(expression, scope, tryResolveValue);
        if (!result.IsSuccess)
        {
            throw result.Error!;
        }

        return result.Value;
    }

    public static EvaluationResult<bool> TryIsTruthy(Value value)
    {
        return value switch
        {
            IntegerValue integer => EvaluationResult<bool>.Success(integer.Value != 0),
            BitValue bit => EvaluationResult<bool>.Success(bit.Value),
            _ => EvaluationResult<bool>.Failure(new InvalidOperationException($"Expected a boolean-like condition, got {value.GetType().Name}.")),
        };
    }

    public static bool IsTruthy(Value value)
    {
        var result = TryIsTruthy(value);
        if (!result.IsSuccess)
        {
            throw result.Error!;
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
                        return EvaluationResult<string>.Failure(itemString.Error!);
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
                return EvaluationResult<string>.Failure(new InvalidOperationException($"Cannot convert {value.GetType().Name} to string for concatenation."));
        }
    }

    public static string ValueToString(Value value)
    {
        var result = TryValueToString(value);
        if (!result.IsSuccess)
        {
            throw result.Error!;
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
                return Failure(new InvalidOperationException($"Expected an integer value for '{typeName}'."));
            case "string" when value is not StringValue:
            {
                var stringResult = TryValueToString(value);
                return stringResult.IsSuccess
                    ? Success(new StringValue(stringResult.Value))
                    : Failure(stringResult.Error!);
            }
            case "code":
                return Success(value);
            case "bit" when value is IntegerValue integer:
                return Success(new BitValue(integer.Value != 0));
            case "bit" when value is BitValue:
                return Success(value);
            case "bit":
                return Failure(new InvalidOperationException($"Expected a bit value for '{typeName}'."));
            case "dag" when value is not DagValue:
                return Failure(new InvalidOperationException($"Expected a dag value for '{typeName}'."));
            default:
                return Success(value);
        }
    }

    public static Value CoerceValue(string typeName, Value value)
    {
        var result = TryCoerceValue(typeName, value);
        if (!result.IsSuccess)
        {
            throw result.Error!;
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
            throw result.Error!;
        }

        return result.Value;
    }

    private EvaluationResult<Value> TryEvaluateList(
        ListSyntax list,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var items = new List<Value>(list.Items.Count);
        foreach (var item in list.Items)
        {
            var itemResult = TryEvaluate(item, scope, tryResolveValue);
            if (!itemResult.IsSuccess)
            {
                return Failure(itemResult.Error!);
            }

            items.Add(itemResult.Value);
        }

        return Success(new ListValue(items));
    }

    private EvaluationResult<Value> TryEvaluateDag(
        DagSyntax dag,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var arguments = new List<DagArgumentValue>(dag.Arguments.Count);
        foreach (var argument in dag.Arguments)
        {
            var valueResult = TryEvaluate(argument.Value, scope, tryResolveValue);
            if (!valueResult.IsSuccess)
            {
                return Failure(valueResult.Error!);
            }

            arguments.Add(new DagArgumentValue(valueResult.Value, argument.Name));
        }

        return Success(new DagValue(dag.OperatorName, arguments));
    }

    private EvaluationResult<Value> EvaluateConcatenation(
        ConcatSyntax concat,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var left = TryEvaluate(concat.Left, scope, tryResolveValue);
        if (!left.IsSuccess)
        {
            return Failure(left.Error!);
        }

        var right = TryEvaluate(concat.Right, scope, tryResolveValue);
        if (!right.IsSuccess)
        {
            return Failure(right.Error!);
        }

        var leftString = TryValueToString(left.Value);
        if (!leftString.IsSuccess)
        {
            return Failure(leftString.Error!);
        }

        var rightString = TryValueToString(right.Value);
        if (!rightString.IsSuccess)
        {
            return Failure(rightString.Error!);
        }

        return Success(new StringValue(leftString.Value + rightString.Value));
    }

    private EvaluationResult<Value> EvaluateBangCall(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        switch (bangCall.OperatorName)
        {
            case "if":
            {
                var cond = TryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
                if (!cond.IsSuccess)
                {
                    return Failure(cond.Error!);
                }

                var truthy = TryIsTruthy(cond.Value);
                if (!truthy.IsSuccess)
                {
                    return Failure(truthy.Error!);
                }

                return TryEvaluate(truthy.Value ? bangCall.Arguments[1] : bangCall.Arguments[2], scope, tryResolveValue);
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
                var val = TryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
                if (!val.IsSuccess)
                {
                    return Failure(val.Error!);
                }

                var truthy = TryIsTruthy(val.Value);
                return truthy.IsSuccess
                    ? Success(new IntegerValue(truthy.Value ? 0 : 1))
                    : Failure(truthy.Error!);
            }
            case "size":
            {
                var val = TryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
                if (!val.IsSuccess)
                {
                    return Failure(val.Error!);
                }

                return val.Value switch
                {
                    StringValue str => Success(new IntegerValue(str.Value.Length)),
                    ListValue list => Success(new IntegerValue(list.Items.Count)),
                    _ => Failure(new InvalidOperationException($"!size requires a string or list argument, got {val.Value.GetType().Name}.")),
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
                return TryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
            case "isa":
            {
                var val = TryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
                return val.IsSuccess
                    ? Success(new IntegerValue(IsValueOfType(val.Value, bangCall.TypeArgument) ? 1 : 0))
                    : Failure(val.Error!);
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
                return Failure(new InvalidOperationException($"Unknown bang operator '!{bangCall.OperatorName}'."));
        }
    }

    private EvaluationResult<Value> EvaluateFoldl(
        FoldlSyntax foldl,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var accValue = TryEvaluate(foldl.Init, scope, tryResolveValue);
        if (!accValue.IsSuccess)
        {
            return Failure(accValue.Error!);
        }

        var listValue = TryEvaluate(foldl.List, scope, tryResolveValue);
        if (!listValue.IsSuccess)
        {
            return Failure(listValue.Error!);
        }

        if (listValue.Value is not ListValue list)
        {
            return Failure(new InvalidCastException());
        }

        var current = accValue.Value;
        foreach (var item in list.Items)
        {
            var innerScope = scope.With(foldl.AccVar, current).With(foldl.CurVar, item);
            var body = TryEvaluate(foldl.Body, innerScope, tryResolveValue);
            if (!body.IsSuccess)
            {
                return Failure(body.Error!);
            }

            current = body.Value;
        }

        return Success(current);
    }

    private EvaluationResult<Value> EvaluateClassInstantiation(
        ClassInstantiationSyntax instantiation,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        if (!context.Classes.TryGetValue(instantiation.ClassName, out var classSyntax))
        {
            return Failure(new KeyNotFoundException($"Unknown TableGen class '{instantiation.ClassName}'."));
        }

        var fields = instantiateClass(classSyntax, instantiation.Arguments, scope, tryResolveValue);
        if (!fields.IsSuccess)
        {
            return Failure(fields.Error!);
        }

        return fields.Value.TryGetValue(instantiation.FieldName, out var fieldValue)
            ? Success(fieldValue)
            : Failure(new KeyNotFoundException($"Class '{instantiation.ClassName}' has no field '{instantiation.FieldName}'."));
    }

    private EvaluationResult<Value> EvaluateAnonymousClassInstantiation(
        AnonymousClassInstantiationSyntax inst,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        if (!context.Classes.TryGetValue(inst.ClassName, out var classSyntax))
        {
            return Failure(new KeyNotFoundException($"Unknown TableGen class '{inst.ClassName}'."));
        }

        var fields = instantiateClass(classSyntax, inst.Arguments, scope, tryResolveValue);
        return fields.IsSuccess
            ? Success(new AnonymousRecordValue(inst.ClassName, fields.Value))
            : Failure(fields.Error!);
    }

    private EvaluationResult<Value> EvaluateFieldAccess(
        FieldAccessSyntax fieldAccess,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var obj = TryEvaluate(fieldAccess.Object, scope, tryResolveValue);
        if (!obj.IsSuccess)
        {
            return Failure(obj.Error!);
        }

        if (obj.Value is AnonymousRecordValue rec)
        {
            return Success(rec.Fields.TryGetValue(fieldAccess.FieldName, out var fv) ? fv : new UnsetValue());
        }

        if (obj.Value is RecordReferenceValue recRef && context.DefinitionsByName.TryGetValue(recRef.RecordName, out var defSyntax))
        {
            var record = buildDefinition(defSyntax);
            if (!record.IsSuccess)
            {
                return Failure(record.Error!);
            }

            return Success(record.Value.Fields.TryGetValue(fieldAccess.FieldName, out var fieldVal) ? fieldVal : new UnsetValue());
        }

        return Success(new UnsetValue());
    }

    private EvaluationResult<Value> EvaluateSubscript(
        SubscriptSyntax subscript,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var target = TryEvaluate(subscript.Target, scope, tryResolveValue);
        if (!target.IsSuccess)
        {
            return Failure(target.Error!);
        }

        var index = TryEvaluateInteger(subscript.Index, scope, tryResolveValue, "subscript");
        if (!index.IsSuccess)
        {
            return Failure(index.Error!);
        }

        switch (target.Value)
        {
            case ListValue list:
            {
                var normalized = TryNormalizeIndex(index.Value, list.Items.Count, "list subscript");
                return normalized.IsSuccess
                    ? Success(list.Items[normalized.Value])
                    : Failure(normalized.Error!);
            }
            case StringValue str:
            {
                var normalized = TryNormalizeIndex(index.Value, str.Value.Length, "string subscript");
                return normalized.IsSuccess
                    ? Success(new StringValue(str.Value[normalized.Value].ToString()))
                    : Failure(normalized.Error!);
            }
            default:
                return Failure(new InvalidOperationException($"Cannot subscript {target.Value.GetType().Name}."));
        }
    }

    private EvaluationResult<Value> EvaluateForeach(
        ForeachSyntax forEach,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var listValue = TryEvaluate(forEach.List, scope, tryResolveValue);
        if (!listValue.IsSuccess)
        {
            return Failure(listValue.Error!);
        }

        if (listValue.Value is not ListValue list)
        {
            return Failure(new InvalidCastException());
        }

        var results = new List<Value>(list.Items.Count);
        foreach (var item in list.Items)
        {
            var innerScope = scope.With(forEach.VarName, item);
            var body = TryEvaluate(forEach.Body, innerScope, tryResolveValue);
            if (!body.IsSuccess)
            {
                return Failure(body.Error!);
            }

            results.Add(body.Value);
        }

        return Success(new ListValue(results));
    }

    private EvaluationResult<Value> TryEvaluateIntegerComparison(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue,
        Func<int, int, bool> predicate)
    {
        var a = TryEvaluateInteger(bangCall.Arguments[0], scope, tryResolveValue, $"!{bangCall.OperatorName}");
        if (!a.IsSuccess)
        {
            return Failure(a.Error!);
        }

        var b = TryEvaluateInteger(bangCall.Arguments[1], scope, tryResolveValue, $"!{bangCall.OperatorName}");
        return b.IsSuccess
            ? Success(new IntegerValue(predicate(a.Value, b.Value) ? 1 : 0))
            : Failure(b.Error!);
    }

    private EvaluationResult<Value> TryEvaluateEquality(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue,
        bool expectedEqual)
    {
        var a = TryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!a.IsSuccess)
        {
            return Failure(a.Error!);
        }

        var b = TryEvaluate(bangCall.Arguments[1], scope, tryResolveValue);
        if (!b.IsSuccess)
        {
            return Failure(b.Error!);
        }

        var isEqual = ValuesEqual(a.Value, b.Value);
        return Success(new IntegerValue(isEqual == expectedEqual ? 1 : 0));
    }

    private EvaluationResult<Value> TryEvaluateIntegerBinary(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue,
        string contextName,
        Func<int, int, int> operation)
    {
        var a = TryEvaluateInteger(bangCall.Arguments[0], scope, tryResolveValue, contextName);
        if (!a.IsSuccess)
        {
            return Failure(a.Error!);
        }

        var b = TryEvaluateInteger(bangCall.Arguments[1], scope, tryResolveValue, contextName);
        return b.IsSuccess
            ? Success(new IntegerValue(operation(a.Value, b.Value)))
            : Failure(b.Error!);
    }

    private EvaluationResult<Value> TryEvaluateUnaryString(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue,
        string contextName,
        Func<string, string> operation)
    {
        var text = TryEvaluateString(bangCall.Arguments[0], scope, tryResolveValue, contextName);
        return text.IsSuccess
            ? Success(new StringValue(operation(text.Value)))
            : Failure(text.Error!);
    }

    private EvaluationResult<Value> TryEvaluateSubstr(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var str = TryEvaluateString(bangCall.Arguments[0], scope, tryResolveValue, "!substr");
        if (!str.IsSuccess)
        {
            return Failure(str.Error!);
        }

        var start = TryEvaluateInteger(bangCall.Arguments[1], scope, tryResolveValue, "!substr");
        if (!start.IsSuccess)
        {
            return Failure(start.Error!);
        }

        var clampedStart = Math.Max(0, Math.Min(start.Value, str.Value.Length));
        if (bangCall.Arguments.Count >= 3)
        {
            var len = TryEvaluateInteger(bangCall.Arguments[2], scope, tryResolveValue, "!substr");
            if (!len.IsSuccess)
            {
                return Failure(len.Error!);
            }

            var clampedLen = Math.Max(0, Math.Min(len.Value, str.Value.Length - clampedStart));
            return Success(new StringValue(str.Value.Substring(clampedStart, clampedLen)));
        }

        return Success(new StringValue(str.Value.Substring(clampedStart)));
    }

    private EvaluationResult<Value> TryEvaluateFind(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var str = TryEvaluateString(bangCall.Arguments[0], scope, tryResolveValue, "!find");
        if (!str.IsSuccess)
        {
            return Failure(str.Error!);
        }

        var sub = TryEvaluateString(bangCall.Arguments[1], scope, tryResolveValue, "!find");
        if (!sub.IsSuccess)
        {
            return Failure(sub.Error!);
        }

        var startIndex = bangCall.Arguments.Count >= 3
            ? TryEvaluateInteger(bangCall.Arguments[2], scope, tryResolveValue, "!find")
            : EvaluationResult<int>.Success(0);
        if (!startIndex.IsSuccess)
        {
            return Failure(startIndex.Error!);
        }

        return Success(new IntegerValue(str.Value.IndexOf(sub.Value, startIndex.Value, StringComparison.Ordinal)));
    }

    private EvaluationResult<Value> TryEvaluateRange(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var start = TryEvaluateInteger(bangCall.Arguments[0], scope, tryResolveValue, "!range");
        if (!start.IsSuccess)
        {
            return Failure(start.Error!);
        }

        var end = TryEvaluateInteger(bangCall.Arguments[1], scope, tryResolveValue, "!range");
        if (!end.IsSuccess)
        {
            return Failure(end.Error!);
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
        TryResolveValue? tryResolveValue)
    {
        var a = TryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!a.IsSuccess)
        {
            return Failure(a.Error!);
        }

        var b = TryEvaluate(bangCall.Arguments[1], scope, tryResolveValue);
        if (!b.IsSuccess)
        {
            return Failure(b.Error!);
        }

        if (a.Value is not ListValue left || b.Value is not ListValue right)
        {
            return Failure(new InvalidCastException());
        }

        var items = new List<Value>(left.Items.Count + right.Items.Count);
        items.AddRange(left.Items);
        items.AddRange(right.Items);
        return Success(new ListValue(items));
    }

    private EvaluationResult<Value> TryEvaluateStrConcat(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var parts = new List<string>(bangCall.Arguments.Count);
        foreach (var arg in bangCall.Arguments)
        {
            var value = TryEvaluate(arg, scope, tryResolveValue);
            if (!value.IsSuccess)
            {
                return Failure(value.Error!);
            }

            var text = TryToString(value.Value, "!strconcat");
            if (!text.IsSuccess)
            {
                return Failure(text.Error!);
            }

            parts.Add(text.Value);
        }

        return Success(new StringValue(string.Concat(parts)));
    }

    private EvaluationResult<Value> TryEvaluateCond(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        for (var i = 0; i + 1 < bangCall.Arguments.Count; i += 2)
        {
            var condition = TryEvaluate(bangCall.Arguments[i], scope, tryResolveValue);
            if (!condition.IsSuccess)
            {
                return Failure(condition.Error!);
            }

            var truthy = TryIsTruthy(condition.Value);
            if (!truthy.IsSuccess)
            {
                return Failure(truthy.Error!);
            }

            if (truthy.Value)
            {
                return TryEvaluate(bangCall.Arguments[i + 1], scope, tryResolveValue);
            }
        }

        return Failure(new InvalidOperationException("!cond requires at least one true condition."));
    }

    private EvaluationResult<Value> TryEvaluateInterleave(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var listVal = TryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!listVal.IsSuccess)
        {
            return Failure(listVal.Error!);
        }

        var sep = TryEvaluateString(bangCall.Arguments[1], scope, tryResolveValue, "!interleave");
        if (!sep.IsSuccess)
        {
            return Failure(sep.Error!);
        }

        if (listVal.Value is not ListValue list)
        {
            return Failure(new InvalidCastException());
        }

        var items = new List<string>(list.Items.Count);
        foreach (var item in list.Items)
        {
            var itemString = TryValueToString(item);
            if (!itemString.IsSuccess)
            {
                return Failure(itemString.Error!);
            }

            items.Add(itemString.Value);
        }

        return Success(new StringValue(string.Join(sep.Value, items)));
    }

    private EvaluationResult<Value> TryEvaluateSubst(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var from = TryEvaluateString(bangCall.Arguments[0], scope, tryResolveValue, "!subst");
        if (!from.IsSuccess)
        {
            return Failure(from.Error!);
        }

        var to = TryEvaluateString(bangCall.Arguments[1], scope, tryResolveValue, "!subst");
        if (!to.IsSuccess)
        {
            return Failure(to.Error!);
        }

        var text = TryEvaluateString(bangCall.Arguments[2], scope, tryResolveValue, "!subst");
        return text.IsSuccess
            ? Success(new StringValue(text.Value.Replace(from.Value, to.Value)))
            : Failure(text.Error!);
    }

    private EvaluationResult<Value> TryEvaluateHead(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var list = TryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!list.IsSuccess)
        {
            return Failure(list.Error!);
        }

        if (list.Value is not ListValue values)
        {
            return Failure(new InvalidCastException());
        }

        return values.Items.Count == 0
            ? Failure(new InvalidOperationException("!head requires a non-empty list."))
            : Success(values.Items[0]);
    }

    private EvaluationResult<Value> TryEvaluateTail(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var list = TryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!list.IsSuccess)
        {
            return Failure(list.Error!);
        }

        if (list.Value is not ListValue values)
        {
            return Failure(new InvalidCastException());
        }

        return values.Items.Count == 0
            ? Failure(new InvalidOperationException("!tail requires a non-empty list."))
            : Success(new ListValue(values.Items.Skip(1).ToList()));
    }

    private EvaluationResult<Value> TryEvaluateEmpty(
        BangCallSyntax bangCall,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var val = TryEvaluate(bangCall.Arguments[0], scope, tryResolveValue);
        if (!val.IsSuccess)
        {
            return Failure(val.Error!);
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
        TryResolveValue? tryResolveValue)
    {
        var variable = ((IdentifierSyntax)bangCall.Arguments[0]).Name;
        var listValue = TryEvaluate(bangCall.Arguments[1], scope, tryResolveValue);
        if (!listValue.IsSuccess)
        {
            return Failure(listValue.Error!);
        }

        if (listValue.Value is not ListValue list)
        {
            return Failure(new InvalidCastException());
        }

        var results = new List<Value>();
        foreach (var item in list.Items)
        {
            var innerScope = scope.With(variable, item);
            var condition = TryEvaluate(bangCall.Arguments[2], innerScope, tryResolveValue);
            if (!condition.IsSuccess)
            {
                return Failure(condition.Error!);
            }

            var truthy = TryIsTruthy(condition.Value);
            if (!truthy.IsSuccess)
            {
                return Failure(truthy.Error!);
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
        TryResolveValue? tryResolveValue,
        string contextName)
    {
        var value = TryEvaluate(expression, scope, tryResolveValue);
        return !value.IsSuccess
            ? EvaluationResult<int>.Failure(value.Error!)
            : TryToInteger(value.Value, contextName);
    }

    private EvaluationResult<string> TryEvaluateString(
        ExpressionSyntax expression,
        Scope scope,
        TryResolveValue? tryResolveValue,
        string contextName)
    {
        var value = TryEvaluate(expression, scope, tryResolveValue);
        return !value.IsSuccess
            ? EvaluationResult<string>.Failure(value.Error!)
            : TryToString(value.Value, contextName);
    }

    private bool IsValueOfType(Value value, string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        var nonNullTypeName = typeName!;
        return value switch
        {
            AnonymousRecordValue record => ClassIsA(record.ClassName, nonNullTypeName),
            RecordReferenceValue recordReference => context.DefinitionsByName.TryGetValue(recordReference.RecordName, out var definition)
                && definition.Bases.Any(@base => ClassIsA(@base.Name, nonNullTypeName)),
            _ => false,
        };
    }

    private bool ClassIsA(string className, string typeName)
    {
        if (className == typeName)
        {
            return true;
        }

        return context.Classes.TryGetValue(className, out var classSyntax)
            && classSyntax.Bases.Any(@base => ClassIsA(@base.Name, typeName));
    }

    private EvaluationResult<Value> ResolveIdentifier(
        string name,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        if (scope.TryGetValue(name, out var value))
        {
            return Success(value);
        }

        if (tryResolveValue != null)
        {
            var resolved = tryResolveValue(name);
            if (resolved.IsSuccess)
            {
                return resolved;
            }
        }

        if (context.DefvarValues.TryGetValue(name, out value))
        {
            return Success(value);
        }

        if (name == "true")
        {
            return Success(new BitValue(true));
        }

        if (name == "false")
        {
            return Success(new BitValue(false));
        }

        if (context.DefinitionsByName.ContainsKey(name))
        {
            return Success(new RecordReferenceValue(name));
        }

        return Success(new SymbolReferenceValue(name));
    }

    private static EvaluationResult<int> TryToInteger(Value value, string contextName)
    {
        return value is IntegerValue integer
            ? EvaluationResult<int>.Success(integer.Value)
            : EvaluationResult<int>.Failure(new InvalidOperationException($"{contextName} requires an integer argument, got {value.GetType().Name}."));
    }

    private static EvaluationResult<string> TryToString(Value value, string contextName)
    {
        return value is StringValue str
            ? EvaluationResult<string>.Success(str.Value)
            : EvaluationResult<string>.Failure(new InvalidOperationException($"{contextName} requires a string argument, got {value.GetType().Name}."));
    }

    private static EvaluationResult<int> TryNormalizeIndex(int index, int length, string contextName)
    {
        var normalized = index < 0 ? length + index : index;
        return normalized < 0 || normalized >= length
            ? EvaluationResult<int>.Failure(new InvalidOperationException($"{contextName} index {index} is out of range."))
            : EvaluationResult<int>.Success(normalized);
    }

    private static bool ValuesEqual(Value a, Value b)
    {
        return (a, b) switch
        {
            (IntegerValue ia, IntegerValue ib) => ia.Value == ib.Value,
            (StringValue sa, StringValue sb) => sa.Value == sb.Value,
            (BitValue ba, BitValue bb) => ba.Value == bb.Value,
            _ => false,
        };
    }

    private static EvaluationResult<Value> Success(Value value)
    {
        return EvaluationResult<Value>.Success(value);
    }

    private static EvaluationResult<Value> Failure(Exception error)
    {
        return EvaluationResult<Value>.Failure(error);
    }
}
