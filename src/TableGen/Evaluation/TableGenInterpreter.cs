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
        private readonly Dictionary<string, TableGenDefSyntax> definitionsByName;

        public Evaluator(TableGenDocumentSyntax document)
        {
            classes = document.Declarations
                .OfType<TableGenClassSyntax>()
                .ToDictionary(static c => c.Name, static c => c);
            foreach (var builtin in CreateBuiltinClasses())
            {
                if (!classes.ContainsKey(builtin.Name))
                {
                    classes.Add(builtin.Name, builtin);
                }
            }

            Definitions = document.Declarations.OfType<TableGenDefSyntax>().ToList();
            definitionsByName = Definitions.ToDictionary(static definition => definition.Name, static definition => definition);
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
            var baseClasses = new List<string>();
            var seenBaseClasses = new HashSet<string>();
            CollectBaseClasses(definition.Bases, seenBaseClasses, baseClasses);
            ApplyBases(definition.Bases, scope, fields);
            ApplyBody(definition.BodyItems, scope, fields);
            return new TableGenRecord(definition.Name, baseClasses, fields);
        }

        private void CollectBaseClasses(
            IReadOnlyList<TableGenBaseSyntax> bases,
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
                TableGenDagSyntax dag => new DagValue(dag.OperatorName, dag.Arguments.Select(argument => new DagArgumentValue(EvaluateExpression(argument.Value, scope), argument.Name)).ToList()),
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

            if (definitionsByName.ContainsKey(name))
            {
                return new RecordReferenceValue(name);
            }

            return new SymbolReferenceValue(name);
        }

        private TableGenValue CoerceValue(string typeName, TableGenValue value)
        {
            return typeName switch
            {
                "int" when value is not IntegerValue => throw new InvalidOperationException($"Expected an integer value for '{typeName}'."),
                "string" when value is not StringValue => throw new InvalidOperationException($"Expected a string value for '{typeName}'."),
                "bit" when value is IntegerValue integer => new BitValue(integer.Value != 0),
                "bit" when value is BitValue => value,
                "bit" => throw new InvalidOperationException($"Expected a bit value for '{typeName}'."),
                "dag" when value is not DagValue => throw new InvalidOperationException($"Expected a dag value for '{typeName}'."),
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

        private static IReadOnlyList<TableGenClassSyntax> CreateBuiltinClasses()
        {
            return
            [
                new TableGenClassSyntax(
                    "Dialect",
                    [],
                    [],
                    [
                        new TableGenFieldSyntax("string", "name", null),
                        new TableGenFieldSyntax("string", "cppNamespace", null),
                        new TableGenFieldSyntax("string", "summary", null),
                        new TableGenFieldSyntax("string", "description", null),
                        new TableGenFieldSyntax("bit", "hasConstantMaterializer", new TableGenIntegerSyntax(0)),
                    ]),
                new TableGenClassSyntax(
                    "Op",
                    [
                        new TableGenTemplateParameterSyntax("Dialect", "dialect", null),
                        new TableGenTemplateParameterSyntax("string", "mnemonic", null),
                        new TableGenTemplateParameterSyntax("list<Trait>", "traits", new TableGenListSyntax([])),
                    ],
                    [],
                    [
                        new TableGenFieldSyntax("Dialect", "dialect", new TableGenIdentifierSyntax("dialect")),
                        new TableGenFieldSyntax("string", "mnemonic", new TableGenIdentifierSyntax("mnemonic")),
                        new TableGenFieldSyntax("list<Trait>", "traits", new TableGenIdentifierSyntax("traits")),
                        new TableGenFieldSyntax("string", "summary", null),
                        new TableGenFieldSyntax("dag", "arguments", null),
                        new TableGenFieldSyntax("dag", "results", null),
                        new TableGenFieldSyntax("string", "assemblyFormat", null),
                        new TableGenFieldSyntax("string", "cppClassName", null),
                    ]),
                new TableGenClassSyntax(
                    "AttrDef",
                    [
                        new TableGenTemplateParameterSyntax("Dialect", "dialect", null),
                        new TableGenTemplateParameterSyntax("string", "attrName", null),
                    ],
                    [],
                    [
                        new TableGenFieldSyntax("Dialect", "dialect", new TableGenIdentifierSyntax("dialect")),
                        new TableGenFieldSyntax("string", "attrName", new TableGenIdentifierSyntax("attrName")),
                        new TableGenFieldSyntax("string", "cppClassName", null),
                    ]),
                new TableGenClassSyntax(
                    "TypeDef",
                    [
                        new TableGenTemplateParameterSyntax("Dialect", "dialect", null),
                        new TableGenTemplateParameterSyntax("string", "typeName", null),
                    ],
                    [],
                    [
                        new TableGenFieldSyntax("Dialect", "dialect", new TableGenIdentifierSyntax("dialect")),
                        new TableGenFieldSyntax("string", "typeName", new TableGenIdentifierSyntax("typeName")),
                        new TableGenFieldSyntax("string", "cppClassName", null),
                    ]),
            ];
        }
    }
}
