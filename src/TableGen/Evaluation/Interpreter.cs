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
        private readonly Dictionary<string, Record> evaluatedDefinitions = new();

        public Evaluator(DocumentSyntax document)
        {
            context = new EvaluationContext(document);
            Definitions = context.Definitions;
            expressionEvaluator = new ExpressionEvaluator(
                context,
                (classSyntax, arguments, outerScope, tryResolveValue) => InstantiateClass(classSyntax, arguments, outerScope, tryResolveValue),
                definition => EvaluateDefinition(definition));
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
            if (evaluatedDefinitions.TryGetValue(definition.Name, out var existingRecord))
            {
                return existingRecord;
            }

            var scope = new Dictionary<string, Value>();
            var baseClasses = new List<string>();
            var seenBaseClasses = new HashSet<string>();
            CollectBaseClasses(definition.Bases, seenBaseClasses, baseClasses);

            var state = new PendingRecordState();
            ApplyPendingBases(definition.Bases, scope, state);
            ApplyPendingBody(definition.BodyItems, scope, state);

            var fields = ResolveFields(state);
            var record = new Record(definition.Name, baseClasses, fields);
            evaluatedDefinitions[definition.Name] = record;
            return record;
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
            return InstantiateClass(classSyntax, arguments, outerScope, tryResolveValue: null);
        }

        private Dictionary<string, Value> InstantiateClass(
            ClassSyntax classSyntax,
            IReadOnlyList<ExpressionSyntax> arguments,
            IReadOnlyDictionary<string, Value> outerScope,
            ExpressionEvaluator.TryResolveValue? tryResolveValue)
        {
            var scope = new Dictionary<string, Value>();

            for (var i = 0; i < classSyntax.TemplateParameters.Count; i++)
            {
                var parameter = classSyntax.TemplateParameters[i];
                Value value;
                if (i < arguments.Count)
                {
                    value = expressionEvaluator.Evaluate(arguments[i], outerScope, tryResolveValue);
                }
                else if (parameter.DefaultValue != null)
                {
                    value = expressionEvaluator.Evaluate(parameter.DefaultValue, scope, tryResolveValue);
                }
                else
                {
                    throw new InvalidOperationException($"Missing value for template parameter '{parameter.Name}' on class '{classSyntax.Name}'.");
                }

                scope[parameter.Name] = value;
            }

            var state = new PendingRecordState();
            ApplyPendingBases(classSyntax.Bases, scope, state);
            ApplyPendingBody(classSyntax.BodyItems, scope, state);
            return ResolveFields(state);
        }

        private void ApplyPendingBases(
            IReadOnlyList<BaseSyntax> bases,
            Dictionary<string, Value> scope,
            PendingRecordState state)
        {
            foreach (var @base in bases)
            {
                if (!context.Classes.TryGetValue(@base.Name, out var classSyntax))
                {
                    throw new KeyNotFoundException($"Unknown TableGen class '{@base.Name}'.");
                }

                var classState = InstantiatePendingClass(classSyntax, @base.Arguments, scope);
                state.Import(classState);
            }
        }

        private PendingRecordState InstantiatePendingClass(
            ClassSyntax classSyntax,
            IReadOnlyList<ExpressionSyntax> arguments,
            IReadOnlyDictionary<string, Value> outerScope)
        {
            var scope = new Dictionary<string, Value>();

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
                    value = expressionEvaluator.Evaluate(parameter.DefaultValue, scope);
                }
                else
                {
                    throw new InvalidOperationException($"Missing value for template parameter '{parameter.Name}' on class '{classSyntax.Name}'.");
                }

                scope[parameter.Name] = value;
            }

            var state = new PendingRecordState();
            ApplyPendingBases(classSyntax.Bases, scope, state);
            ApplyPendingBody(classSyntax.BodyItems, scope, state);
            return state;
        }

        private void ApplyPendingBody(
            IReadOnlyList<BodyItemSyntax> bodyItems,
            Dictionary<string, Value> scope,
            PendingRecordState state)
        {
            foreach (var item in bodyItems)
            {
                switch (item)
                {
                    case FieldSyntax field:
                    {
                        state.DefineField(field, scope);
                        break;
                    }
                    case LetSyntax let:
                    {
                        state.ApplyLet(let, scope);
                        break;
                    }
                    case LocalDefVarSyntax defVar:
                    {
                        scope[defVar.Name] = expressionEvaluator.Evaluate(defVar.Value, scope, TryResolveField(state));
                        break;
                    }
                    case AssertSyntax assert:
                    {
                        var condition = expressionEvaluator.Evaluate(assert.Condition, scope, TryResolveField(state));
                        if (!ExpressionEvaluator.IsTruthy(condition))
                        {
                            var message = assert.Message == null
                                ? "TableGen assertion failed."
                                : ExpressionEvaluator.ValueToString(expressionEvaluator.Evaluate(assert.Message, scope, TryResolveField(state)));
                            throw new InvalidOperationException(message);
                        }

                        break;
                    }
                }
            }
        }

        private Dictionary<string, Value> ResolveFields(PendingRecordState state)
        {
            var fields = new Dictionary<string, Value>();
            foreach (var pair in state.Fields)
            {
                if (pair.Value.HasExpression && TryResolveFieldValue(state, pair.Key, out var value))
                {
                    fields[pair.Key] = value;
                }
            }

            return fields;
        }

        private ExpressionEvaluator.TryResolveValue TryResolveField(PendingRecordState state)
        {
            return (string name, out Value value) => TryResolveFieldValue(state, name, out value);
        }

        private bool TryResolveFieldValue(PendingRecordState state, string name, out Value value)
        {
            if (!state.TryGetField(name, out var field) || !field.HasExpression)
            {
                value = null!;
                return false;
            }

            if (field.HasResolvedValue)
            {
                value = field.ResolvedValue!;
                return true;
            }

            if (field.IsResolving)
            {
                throw new InvalidOperationException($"Detected a cycle while resolving field '{name}'.");
            }

            field.IsResolving = true;
            try
            {
                var resolved = expressionEvaluator.Evaluate(field.Expression!, field.LexicalScope, TryResolveField(state));
                if (field.DeclaredTypeName != null)
                {
                    resolved = ExpressionEvaluator.CoerceValue(field.DeclaredTypeName, resolved);
                }

                field.ResolvedValue = resolved;
                field.HasResolvedValue = true;
                value = resolved;
                return true;
            }
            finally
            {
                field.IsResolving = false;
            }
        }
    }
}
