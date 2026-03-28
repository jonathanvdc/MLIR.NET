namespace TableGen.Evaluation;

using System.Collections.Generic;
using System.Linq;
using TableGen.Syntax;

/// <summary>
/// Evaluates TableGen syntax into expanded records.
/// </summary>
public static class TableGenInterpreter
{
    /// <summary>
    /// Evaluates a parsed TableGen document.
    /// </summary>
    /// <param name="document">The parsed syntax tree.</param>
    /// <returns>The interpreted document.</returns>
    public static InterpretedDocument Evaluate(TableGenDocumentSyntax document)
    {
        var evaluator = new Evaluator(document);
        return evaluator.Evaluate();
    }

    private sealed class Evaluator
    {
        private readonly Dictionary<string, TableGenClassSyntax> classes;

        public Evaluator(TableGenDocumentSyntax document)
        {
            classes = document.Declarations
                .OfType<TableGenClassSyntax>()
                .ToDictionary(static c => c.Name, static c => c);
            Definitions = document.Declarations.OfType<TableGenDefSyntax>().ToList();
        }

        private IReadOnlyList<TableGenDefSyntax> Definitions { get; }

        public InterpretedDocument Evaluate()
        {
            var records = new List<TableGenRecord>(Definitions.Count);
            foreach (var definition in Definitions)
            {
                records.Add(EvaluateDefinition(definition));
            }

            return new InterpretedDocument(records);
        }

        private TableGenRecord EvaluateDefinition(TableGenDefSyntax definition)
        {
            var scope = new Dictionary<string, TableGenValue>();
            var fields = new Dictionary<string, TableGenValue>();
            ApplyBases(definition.Bases, scope, fields);
            ApplyBody(definition.BodyItems, scope, fields);
            return new TableGenRecord(definition.Name, fields);
        }

        private void ApplyBases(
            IReadOnlyList<TableGenBaseSyntax> bases,
            Dictionary<string, TableGenValue> scope,
            Dictionary<string, TableGenValue> fields)
        {
            foreach (var @base in bases)
            {
                if (!classes.TryGetValue(@base.Name, out var classSyntax))
                {
                    throw new KeyNotFoundException($"Unknown TableGen class '{@base.Name}'.");
                }

                var classFields = InstantiateClass(classSyntax, @base.Arguments, scope);
                foreach (var field in classFields)
                {
                    fields[field.Key] = field.Value;
                    scope[field.Key] = field.Value;
                }
            }
        }

        private Dictionary<string, TableGenValue> InstantiateClass(
            TableGenClassSyntax classSyntax,
            IReadOnlyList<TableGenExpressionSyntax> arguments,
            IReadOnlyDictionary<string, TableGenValue> outerScope)
        {
            var scope = new Dictionary<string, TableGenValue>();
            var fields = new Dictionary<string, TableGenValue>();

            for (var i = 0; i < classSyntax.TemplateParameters.Count; i++)
            {
                var parameter = classSyntax.TemplateParameters[i];
                TableGenValue value;
                if (i < arguments.Count)
                {
                    value = EvaluateExpression(arguments[i], outerScope);
                }
                else if (parameter.DefaultValue != null)
                {
                    value = EvaluateExpression(parameter.DefaultValue, outerScope);
                }
                else
                {
                    throw new InvalidOperationException($"Missing value for template parameter '{parameter.Name}' on class '{classSyntax.Name}'.");
                }

                scope[parameter.Name] = value;
            }

            ApplyBases(classSyntax.Bases, scope, fields);
            ApplyBody(classSyntax.BodyItems, scope, fields);
            return fields;
        }

        private void ApplyBody(
            IReadOnlyList<TableGenBodyItemSyntax> bodyItems,
            Dictionary<string, TableGenValue> scope,
            Dictionary<string, TableGenValue> fields)
        {
            foreach (var item in bodyItems)
            {
                switch (item)
                {
                    case TableGenFieldSyntax field:
                    {
                        if (field.Initializer == null)
                        {
                            continue;
                        }

                        var value = EvaluateExpression(field.Initializer, scope);
                        fields[field.Name] = CoerceValue(field.TypeName, value);
                        scope[field.Name] = fields[field.Name];
                        break;
                    }
                    case TableGenLetSyntax let:
                    {
                        var value = EvaluateExpression(let.Value, scope);
                        var finalValue = fields.TryGetValue(let.Name, out var existingField)
                            ? CoerceExistingValue(existingField, value)
                            : value;
                        fields[let.Name] = finalValue;
                        scope[let.Name] = finalValue;
                        break;
                    }
                }
            }
        }

        private TableGenValue EvaluateExpression(TableGenExpressionSyntax expression, IReadOnlyDictionary<string, TableGenValue> scope)
        {
            return expression switch
            {
                TableGenIntegerSyntax integer => new IntegerValue(integer.Value),
                TableGenStringSyntax str => new StringValue(str.Value),
                TableGenIdentifierSyntax identifier => ResolveIdentifier(identifier.Name, scope),
                TableGenListSyntax list => new ListValue(list.Items.Select(item => EvaluateExpression(item, scope)).ToList()),
                _ => throw new InvalidOperationException("Unknown TableGen expression."),
            };
        }

        private TableGenValue ResolveIdentifier(string name, IReadOnlyDictionary<string, TableGenValue> scope)
        {
            if (scope.TryGetValue(name, out var value))
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

            throw new KeyNotFoundException($"Unknown TableGen identifier '{name}'.");
        }

        private TableGenValue CoerceValue(string typeName, TableGenValue value)
        {
            return typeName switch
            {
                "bit" when value is IntegerValue integer => new BitValue(integer.Value != 0),
                "bit" when value is BitValue => value,
                _ => value,
            };
        }

        private TableGenValue CoerceExistingValue(TableGenValue existingValue, TableGenValue replacementValue)
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
