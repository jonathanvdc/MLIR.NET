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
    private readonly Func<ClassSyntax, IReadOnlyList<ExpressionSyntax>, IReadOnlyDictionary<string, Value>, TryResolveValue?, Dictionary<string, Value>> instantiateClass;
    private readonly Func<DefSyntax, Record> buildDefinition;

    internal delegate bool TryResolveValue(string name, out Value value);

    public ExpressionEvaluator(
        EvaluationContext context,
        Func<ClassSyntax, IReadOnlyList<ExpressionSyntax>, IReadOnlyDictionary<string, Value>, TryResolveValue?, Dictionary<string, Value>> instantiateClass,
        Func<DefSyntax, Record> buildDefinition)
    {
        this.context = context;
        this.instantiateClass = instantiateClass;
        this.buildDefinition = buildDefinition;
    }

    public Value Evaluate(ExpressionSyntax expression, IReadOnlyDictionary<string, Value> scope, TryResolveValue? tryResolveValue = null)
    {
        return expression switch
        {
            IntegerSyntax integer => new IntegerValue(integer.Value),
            StringSyntax str => new StringValue(str.Value),
            UnsetSyntax => new UnsetValue(),
            IdentifierSyntax identifier => ResolveIdentifier(identifier.Name, scope, tryResolveValue),
            ListSyntax list => new ListValue(list.Items.Select(item => Evaluate(item, scope, tryResolveValue)).ToList()),
            DagSyntax dag => new DagValue(dag.OperatorName, dag.Arguments.Select(argument => new DagArgumentValue(Evaluate(argument.Value, scope, tryResolveValue), argument.Name)).ToList()),
            ConcatSyntax concat => EvaluateConcatenation(concat, scope, tryResolveValue),
            BangCallSyntax bangCall => EvaluateBangCall(bangCall, scope, tryResolveValue),
            FoldlSyntax foldl => EvaluateFoldl(foldl, scope, tryResolveValue),
            ForeachSyntax forEach => EvaluateForeach(forEach, scope, tryResolveValue),
            AnonymousClassInstantiationSyntax anonInst => EvaluateAnonymousClassInstantiation(anonInst, scope, tryResolveValue),
            FieldAccessSyntax fieldAccess => EvaluateFieldAccess(fieldAccess, scope, tryResolveValue),
            SubscriptSyntax subscript => EvaluateSubscript(subscript, scope, tryResolveValue),
            ClassInstantiationSyntax instantiation => EvaluateClassInstantiation(instantiation, scope, tryResolveValue),
            _ => throw new InvalidOperationException("Unknown TableGen expression."),
        };
    }

    public static bool IsTruthy(Value value) => value switch
    {
        IntegerValue integer => integer.Value != 0,
        BitValue bit => bit.Value,
        _ => throw new InvalidOperationException($"Expected a boolean-like condition, got {value.GetType().Name}."),
    };

    public static string ValueToString(Value value) => value switch
    {
        StringValue str => str.Value,
        IntegerValue integer => integer.Value.ToString(CultureInfo.InvariantCulture),
        BitValue bit => bit.Value ? "1" : "0",
        ListValue list => string.Concat(list.Items.Select(ValueToString)),
        SymbolReferenceValue symbol => symbol.SymbolName,
        RecordReferenceValue record => record.RecordName,
        UnsetValue => string.Empty,
        AnonymousRecordValue rec => rec.ClassName,
        _ => throw new InvalidOperationException($"Cannot convert {value.GetType().Name} to string for concatenation."),
    };

    public static Value CoerceValue(string typeName, Value value)
    {
        if (value is UnsetValue)
        {
            return value;
        }

        return typeName switch
        {
            "int" when value is not IntegerValue => throw new InvalidOperationException($"Expected an integer value for '{typeName}'."),
            "string" when value is not StringValue => new StringValue(ValueToString(value)),
            "code" => value,
            "bit" when value is IntegerValue integer => new BitValue(integer.Value != 0),
            "bit" when value is BitValue => value,
            "bit" => throw new InvalidOperationException($"Expected a bit value for '{typeName}'."),
            "dag" when value is not DagValue => throw new InvalidOperationException($"Expected a dag value for '{typeName}'."),
            _ => value,
        };
    }

    public static Value CoerceExistingValue(Value existingValue, Value replacementValue)
    {
        return existingValue switch
        {
            BitValue when replacementValue is IntegerValue integer => new BitValue(integer.Value != 0),
            BitValue when replacementValue is BitValue => replacementValue,
            _ => replacementValue,
        };
    }

    private Value EvaluateConcatenation(ConcatSyntax concat, IReadOnlyDictionary<string, Value> scope, TryResolveValue? tryResolveValue)
    {
        var left = Evaluate(concat.Left, scope, tryResolveValue);
        var right = Evaluate(concat.Right, scope, tryResolveValue);
        return new StringValue(ValueToString(left) + ValueToString(right));
    }

    private Value EvaluateBangCall(BangCallSyntax bangCall, IReadOnlyDictionary<string, Value> scope, TryResolveValue? tryResolveValue)
    {
        switch (bangCall.OperatorName)
        {
            case "if":
            {
                var cond = Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
                return IsTruthy(cond)
                    ? Evaluate(bangCall.Arguments[1], scope, tryResolveValue)
                    : Evaluate(bangCall.Arguments[2], scope, tryResolveValue);
            }

            case "gt":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!gt");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!gt");
                return new IntegerValue(a > b ? 1 : 0);
            }

            case "ge":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!ge");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!ge");
                return new IntegerValue(a >= b ? 1 : 0);
            }

            case "lt":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!lt");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!lt");
                return new IntegerValue(a < b ? 1 : 0);
            }

            case "le":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!le");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!le");
                return new IntegerValue(a <= b ? 1 : 0);
            }

            case "eq":
            {
                var a = Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
                var b = Evaluate(bangCall.Arguments[1], scope, tryResolveValue);
                return new IntegerValue(ValuesEqual(a, b) ? 1 : 0);
            }

            case "ne":
            {
                var a = Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
                var b = Evaluate(bangCall.Arguments[1], scope, tryResolveValue);
                return new IntegerValue(ValuesEqual(a, b) ? 0 : 1);
            }

            case "add":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!add");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!add");
                return new IntegerValue(a + b);
            }

            case "sub":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!sub");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!sub");
                return new IntegerValue(a - b);
            }

            case "mul":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!mul");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!mul");
                return new IntegerValue(a * b);
            }

            case "and":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!and");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!and");
                return new IntegerValue(a & b);
            }

            case "or":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!or");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!or");
                return new IntegerValue(a | b);
            }

            case "not":
            {
                var val = Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
                return new IntegerValue(IsTruthy(val) ? 0 : 1);
            }

            case "size":
            {
                var val = Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
                return val switch
                {
                    StringValue str => new IntegerValue(str.Value.Length),
                    ListValue list => new IntegerValue(list.Items.Count),
                    _ => throw new InvalidOperationException($"!size requires a string or list argument, got {val.GetType().Name}."),
                };
            }

            case "toupper":
            {
                var str = ToString(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!toupper");
                return new StringValue(str.ToUpperInvariant());
            }

            case "tolower":
            {
                var str = ToString(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!tolower");
                return new StringValue(str.ToLowerInvariant());
            }

            case "substr":
            {
                var str = ToString(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!substr");
                var start = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!substr");
                var clampedStart = Math.Max(0, Math.Min(start, str.Length));
                if (bangCall.Arguments.Count >= 3)
                {
                    var len = ToInteger(Evaluate(bangCall.Arguments[2], scope, tryResolveValue), "!substr");
                    var clampedLen = Math.Max(0, Math.Min(len, str.Length - clampedStart));
                    return new StringValue(str.Substring(clampedStart, clampedLen));
                }

                return new StringValue(str.Substring(clampedStart));
            }

            case "find":
            {
                var str = ToString(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!find");
                var sub = ToString(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!find");
                var startIndex = bangCall.Arguments.Count >= 3
                    ? ToInteger(Evaluate(bangCall.Arguments[2], scope, tryResolveValue), "!find")
                    : 0;
                return new IntegerValue(str.IndexOf(sub, startIndex, StringComparison.Ordinal));
            }

            case "range":
            {
                var start = ToInteger(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!range");
                var end = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!range");
                var items = new List<Value>(Math.Max(0, end - start));
                for (var i = start; i < end; i++)
                {
                    items.Add(new IntegerValue(i));
                }

                return new ListValue(items);
            }

            case "listconcat":
            {
                var a = (ListValue)Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
                var b = (ListValue)Evaluate(bangCall.Arguments[1], scope, tryResolveValue);
                var items = new List<Value>(a.Items.Count + b.Items.Count);
                items.AddRange(a.Items);
                items.AddRange(b.Items);
                return new ListValue(items);
            }

            case "strconcat":
            {
                var result = string.Concat(bangCall.Arguments.Select(arg => ToString(Evaluate(arg, scope, tryResolveValue), "!strconcat")));
                return new StringValue(result);
            }

            case "shl":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!shl");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!shl");
                return new IntegerValue(a << b);
            }

            case "cast":
            {
                return Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
            }

            case "isa":
            {
                var val = Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
                return new IntegerValue(IsValueOfType(val, bangCall.TypeArgument) ? 1 : 0);
            }

            case "cond":
            {
                for (var i = 0; i + 1 < bangCall.Arguments.Count; i += 2)
                {
                    if (IsTruthy(Evaluate(bangCall.Arguments[i], scope, tryResolveValue)))
                    {
                        return Evaluate(bangCall.Arguments[i + 1], scope, tryResolveValue);
                    }
                }

                throw new InvalidOperationException("!cond requires at least one true condition.");
            }

            case "interleave":
            {
                var listVal = (ListValue)Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
                var sep = ToString(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!interleave");
                return new StringValue(string.Join(sep, listVal.Items.Select(item => ValueToString(item))));
            }

            case "subst":
            {
                var from = ToString(Evaluate(bangCall.Arguments[0], scope, tryResolveValue), "!subst");
                var to = ToString(Evaluate(bangCall.Arguments[1], scope, tryResolveValue), "!subst");
                var text = ToString(Evaluate(bangCall.Arguments[2], scope, tryResolveValue), "!subst");
                return new StringValue(text.Replace(from, to));
            }

            case "head":
            {
                var list = (ListValue)Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
                if (list.Items.Count == 0)
                {
                    throw new InvalidOperationException("!head requires a non-empty list.");
                }

                return list.Items[0];
            }

            case "tail":
            {
                var list = (ListValue)Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
                if (list.Items.Count == 0)
                {
                    throw new InvalidOperationException("!tail requires a non-empty list.");
                }

                return new ListValue(list.Items.Skip(1).ToList());
            }

            case "empty":
            {
                var val = Evaluate(bangCall.Arguments[0], scope, tryResolveValue);
                return val switch
                {
                    StringValue str => new IntegerValue(string.IsNullOrEmpty(str.Value) ? 1 : 0),
                    ListValue list => new IntegerValue(list.Items.Count == 0 ? 1 : 0),
                    UnsetValue => new IntegerValue(1),
                    _ => new IntegerValue(0),
                };
            }

            case "filter":
            {
                var variable = ((IdentifierSyntax)bangCall.Arguments[0]).Name;
                var listValue = (ListValue)Evaluate(bangCall.Arguments[1], scope, tryResolveValue);
                var results = new List<Value>();
                foreach (var item in listValue.Items)
                {
                    var innerScope = scope.ToDictionary(static kv => kv.Key, static kv => kv.Value);
                    innerScope[variable] = item;
                    if (IsTruthy(Evaluate(bangCall.Arguments[2], innerScope, tryResolveValue)))
                    {
                        results.Add(item);
                    }
                }

                return new ListValue(results);
            }

            default:
                throw new InvalidOperationException($"Unknown bang operator '!{bangCall.OperatorName}'.");
        }
    }

    private Value EvaluateFoldl(FoldlSyntax foldl, IReadOnlyDictionary<string, Value> scope, TryResolveValue? tryResolveValue)
    {
        var accValue = Evaluate(foldl.Init, scope, tryResolveValue);
        var listValue = (ListValue)Evaluate(foldl.List, scope, tryResolveValue);
        foreach (var item in listValue.Items)
        {
            var innerScope = scope.ToDictionary(static kv => kv.Key, static kv => kv.Value);
            innerScope[foldl.AccVar] = accValue;
            innerScope[foldl.CurVar] = item;
            accValue = Evaluate(foldl.Body, innerScope, tryResolveValue);
        }

        return accValue;
    }

    private Value EvaluateClassInstantiation(
        ClassInstantiationSyntax instantiation,
        IReadOnlyDictionary<string, Value> scope,
        TryResolveValue? tryResolveValue)
    {
        if (!context.Classes.TryGetValue(instantiation.ClassName, out var classSyntax))
        {
            throw new KeyNotFoundException($"Unknown TableGen class '{instantiation.ClassName}'.");
        }

        var fields = instantiateClass(classSyntax, instantiation.Arguments, scope, tryResolveValue);
        if (!fields.TryGetValue(instantiation.FieldName, out var fieldValue))
        {
            throw new KeyNotFoundException($"Class '{instantiation.ClassName}' has no field '{instantiation.FieldName}'.");
        }

        return fieldValue;
    }

    private Value EvaluateAnonymousClassInstantiation(
        AnonymousClassInstantiationSyntax inst,
        IReadOnlyDictionary<string, Value> scope,
        TryResolveValue? tryResolveValue)
    {
        if (!context.Classes.TryGetValue(inst.ClassName, out var classSyntax))
        {
            throw new KeyNotFoundException($"Unknown TableGen class '{inst.ClassName}'.");
        }

        var fields = instantiateClass(classSyntax, inst.Arguments, scope, tryResolveValue);
        return new AnonymousRecordValue(inst.ClassName, fields);
    }

    private Value EvaluateFieldAccess(FieldAccessSyntax fieldAccess, IReadOnlyDictionary<string, Value> scope, TryResolveValue? tryResolveValue)
    {
        var obj = Evaluate(fieldAccess.Object, scope, tryResolveValue);

        if (obj is AnonymousRecordValue rec)
        {
            return rec.Fields.TryGetValue(fieldAccess.FieldName, out var fv) ? fv : new UnsetValue();
        }

        if (obj is RecordReferenceValue recRef && context.DefinitionsByName.TryGetValue(recRef.RecordName, out var defSyntax))
        {
            var record = buildDefinition(defSyntax);
            return record.Fields.TryGetValue(fieldAccess.FieldName, out var fieldVal) ? fieldVal : new UnsetValue();
        }

        return new UnsetValue();
    }

    private Value EvaluateSubscript(SubscriptSyntax subscript, IReadOnlyDictionary<string, Value> scope, TryResolveValue? tryResolveValue)
    {
        var target = Evaluate(subscript.Target, scope, tryResolveValue);
        var index = ToInteger(Evaluate(subscript.Index, scope, tryResolveValue), "subscript");
        return target switch
        {
            ListValue list => list.Items[NormalizeIndex(index, list.Items.Count, "list subscript")],
            StringValue str => new StringValue(str.Value[NormalizeIndex(index, str.Value.Length, "string subscript")].ToString()),
            _ => throw new InvalidOperationException($"Cannot subscript {target.GetType().Name}."),
        };
    }

    private Value EvaluateForeach(ForeachSyntax forEach, IReadOnlyDictionary<string, Value> scope, TryResolveValue? tryResolveValue)
    {
        var listValue = (ListValue)Evaluate(forEach.List, scope, tryResolveValue);
        var results = new List<Value>(listValue.Items.Count);
        foreach (var item in listValue.Items)
        {
            var innerScope = scope.ToDictionary(static kv => kv.Key, static kv => kv.Value);
            innerScope[forEach.VarName] = item;
            results.Add(Evaluate(forEach.Body, innerScope, tryResolveValue));
        }

        return new ListValue(results);
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

        if (!context.Classes.TryGetValue(className, out var classSyntax))
        {
            return false;
        }

        return classSyntax.Bases.Any(@base => ClassIsA(@base.Name, typeName));
    }

    private Value ResolveIdentifier(string name, IReadOnlyDictionary<string, Value> scope, TryResolveValue? tryResolveValue)
    {
        if (scope.TryGetValue(name, out var value))
        {
            return value;
        }

        if (tryResolveValue != null && tryResolveValue(name, out value))
        {
            return value;
        }

        if (context.DefvarValues.TryGetValue(name, out value))
        {
            return value;
        }

        if (name == "true")
        {
            return new BitValue(true);
        }

        if (name == "false")
        {
            return new BitValue(false);
        }

        if (context.DefinitionsByName.ContainsKey(name))
        {
            return new RecordReferenceValue(name);
        }

        return new SymbolReferenceValue(name);
    }

    private static int ToInteger(Value value, string context) => value switch
    {
        IntegerValue integer => integer.Value,
        _ => throw new InvalidOperationException($"{context} requires an integer argument, got {value.GetType().Name}."),
    };

    private static string ToString(Value value, string context) => value switch
    {
        StringValue str => str.Value,
        _ => throw new InvalidOperationException($"{context} requires a string argument, got {value.GetType().Name}."),
    };

    private static int NormalizeIndex(int index, int length, string context)
    {
        var normalized = index < 0 ? length + index : index;
        if (normalized < 0 || normalized >= length)
        {
            throw new InvalidOperationException($"{context} index {index} is out of range.");
        }

        return normalized;
    }

    private static bool ValuesEqual(Value a, Value b) => (a, b) switch
    {
        (IntegerValue ia, IntegerValue ib) => ia.Value == ib.Value,
        (StringValue sa, StringValue sb) => sa.Value == sb.Value,
        (BitValue ba, BitValue bb) => ba.Value == bb.Value,
        _ => false,
    };
}
