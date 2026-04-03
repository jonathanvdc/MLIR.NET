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
        private readonly EvaluationContext context;
        private readonly ExpressionEvaluator expressionEvaluator;

        public Evaluator(DocumentSyntax document)
        {
            context = new EvaluationContext(document);
            Definitions = context.Definitions;
            expressionEvaluator = new ExpressionEvaluator(
                context,
                (classSyntax, arguments, outerScope) => InstantiateClass(classSyntax, arguments, outerScope),
                (bases, scope, fields) => ApplyBases(bases, scope, fields),
                (bodyItems, scope, fields) => ApplyBody(bodyItems, scope, fields));
        }

        private IReadOnlyList<DefSyntax> Definitions { get; }

        public InterpretedDocument Evaluate()
        {
            var emptyScope = new Dictionary<string, Value>();
            foreach (var defvar in context.Document.Declarations.OfType<DefVarSyntax>())
            {
                context.DefvarValues[defvar.Name] = expressionEvaluator.Evaluate(defvar.Value, emptyScope);
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
            ApplyBases(definition.Bases, scope, fields);
            ApplyBody(definition.BodyItems, scope, fields);
            return new Record(definition.Name, baseClasses, fields);
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

                if (context.Classes.TryGetValue(@base.Name, out var classSyntax))
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
                    value = expressionEvaluator.Evaluate(arguments[i], outerScope);
                }
                else if (parameter.DefaultValue != null)
                {
                    // Evaluate default values against the partially-built scope so that
                    // earlier template parameters (e.g. `string str = sym`) resolve correctly.
                    value = expressionEvaluator.Evaluate(parameter.DefaultValue, scope);
                }
                else
                {
                    throw new InvalidOperationException($"Missing value for template parameter '{parameter.Name}' on class '{classSyntax.Name}'.");
                }

                scope[parameter.Name] = value;
            }

            var bodyPreOverrides = CollectPreOverrides(classSyntax.BodyItems, scope);
            var mergedPreOverrides = MergePreOverrides(bodyPreOverrides, preOverrides);

            ApplyBases(classSyntax.Bases, scope, fields, mergedPreOverrides);

            // Apply body.  Field declarations (FieldSyntax) whose name is in preOverrides are skipped
            // because the derived class has already supplied the authoritative value.
            ApplyBody(classSyntax.BodyItems, scope, fields, mergedPreOverrides);

            return fields;
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
                if (!context.Classes.TryGetValue(@base.Name, out var classSyntax))
                {
                    throw new KeyNotFoundException($"Unknown TableGen class '{@base.Name}'.");
                }

                var classFields = InstantiateClass(classSyntax, @base.Arguments, scope, preOverrides);
                foreach (var field in classFields)
                {
                    fields[field.Key] = field.Value;
                    scope[field.Key] = field.Value;
                }
            }
        }

        private void ApplyBody(
            IReadOnlyList<BodyItemSyntax> bodyItems,
            Dictionary<string, Value> scope,
            Dictionary<string, Value> fields,
            IReadOnlyDictionary<string, Value>? preOverrides = null)
        {
            foreach (var item in bodyItems)
            {
                switch (item)
                {
                    case FieldSyntax field:
                    {
                        if (preOverrides != null && preOverrides.ContainsKey(field.Name))
                        {
                            continue;
                        }

                        if (field.Initializer == null)
                        {
                            continue;
                        }

                        var value = expressionEvaluator.Evaluate(field.Initializer, scope);
                        fields[field.Name] = ExpressionEvaluator.CoerceValue(field.TypeName, value);
                        scope[field.Name] = fields[field.Name];
                        break;
                    }
                    case LetSyntax let:
                    {
                        var value = expressionEvaluator.Evaluate(let.Value, scope);
                        var finalValue = fields.TryGetValue(let.Name, out var existingField)
                            ? ExpressionEvaluator.CoerceExistingValue(existingField, value)
                            : value;
                        fields[let.Name] = finalValue;
                        scope[let.Name] = finalValue;
                        break;
                    }
                    case LocalDefVarSyntax defVar:
                    {
                        scope[defVar.Name] = expressionEvaluator.Evaluate(defVar.Value, scope);
                        break;
                    }
                    case AssertSyntax assert:
                    {
                        var condition = expressionEvaluator.Evaluate(assert.Condition, scope);
                        if (!ExpressionEvaluator.IsTruthy(condition))
                        {
                            var message = assert.Message == null
                                ? "TableGen assertion failed."
                                : ExpressionEvaluator.ValueToString(expressionEvaluator.Evaluate(assert.Message, scope));
                            throw new InvalidOperationException(message);
                        }

                        break;
                    }
                }
            }
        }

        private Dictionary<string, Value> CollectPreOverrides(
            IReadOnlyList<BodyItemSyntax> bodyItems,
            IReadOnlyDictionary<string, Value> scope)
        {
            var preOverrides = new Dictionary<string, Value>();
            var preOverrideScope = scope.ToDictionary(static kv => kv.Key, static kv => kv.Value);

            foreach (var item in bodyItems.OfType<LetSyntax>())
            {
                try
                {
                    var value = expressionEvaluator.Evaluate(item.Value, preOverrideScope);
                    preOverrides[item.Name] = value;
                    preOverrideScope[item.Name] = value;
                }
                catch (Exception ex) when (
                    ex is InvalidOperationException
                    or InvalidCastException
                    or KeyNotFoundException)
                {
                    // Some let expressions only become valid after inherited fields or later body items
                    // exist. Skip pre-seeding those; the regular body pass will still evaluate them.
                }
            }

            return preOverrides;
        }

        private static IReadOnlyDictionary<string, Value>? MergePreOverrides(
            IReadOnlyDictionary<string, Value>? localPreOverrides,
            IReadOnlyDictionary<string, Value>? inheritedPreOverrides)
        {
            if (localPreOverrides == null || localPreOverrides.Count == 0)
            {
                return inheritedPreOverrides;
            }

            if (inheritedPreOverrides == null || inheritedPreOverrides.Count == 0)
            {
                return localPreOverrides;
            }

            var merged = localPreOverrides.ToDictionary(static kv => kv.Key, static kv => kv.Value);
            foreach (var pair in inheritedPreOverrides)
            {
                merged[pair.Key] = pair.Value;
            }

            return merged;
        }
    }
}
