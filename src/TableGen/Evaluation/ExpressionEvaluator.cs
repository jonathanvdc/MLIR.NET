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
    private readonly Func<ClassSyntax, IReadOnlyList<ExpressionSyntax>, IReadOnlyDictionary<string, Value>, Dictionary<string, Value>> instantiateClass;
    private readonly Action<IReadOnlyList<BaseSyntax>, Dictionary<string, Value>, Dictionary<string, Value>> applyBases;
    private readonly Action<IReadOnlyList<BodyItemSyntax>, Dictionary<string, Value>, Dictionary<string, Value>> applyBody;

    public ExpressionEvaluator(
        EvaluationContext context,
        Func<ClassSyntax, IReadOnlyList<ExpressionSyntax>, IReadOnlyDictionary<string, Value>, Dictionary<string, Value>> instantiateClass,
        Action<IReadOnlyList<BaseSyntax>, Dictionary<string, Value>, Dictionary<string, Value>> applyBases,
        Action<IReadOnlyList<BodyItemSyntax>, Dictionary<string, Value>, Dictionary<string, Value>> applyBody)
    {
        this.context = context;
        this.instantiateClass = instantiateClass;
        this.applyBases = applyBases;
        this.applyBody = applyBody;
    }

    public Value Evaluate(ExpressionSyntax expression, IReadOnlyDictionary<string, Value> scope)
    {
        return expression switch
        {
            IntegerSyntax integer => new IntegerValue(integer.Value),
            StringSyntax str => new StringValue(str.Value),
            UnsetSyntax => new UnsetValue(),
            IdentifierSyntax identifier => ResolveIdentifier(identifier.Name, scope),
            ListSyntax list => new ListValue(list.Items.Select(item => Evaluate(item, scope)).ToList()),
            DagSyntax dag => new DagValue(dag.OperatorName, dag.Arguments.Select(argument => new DagArgumentValue(Evaluate(argument.Value, scope), argument.Name)).ToList()),
            ConcatSyntax concat => EvaluateConcatenation(concat, scope),
            BangCallSyntax bangCall => EvaluateBangCall(bangCall, scope),
            FoldlSyntax foldl => EvaluateFoldl(foldl, scope),
            ForeachSyntax forEach => EvaluateForeach(forEach, scope),
            AnonymousClassInstantiationSyntax anonInst => EvaluateAnonymousClassInstantiation(anonInst, scope),
            FieldAccessSyntax fieldAccess => EvaluateFieldAccess(fieldAccess, scope),
            SubscriptSyntax subscript => EvaluateSubscript(subscript, scope),
            ClassInstantiationSyntax instantiation => EvaluateClassInstantiation(instantiation, scope),
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

    private Value EvaluateConcatenation(ConcatSyntax concat, IReadOnlyDictionary<string, Value> scope)
    {
        var left = Evaluate(concat.Left, scope);
        var right = Evaluate(concat.Right, scope);
        return new StringValue(ValueToString(left) + ValueToString(right));
    }

    private Value EvaluateBangCall(BangCallSyntax bangCall, IReadOnlyDictionary<string, Value> scope)
    {
        switch (bangCall.OperatorName)
        {
            case "if":
            {
                var cond = Evaluate(bangCall.Arguments[0], scope);
                return IsTruthy(cond)
                    ? Evaluate(bangCall.Arguments[1], scope)
                    : Evaluate(bangCall.Arguments[2], scope);
            }

            case "gt":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope), "!gt");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!gt");
                return new IntegerValue(a > b ? 1 : 0);
            }

            case "ge":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope), "!ge");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!ge");
                return new IntegerValue(a >= b ? 1 : 0);
            }

            case "lt":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope), "!lt");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!lt");
                return new IntegerValue(a < b ? 1 : 0);
            }

            case "le":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope), "!le");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!le");
                return new IntegerValue(a <= b ? 1 : 0);
            }

            case "eq":
            {
                var a = Evaluate(bangCall.Arguments[0], scope);
                var b = Evaluate(bangCall.Arguments[1], scope);
                return new IntegerValue(ValuesEqual(a, b) ? 1 : 0);
            }

            case "ne":
            {
                var a = Evaluate(bangCall.Arguments[0], scope);
                var b = Evaluate(bangCall.Arguments[1], scope);
                return new IntegerValue(ValuesEqual(a, b) ? 0 : 1);
            }

            case "add":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope), "!add");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!add");
                return new IntegerValue(a + b);
            }

            case "sub":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope), "!sub");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!sub");
                return new IntegerValue(a - b);
            }

            case "mul":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope), "!mul");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!mul");
                return new IntegerValue(a * b);
            }

            case "and":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope), "!and");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!and");
                return new IntegerValue(a & b);
            }

            case "or":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope), "!or");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!or");
                return new IntegerValue(a | b);
            }

            case "not":
            {
                var val = Evaluate(bangCall.Arguments[0], scope);
                return new IntegerValue(IsTruthy(val) ? 0 : 1);
            }

            case "size":
            {
                var val = Evaluate(bangCall.Arguments[0], scope);
                return val switch
                {
                    StringValue str => new IntegerValue(str.Value.Length),
                    ListValue list => new IntegerValue(list.Items.Count),
                    _ => throw new InvalidOperationException($"!size requires a string or list argument, got {val.GetType().Name}."),
                };
            }

            case "toupper":
            {
                var str = ToString(Evaluate(bangCall.Arguments[0], scope), "!toupper");
                return new StringValue(str.ToUpperInvariant());
            }

            case "tolower":
            {
                var str = ToString(Evaluate(bangCall.Arguments[0], scope), "!tolower");
                return new StringValue(str.ToLowerInvariant());
            }

            case "substr":
            {
                var str = ToString(Evaluate(bangCall.Arguments[0], scope), "!substr");
                var start = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!substr");
                var clampedStart = Math.Max(0, Math.Min(start, str.Length));
                if (bangCall.Arguments.Count >= 3)
                {
                    var len = ToInteger(Evaluate(bangCall.Arguments[2], scope), "!substr");
                    var clampedLen = Math.Max(0, Math.Min(len, str.Length - clampedStart));
                    return new StringValue(str.Substring(clampedStart, clampedLen));
                }

                return new StringValue(str.Substring(clampedStart));
            }

            case "find":
            {
                var str = ToString(Evaluate(bangCall.Arguments[0], scope), "!find");
                var sub = ToString(Evaluate(bangCall.Arguments[1], scope), "!find");
                var startIndex = bangCall.Arguments.Count >= 3
                    ? ToInteger(Evaluate(bangCall.Arguments[2], scope), "!find")
                    : 0;
                return new IntegerValue(str.IndexOf(sub, startIndex, StringComparison.Ordinal));
            }

            case "range":
            {
                var start = ToInteger(Evaluate(bangCall.Arguments[0], scope), "!range");
                var end = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!range");
                var items = new List<Value>(Math.Max(0, end - start));
                for (var i = start; i < end; i++)
                {
                    items.Add(new IntegerValue(i));
                }

                return new ListValue(items);
            }

            case "listconcat":
            {
                var a = (ListValue)Evaluate(bangCall.Arguments[0], scope);
                var b = (ListValue)Evaluate(bangCall.Arguments[1], scope);
                var items = new List<Value>(a.Items.Count + b.Items.Count);
                items.AddRange(a.Items);
                items.AddRange(b.Items);
                return new ListValue(items);
            }

            case "strconcat":
            {
                var result = string.Concat(bangCall.Arguments.Select(arg => ToString(Evaluate(arg, scope), "!strconcat")));
                return new StringValue(result);
            }

            case "shl":
            {
                var a = ToInteger(Evaluate(bangCall.Arguments[0], scope), "!shl");
                var b = ToInteger(Evaluate(bangCall.Arguments[1], scope), "!shl");
                return new IntegerValue(a << b);
            }

            case "cast":
            {
                return Evaluate(bangCall.Arguments[0], scope);
            }

            case "isa":
            {
                var val = Evaluate(bangCall.Arguments[0], scope);
                return new IntegerValue(IsValueOfType(val, bangCall.TypeArgument) ? 1 : 0);
            }

            case "cond":
            {
                for (var i = 0; i + 1 < bangCall.Arguments.Count; i += 2)
                {
                    if (IsTruthy(Evaluate(bangCall.Arguments[i], scope)))
                    {
                        return Evaluate(bangCall.Arguments[i + 1], scope);
                    }
                }

                throw new InvalidOperationException("!cond requires at least one true condition.");
            }

            case "interleave":
            {
                var listVal = (ListValue)Evaluate(bangCall.Arguments[0], scope);
                var sep = ToString(Evaluate(bangCall.Arguments[1], scope), "!interleave");
                return new StringValue(string.Join(sep, listVal.Items.Select(item => ValueToString(item))));
            }

            case "subst":
            {
                var from = ToString(Evaluate(bangCall.Arguments[0], scope), "!subst");
                var to = ToString(Evaluate(bangCall.Arguments[1], scope), "!subst");
                var text = ToString(Evaluate(bangCall.Arguments[2], scope), "!subst");
                return new StringValue(text.Replace(from, to));
            }

            case "head":
            {
                var list = (ListValue)Evaluate(bangCall.Arguments[0], scope);
                if (list.Items.Count == 0)
                {
                    throw new InvalidOperationException("!head requires a non-empty list.");
                }

                return list.Items[0];
            }

            case "tail":
            {
                var list = (ListValue)Evaluate(bangCall.Arguments[0], scope);
                if (list.Items.Count == 0)
                {
                    throw new InvalidOperationException("!tail requires a non-empty list.");
                }

                return new ListValue(list.Items.Skip(1).ToList());
            }

            case "empty":
            {
                var val = Evaluate(bangCall.Arguments[0], scope);
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
                var listValue = (ListValue)Evaluate(bangCall.Arguments[1], scope);
                var results = new List<Value>();
                foreach (var item in listValue.Items)
                {
                    var innerScope = scope.ToDictionary(static kv => kv.Key, static kv => kv.Value);
                    innerScope[variable] = item;
                    if (IsTruthy(Evaluate(bangCall.Arguments[2], innerScope)))
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

    private Value EvaluateFoldl(FoldlSyntax foldl, IReadOnlyDictionary<string, Value> scope)
    {
        var accValue = Evaluate(foldl.Init, scope);
        var listValue = (ListValue)Evaluate(foldl.List, scope);
        foreach (var item in listValue.Items)
        {
            var innerScope = scope.ToDictionary(static kv => kv.Key, static kv => kv.Value);
            innerScope[foldl.AccVar] = accValue;
            innerScope[foldl.CurVar] = item;
            accValue = Evaluate(foldl.Body, innerScope);
        }

        return accValue;
    }

    private Value EvaluateClassInstantiation(ClassInstantiationSyntax instantiation, IReadOnlyDictionary<string, Value> scope)
    {
        if (!context.Classes.TryGetValue(instantiation.ClassName, out var classSyntax))
        {
            throw new KeyNotFoundException($"Unknown TableGen class '{instantiation.ClassName}'.");
        }

        var fields = instantiateClass(classSyntax, instantiation.Arguments, scope);
        if (!fields.TryGetValue(instantiation.FieldName, out var fieldValue))
        {
            throw new KeyNotFoundException($"Class '{instantiation.ClassName}' has no field '{instantiation.FieldName}'.");
        }

        return fieldValue;
    }

    private Value EvaluateAnonymousClassInstantiation(AnonymousClassInstantiationSyntax inst, IReadOnlyDictionary<string, Value> scope)
    {
        if (!context.Classes.TryGetValue(inst.ClassName, out var classSyntax))
        {
            throw new KeyNotFoundException($"Unknown TableGen class '{inst.ClassName}'.");
        }

        var fields = instantiateClass(classSyntax, inst.Arguments, scope);
        return new AnonymousRecordValue(inst.ClassName, fields);
    }

    private Value EvaluateFieldAccess(FieldAccessSyntax fieldAccess, IReadOnlyDictionary<string, Value> scope)
    {
        var obj = Evaluate(fieldAccess.Object, scope);

        if (obj is AnonymousRecordValue rec)
        {
            return rec.Fields.TryGetValue(fieldAccess.FieldName, out var fv) ? fv : new UnsetValue();
        }

        if (obj is RecordReferenceValue recRef && context.DefinitionsByName.TryGetValue(recRef.RecordName, out var defSyntax))
        {
            var fields = new Dictionary<string, Value>();
            var recScope = new Dictionary<string, Value>();
            applyBases(defSyntax.Bases, recScope, fields);
            applyBody(defSyntax.BodyItems, recScope, fields);
            return fields.TryGetValue(fieldAccess.FieldName, out var fieldVal) ? fieldVal : new UnsetValue();
        }

        return new UnsetValue();
    }

    private Value EvaluateSubscript(SubscriptSyntax subscript, IReadOnlyDictionary<string, Value> scope)
    {
        var target = Evaluate(subscript.Target, scope);
        var index = ToInteger(Evaluate(subscript.Index, scope), "subscript");
        return target switch
        {
            ListValue list => list.Items[NormalizeIndex(index, list.Items.Count, "list subscript")],
            StringValue str => new StringValue(str.Value[NormalizeIndex(index, str.Value.Length, "string subscript")].ToString()),
            _ => throw new InvalidOperationException($"Cannot subscript {target.GetType().Name}."),
        };
    }

    private Value EvaluateForeach(ForeachSyntax forEach, IReadOnlyDictionary<string, Value> scope)
    {
        var listValue = (ListValue)Evaluate(forEach.List, scope);
        var results = new List<Value>(listValue.Items.Count);
        foreach (var item in listValue.Items)
        {
            var innerScope = scope.ToDictionary(static kv => kv.Key, static kv => kv.Value);
            innerScope[forEach.VarName] = item;
            results.Add(Evaluate(forEach.Body, innerScope));
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

    private Value ResolveIdentifier(string name, IReadOnlyDictionary<string, Value> scope)
    {
        if (scope.TryGetValue(name, out var value))
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
