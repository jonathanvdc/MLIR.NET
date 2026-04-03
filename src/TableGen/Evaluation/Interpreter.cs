namespace TableGen.Evaluation;

using System.Collections.Generic;
using System.Linq;
using TableGen.Syntax;

/// <summary>
/// Evaluates TableGen syntax into expanded records.
/// </summary>
public static class Interpreter
{
    /// <summary>
    /// Evaluates a parsed TableGen document.
    /// </summary>
    /// <param name="document">The parsed syntax tree.</param>
    /// <returns>The interpreted document.</returns>
    public static InterpretedDocument Evaluate(DocumentSyntax document)
    {
        var evaluator = new Evaluator(document);
        return evaluator.Evaluate();
    }

    private sealed class Evaluator
    {
        private readonly DocumentSyntax document;
        private readonly Dictionary<string, ClassSyntax> classes;
        private readonly Dictionary<string, DefSyntax> definitionsByName;
        private readonly Dictionary<string, Value> defvarValues = new();

        public Evaluator(DocumentSyntax document)
        {
            this.document = document;
            classes = document.Declarations
                .OfType<ClassSyntax>()
                .ToDictionary(static c => c.Name, static c => c);
            Definitions = document.Declarations.OfType<DefSyntax>().ToList();
            definitionsByName = Definitions.ToDictionary(static definition => definition.Name, static definition => definition);
        }

        private IReadOnlyList<DefSyntax> Definitions { get; }

        public InterpretedDocument Evaluate()
        {
            var emptyScope = new Dictionary<string, Value>();
            foreach (var defvar in document.Declarations.OfType<DefVarSyntax>())
            {
                defvarValues[defvar.Name] = EvaluateExpression(defvar.Value, emptyScope);
            }

            var records = new List<Record>(Definitions.Count);
            foreach (var definition in Definitions)
            {
                records.Add(EvaluateDefinition(definition));
            }

            return new InterpretedDocument(records);
        }

        private Record EvaluateDefinition(DefSyntax definition)
        {
            var scope = new Dictionary<string, Value>();
            var fields = new Dictionary<string, Value>();
            var baseClasses = new List<string>();
            var seenBaseClasses = new HashSet<string>();
            CollectBaseClasses(definition.Bases, seenBaseClasses, baseClasses);

            // Collect let overrides from the definition body first so base-class fields
            // that depend on overridden values (e.g. attrName = dialect.name # "." # mnemonic)
            // are computed with the correct values.
            var letOverrides = new Dictionary<string, Value>(StringComparer.Ordinal);
            CollectLetOverrides(definition.BodyItems, letOverrides);

            ApplyBases(definition.Bases, scope, fields, preOverrides: letOverrides.Count > 0 ? letOverrides : null);
            ApplyBody(definition.BodyItems, scope, fields, letOverrides: null, preOverrides: null);
            return new Record(definition.Name, baseClasses, fields);
        }

        private void CollectLetOverrides(
            IReadOnlyList<BodyItemSyntax> bodyItems,
            Dictionary<string, Value> letOverrides)
        {
            // We need a temporary scope to evaluate the let expressions.  We use an empty scope
            // because at this point we don't yet have field values available; simple expressions
            // (string literals, identifier references to template parameters, etc.) will still resolve.
            // This is a best-effort pre-pass: only lets with evaluable expressions contribute.
            var tempScope = new Dictionary<string, Value>();
            foreach (var item in bodyItems)
            {
                if (item is LetSyntax let)
                {
                    try
                    {
                        var value = EvaluateExpression(let.Value, tempScope);
                        letOverrides[let.Name] = value;
                        tempScope[let.Name] = value;
                    }
                    catch
                    {
                        // If the expression can't be evaluated yet, skip it.
                    }
                }
            }
        }

        private void CollectBaseClasses(
            IReadOnlyList<BaseSyntax> bases,
            HashSet<string> seenBaseClasses,
            List<string> baseClasses)
        {
            foreach (var @base in bases)
            {
                if (seenBaseClasses.Add(@base.Name))
                {
                    baseClasses.Add(@base.Name);
                }

                if (classes.TryGetValue(@base.Name, out var classSyntax))
                {
                    CollectBaseClasses(classSyntax.Bases, seenBaseClasses, baseClasses);
                }
            }
        }

        private Dictionary<string, Value> InstantiateClass(
            ClassSyntax classSyntax,
            IReadOnlyList<ExpressionSyntax> arguments,
            IReadOnlyDictionary<string, Value> outerScope)
        {
            return InstantiateClass(classSyntax, arguments, outerScope, preOverrides: null);
        }

        /// <summary>
        /// Instantiates a class, optionally pre-seeding fields with <paramref name="preOverrides"/> that come
        /// from <c>let</c> statements in a derived class.  Pre-seeded values are written into the local
        /// scope before any base-class or body processing so that computed fields such as
        /// <c>attrName = dialect.name # "." # mnemonic</c> in <c>AttrDef</c> see the already-resolved value
        /// of <c>mnemonic</c> that was supplied by the derived class body.
        /// Field declarations whose initializer would overwrite a pre-seeded entry are skipped.
        /// </summary>
        private Dictionary<string, Value> InstantiateClass(
            ClassSyntax classSyntax,
            IReadOnlyList<ExpressionSyntax> arguments,
            IReadOnlyDictionary<string, Value> outerScope,
            IReadOnlyDictionary<string, Value>? preOverrides)
        {
            var scope = new Dictionary<string, Value>();
            var fields = new Dictionary<string, Value>();

            // Pre-seed scope and fields with derived-class let overrides so that field initializers
            // in this class (and its base classes) can see them before they are declared.
            if (preOverrides != null)
            {
                foreach (var pair in preOverrides)
                {
                    scope[pair.Key] = pair.Value;
                    fields[pair.Key] = pair.Value;
                }
            }

            for (var i = 0; i < classSyntax.TemplateParameters.Count; i++)
            {
                var parameter = classSyntax.TemplateParameters[i];
                Value value;
                if (i < arguments.Count)
                {
                    value = EvaluateExpression(arguments[i], outerScope);
                }
                else if (parameter.DefaultValue != null)
                {
                    // Evaluate default values against the partially-built scope so that
                    // earlier template parameters (e.g. `string str = sym`) resolve correctly.
                    value = EvaluateExpression(parameter.DefaultValue, scope);
                }
                else
                {
                    throw new InvalidOperationException($"Missing value for template parameter '{parameter.Name}' on class '{classSyntax.Name}'.");
                }

                scope[parameter.Name] = value;
            }

            // Pre-scan this class's own body for `let` overrides and merge them with any incoming
            // preOverrides before processing base classes.  This ensures that computed base-class fields
            // (e.g. `attrName = dialect.name # "." # mnemonic` in AttrDef) see the mnemonic value set
            // by `let mnemonic = name` in this class's body (EnumAttr).
            var effectivePreOverrides = CollectBodyLetOverrides(classSyntax.BodyItems, scope, preOverrides);

            ApplyBases(classSyntax.Bases, scope, fields, effectivePreOverrides);

            // Apply body.  Field declarations (FieldSyntax) whose name is in effectivePreOverrides are skipped
            // because the derived class has already supplied the authoritative value.
            ApplyBody(classSyntax.BodyItems, scope, fields, letOverrides: null, preOverrides: effectivePreOverrides);

            return fields;
        }

        private IReadOnlyDictionary<string, Value>? CollectBodyLetOverrides(
            IReadOnlyList<BodyItemSyntax> bodyItems,
            IReadOnlyDictionary<string, Value> scope,
            IReadOnlyDictionary<string, Value>? incoming)
        {
            // Evaluate let statements from this body in a scratch scope and collect their values.
            var tempScope = new Dictionary<string, Value>(StringComparer.Ordinal);
            foreach (var pair in scope) tempScope[pair.Key] = pair.Value;
            if (incoming != null)
            {
                foreach (var pair in incoming) tempScope[pair.Key] = pair.Value;
            }

            Dictionary<string, Value>? collected = null;
            foreach (var item in bodyItems)
            {
                if (item is LetSyntax let)
                {
                    try
                    {
                        var value = EvaluateExpression(let.Value, tempScope);
                        collected ??= new Dictionary<string, Value>(StringComparer.Ordinal);
                        collected[let.Name] = value;
                        tempScope[let.Name] = value;
                    }
                    catch
                    {
                        // Expression can't be evaluated yet (depends on fields not yet in scope).
                    }
                }
            }

            if (collected == null) return incoming;

            // Merge: incoming overrides are the base, body lets override on top.
            var result = new Dictionary<string, Value>(StringComparer.Ordinal);
            if (incoming != null) foreach (var pair in incoming) result[pair.Key] = pair.Value;
            foreach (var pair in collected) result[pair.Key] = pair.Value;
            return result;
        }

        private void ApplyBases(
            IReadOnlyList<BaseSyntax> bases,
            Dictionary<string, Value> scope,
            Dictionary<string, Value> fields)
        {
            ApplyBases(bases, scope, fields, preOverrides: null);
        }

        private void ApplyBases(
            IReadOnlyList<BaseSyntax> bases,
            Dictionary<string, Value> scope,
            Dictionary<string, Value> fields,
            IReadOnlyDictionary<string, Value>? preOverrides)
        {
            foreach (var @base in bases)
            {
                if (!classes.TryGetValue(@base.Name, out var classSyntax))
                {
                    throw new KeyNotFoundException($"Unknown TableGen class '{@base.Name}'.");
                }

                var classFields = InstantiateClass(classSyntax, @base.Arguments, scope, preOverrides);
                foreach (var field in classFields)
                {
                    // Always write base-class fields, including pre-seeded ones — InstantiateClass
                    // will have applied the correct type coercion (e.g. bit Enabled sees IntegerValue(1)
                    // and stores BitValue(true)), so the outer ApplyBody can then apply CoerceExistingValue
                    // correctly when the definition's own let statement is processed.
                    fields[field.Key] = field.Value;
                    scope[field.Key] = field.Value;
                }
            }
        }

        private void ApplyBody(
            IReadOnlyList<BodyItemSyntax> bodyItems,
            Dictionary<string, Value> scope,
            Dictionary<string, Value> fields,
            System.Collections.Generic.HashSet<string>? letOverrides,
            IReadOnlyDictionary<string, Value>? preOverrides)
        {
            foreach (var item in bodyItems)
            {
                switch (item)
                {
                    case FieldSyntax field:
                    {
                        if (field.Initializer == null)
                        {
                            continue;
                        }

                        // When a derived-class let has already supplied a value for this field, apply
                        // the field declaration's type coercion to it (e.g. convert IntegerValue(1) to
                        // BitValue(true) for a `bit` field) and update scope, but don't re-evaluate.
                        if (preOverrides != null && preOverrides.TryGetValue(field.Name, out var preValue))
                        {
                            var coerced = CoerceValue(field.TypeName, preValue);
                            fields[field.Name] = coerced;
                            scope[field.Name] = coerced;
                            continue;
                        }

                        var value = EvaluateExpression(field.Initializer, scope);
                        fields[field.Name] = CoerceValue(field.TypeName, value);
                        scope[field.Name] = fields[field.Name];
                        break;
                    }
                    case LetSyntax let:
                    {
                        var value = EvaluateExpression(let.Value, scope);
                        var finalValue = fields.TryGetValue(let.Name, out var existingField)
                            ? CoerceExistingValue(existingField, value)
                            : value;
                        fields[let.Name] = finalValue;
                        scope[let.Name] = finalValue;
                        letOverrides?.Add(let.Name);
                        break;
                    }
                    case LocalDefVarSyntax defVar:
                    {
                        scope[defVar.Name] = EvaluateExpression(defVar.Value, scope);
                        break;
                    }
                    case AssertSyntax assert:
                    {
                        var condition = EvaluateExpression(assert.Condition, scope);
                        if (!IsTruthy(condition))
                        {
                            var message = assert.Message == null
                                ? "TableGen assertion failed."
                                : ValueToString(EvaluateExpression(assert.Message, scope));
                            throw new InvalidOperationException(message);
                        }

                        break;
                    }
                }
            }
        }

        private Value EvaluateExpression(ExpressionSyntax expression, IReadOnlyDictionary<string, Value> scope)
        {
            return expression switch
            {
                IntegerSyntax integer => new IntegerValue(integer.Value),
                StringSyntax str => new StringValue(str.Value),
                UnsetSyntax => new UnsetValue(),
                IdentifierSyntax identifier => ResolveIdentifier(identifier.Name, scope),
                ListSyntax list => new ListValue(list.Items.Select(item => EvaluateExpression(item, scope)).ToList()),
                DagSyntax dag => new DagValue(dag.OperatorName, dag.Arguments.Select(argument => new DagArgumentValue(EvaluateExpression(argument.Value, scope), argument.Name)).ToList()),
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

        private Value EvaluateConcatenation(ConcatSyntax concat, IReadOnlyDictionary<string, Value> scope)
        {
            var left = EvaluateExpression(concat.Left, scope);
            var right = EvaluateExpression(concat.Right, scope);
            return new StringValue(ValueToString(left) + ValueToString(right));
        }

        private Value EvaluateBangCall(BangCallSyntax bangCall, IReadOnlyDictionary<string, Value> scope)
        {
            switch (bangCall.OperatorName)
            {
                case "if":
                {
                    var cond = EvaluateExpression(bangCall.Arguments[0], scope);
                    return IsTruthy(cond)
                        ? EvaluateExpression(bangCall.Arguments[1], scope)
                        : EvaluateExpression(bangCall.Arguments[2], scope);
                }

                case "gt":
                {
                    var a = ToInteger(EvaluateExpression(bangCall.Arguments[0], scope), "!gt");
                    var b = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!gt");
                    return new IntegerValue(a > b ? 1 : 0);
                }

                case "ge":
                {
                    var a = ToInteger(EvaluateExpression(bangCall.Arguments[0], scope), "!ge");
                    var b = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!ge");
                    return new IntegerValue(a >= b ? 1 : 0);
                }

                case "lt":
                {
                    var a = ToInteger(EvaluateExpression(bangCall.Arguments[0], scope), "!lt");
                    var b = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!lt");
                    return new IntegerValue(a < b ? 1 : 0);
                }

                case "le":
                {
                    var a = ToInteger(EvaluateExpression(bangCall.Arguments[0], scope), "!le");
                    var b = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!le");
                    return new IntegerValue(a <= b ? 1 : 0);
                }

                case "eq":
                {
                    var a = EvaluateExpression(bangCall.Arguments[0], scope);
                    var b = EvaluateExpression(bangCall.Arguments[1], scope);
                    return new IntegerValue(ValuesEqual(a, b) ? 1 : 0);
                }

                case "ne":
                {
                    var a = EvaluateExpression(bangCall.Arguments[0], scope);
                    var b = EvaluateExpression(bangCall.Arguments[1], scope);
                    return new IntegerValue(ValuesEqual(a, b) ? 0 : 1);
                }

                case "add":
                {
                    var a = ToInteger(EvaluateExpression(bangCall.Arguments[0], scope), "!add");
                    var b = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!add");
                    return new IntegerValue(a + b);
                }

                case "sub":
                {
                    var a = ToInteger(EvaluateExpression(bangCall.Arguments[0], scope), "!sub");
                    var b = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!sub");
                    return new IntegerValue(a - b);
                }

                case "mul":
                {
                    var a = ToInteger(EvaluateExpression(bangCall.Arguments[0], scope), "!mul");
                    var b = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!mul");
                    return new IntegerValue(a * b);
                }

                case "and":
                {
                    var a = ToInteger(EvaluateExpression(bangCall.Arguments[0], scope), "!and");
                    var b = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!and");
                    return new IntegerValue(a & b);
                }

                case "or":
                {
                    var a = ToInteger(EvaluateExpression(bangCall.Arguments[0], scope), "!or");
                    var b = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!or");
                    return new IntegerValue(a | b);
                }

                case "not":
                {
                    var val = EvaluateExpression(bangCall.Arguments[0], scope);
                    return new IntegerValue(IsTruthy(val) ? 0 : 1);
                }

                case "size":
                {
                    var val = EvaluateExpression(bangCall.Arguments[0], scope);
                    return val switch
                    {
                        StringValue str => new IntegerValue(str.Value.Length),
                        ListValue list => new IntegerValue(list.Items.Count),
                        _ => throw new InvalidOperationException($"!size requires a string or list argument, got {val.GetType().Name}."),
                    };
                }

                case "toupper":
                {
                    var str = ToString(EvaluateExpression(bangCall.Arguments[0], scope), "!toupper");
                    return new StringValue(str.ToUpperInvariant());
                }

                case "tolower":
                {
                    var str = ToString(EvaluateExpression(bangCall.Arguments[0], scope), "!tolower");
                    return new StringValue(str.ToLowerInvariant());
                }

                case "substr":
                {
                    var str = ToString(EvaluateExpression(bangCall.Arguments[0], scope), "!substr");
                    var start = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!substr");
                    var clampedStart = System.Math.Max(0, System.Math.Min(start, str.Length));
                    if (bangCall.Arguments.Count >= 3)
                    {
                        var len = ToInteger(EvaluateExpression(bangCall.Arguments[2], scope), "!substr");
                        var clampedLen = System.Math.Max(0, System.Math.Min(len, str.Length - clampedStart));
                        return new StringValue(str.Substring(clampedStart, clampedLen));
                    }

                    return new StringValue(str.Substring(clampedStart));
                }

                case "find":
                {
                    var str = ToString(EvaluateExpression(bangCall.Arguments[0], scope), "!find");
                    var sub = ToString(EvaluateExpression(bangCall.Arguments[1], scope), "!find");
                    var startIndex = bangCall.Arguments.Count >= 3
                        ? ToInteger(EvaluateExpression(bangCall.Arguments[2], scope), "!find")
                        : 0;
                    return new IntegerValue(str.IndexOf(sub, startIndex, System.StringComparison.Ordinal));
                }

                case "range":
                {
                    var start = ToInteger(EvaluateExpression(bangCall.Arguments[0], scope), "!range");
                    var end = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!range");
                    var items = new System.Collections.Generic.List<Value>(System.Math.Max(0, end - start));
                    for (var i = start; i < end; i++)
                    {
                        items.Add(new IntegerValue(i));
                    }

                    return new ListValue(items);
                }

                case "listconcat":
                {
                    var a = (ListValue)EvaluateExpression(bangCall.Arguments[0], scope);
                    var b = (ListValue)EvaluateExpression(bangCall.Arguments[1], scope);
                    var items = new System.Collections.Generic.List<Value>(a.Items.Count + b.Items.Count);
                    items.AddRange(a.Items);
                    items.AddRange(b.Items);
                    return new ListValue(items);
                }

                case "strconcat":
                {
                    var result = string.Concat(bangCall.Arguments.Select(arg => ToString(EvaluateExpression(arg, scope), "!strconcat")));
                    return new StringValue(result);
                }

                case "shl":
                {
                    var a = ToInteger(EvaluateExpression(bangCall.Arguments[0], scope), "!shl");
                    var b = ToInteger(EvaluateExpression(bangCall.Arguments[1], scope), "!shl");
                    return new IntegerValue(a << b);
                }

                case "cast":
                {
                    return EvaluateExpression(bangCall.Arguments[0], scope);
                }

                case "isa":
                {
                    var val = EvaluateExpression(bangCall.Arguments[0], scope);
                    return new IntegerValue(IsValueOfType(val, bangCall.TypeArgument) ? 1 : 0);
                }

                case "cond":
                {
                    for (var i = 0; i + 1 < bangCall.Arguments.Count; i += 2)
                    {
                        if (IsTruthy(EvaluateExpression(bangCall.Arguments[i], scope)))
                        {
                            return EvaluateExpression(bangCall.Arguments[i + 1], scope);
                        }
                    }

                    throw new InvalidOperationException("!cond requires at least one true condition.");
                }

                case "interleave":
                {
                    var listVal = (ListValue)EvaluateExpression(bangCall.Arguments[0], scope);
                    var sep = ToString(EvaluateExpression(bangCall.Arguments[1], scope), "!interleave");
                    return new StringValue(string.Join(sep, listVal.Items.Select(item => ValueToString(item))));
                }

                case "subst":
                {
                    var from = ToString(EvaluateExpression(bangCall.Arguments[0], scope), "!subst");
                    var to = ToString(EvaluateExpression(bangCall.Arguments[1], scope), "!subst");
                    var text = ToString(EvaluateExpression(bangCall.Arguments[2], scope), "!subst");
                    return new StringValue(text.Replace(from, to));
                }

                case "head":
                {
                    var list = (ListValue)EvaluateExpression(bangCall.Arguments[0], scope);
                    if (list.Items.Count == 0)
                    {
                        throw new InvalidOperationException("!head requires a non-empty list.");
                    }

                    return list.Items[0];
                }

                case "tail":
                {
                    var list = (ListValue)EvaluateExpression(bangCall.Arguments[0], scope);
                    if (list.Items.Count == 0)
                    {
                        throw new InvalidOperationException("!tail requires a non-empty list.");
                    }

                    return new ListValue(list.Items.Skip(1).ToList());
                }

                case "empty":
                {
                    var val = EvaluateExpression(bangCall.Arguments[0], scope);
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
                    var listValue = (ListValue)EvaluateExpression(bangCall.Arguments[1], scope);
                    var results = new List<Value>();
                    foreach (var item in listValue.Items)
                    {
                        var innerScope = scope.ToDictionary(static kv => kv.Key, static kv => kv.Value);
                        innerScope[variable] = item;
                        if (IsTruthy(EvaluateExpression(bangCall.Arguments[2], innerScope)))
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
            var accValue = EvaluateExpression(foldl.Init, scope);
            var listValue = (ListValue)EvaluateExpression(foldl.List, scope);
            foreach (var item in listValue.Items)
            {
                var innerScope = scope.ToDictionary(static kv => kv.Key, static kv => kv.Value);
                innerScope[foldl.AccVar] = accValue;
                innerScope[foldl.CurVar] = item;
                accValue = EvaluateExpression(foldl.Body, innerScope);
            }

            return accValue;
        }

        private Value EvaluateClassInstantiation(ClassInstantiationSyntax instantiation, IReadOnlyDictionary<string, Value> scope)
        {
            if (!classes.TryGetValue(instantiation.ClassName, out var classSyntax))
            {
                throw new KeyNotFoundException($"Unknown TableGen class '{instantiation.ClassName}'.");
            }

            var fields = InstantiateClass(classSyntax, instantiation.Arguments, scope);
            if (!fields.TryGetValue(instantiation.FieldName, out var fieldValue))
            {
                throw new KeyNotFoundException($"Class '{instantiation.ClassName}' has no field '{instantiation.FieldName}'.");
            }

            return fieldValue;
        }

        private Value EvaluateAnonymousClassInstantiation(AnonymousClassInstantiationSyntax inst, IReadOnlyDictionary<string, Value> scope)
        {
            if (!classes.TryGetValue(inst.ClassName, out var classSyntax))
            {
                throw new KeyNotFoundException($"Unknown TableGen class '{inst.ClassName}'.");
            }

            var fields = InstantiateClass(classSyntax, inst.Arguments, scope);
            return new AnonymousRecordValue(inst.ClassName, fields);
        }

        private Value EvaluateFieldAccess(FieldAccessSyntax fieldAccess, IReadOnlyDictionary<string, Value> scope)
        {
            var obj = EvaluateExpression(fieldAccess.Object, scope);

            if (obj is AnonymousRecordValue rec)
            {
                return rec.Fields.TryGetValue(fieldAccess.FieldName, out var fv) ? fv : new UnsetValue();
            }

            if (obj is RecordReferenceValue recRef && definitionsByName.TryGetValue(recRef.RecordName, out var defSyntax))
            {
                var fields = new Dictionary<string, Value>();
                var recScope = new Dictionary<string, Value>();
                ApplyBases(defSyntax.Bases, recScope, fields);
                ApplyBody(defSyntax.BodyItems, recScope, fields, letOverrides: null, preOverrides: null);
                return fields.TryGetValue(fieldAccess.FieldName, out var fieldVal) ? fieldVal : new UnsetValue();
            }

            return new UnsetValue();
        }

        private Value EvaluateSubscript(SubscriptSyntax subscript, IReadOnlyDictionary<string, Value> scope)
        {
            var target = EvaluateExpression(subscript.Target, scope);
            var index = ToInteger(EvaluateExpression(subscript.Index, scope), "subscript");
            return target switch
            {
                ListValue list => list.Items[NormalizeIndex(index, list.Items.Count, "list subscript")],
                StringValue str => new StringValue(str.Value[NormalizeIndex(index, str.Value.Length, "string subscript")].ToString()),
                _ => throw new InvalidOperationException($"Cannot subscript {target.GetType().Name}."),
            };
        }

        private Value EvaluateForeach(ForeachSyntax forEach, IReadOnlyDictionary<string, Value> scope)
        {
            var listValue = (ListValue)EvaluateExpression(forEach.List, scope);
            var results = new List<Value>(listValue.Items.Count);
            foreach (var item in listValue.Items)
            {
                var innerScope = scope.ToDictionary(static kv => kv.Key, static kv => kv.Value);
                innerScope[forEach.VarName] = item;
                results.Add(EvaluateExpression(forEach.Body, innerScope));
            }

            return new ListValue(results);
        }

        private static bool IsTruthy(Value value) => value switch
        {
            IntegerValue integer => integer.Value != 0,
            BitValue bit => bit.Value,
            _ => throw new InvalidOperationException($"Expected a boolean-like condition, got {value.GetType().Name}."),
        };

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

        private static string ValueToString(Value value) => value switch
        {
            StringValue str => str.Value,
            IntegerValue integer => integer.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            BitValue bit => bit.Value ? "1" : "0",
            ListValue list => string.Concat(list.Items.Select(ValueToString)),
            SymbolReferenceValue symbol => symbol.SymbolName,
            RecordReferenceValue record => record.RecordName,
            UnsetValue => string.Empty,
            AnonymousRecordValue rec => rec.ClassName,
            _ => throw new InvalidOperationException($"Cannot convert {value.GetType().Name} to string for concatenation."),
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
                RecordReferenceValue recordReference => definitionsByName.TryGetValue(recordReference.RecordName, out var definition)
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

            if (!classes.TryGetValue(className, out var classSyntax))
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

            if (defvarValues.TryGetValue(name, out value))
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

            if (definitionsByName.ContainsKey(name))
            {
                return new RecordReferenceValue(name);
            }

            return new SymbolReferenceValue(name);
        }

        private Value CoerceValue(string typeName, Value value)
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

        private Value CoerceExistingValue(Value existingValue, Value replacementValue)
        {
            return existingValue switch
            {
                BitValue when replacementValue is IntegerValue integer => new BitValue(integer.Value != 0),
                BitValue when replacementValue is BitValue => replacementValue,
                _ => replacementValue,
            };
        }
    }
}
