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
        private readonly Dictionary<string, ClassSyntax> classes;
        private readonly Dictionary<string, DefSyntax> definitionsByName;

        public Evaluator(DocumentSyntax document)
        {
            classes = document.Declarations
                .OfType<ClassSyntax>()
                .ToDictionary(static c => c.Name, static c => c);
            foreach (var builtin in CreateBuiltinClasses())
            {
                if (!classes.ContainsKey(builtin.Name))
                {
                    classes.Add(builtin.Name, builtin);
                }
            }

            Definitions = document.Declarations.OfType<DefSyntax>().ToList();
            definitionsByName = Definitions.ToDictionary(static definition => definition.Name, static definition => definition);
        }

        private IReadOnlyList<DefSyntax> Definitions { get; }

        public InterpretedDocument Evaluate()
        {
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

                if (classes.TryGetValue(@base.Name, out var classSyntax))
                {
                    CollectBaseClasses(classSyntax.Bases, seenBaseClasses, baseClasses);
                }
            }
        }

        private void ApplyBases(
            IReadOnlyList<BaseSyntax> bases,
            Dictionary<string, Value> scope,
            Dictionary<string, Value> fields)
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

        private Dictionary<string, Value> InstantiateClass(
            ClassSyntax classSyntax,
            IReadOnlyList<ExpressionSyntax> arguments,
            IReadOnlyDictionary<string, Value> outerScope)
        {
            var scope = new Dictionary<string, Value>();
            var fields = new Dictionary<string, Value>();

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
            IReadOnlyList<BodyItemSyntax> bodyItems,
            Dictionary<string, Value> scope,
            Dictionary<string, Value> fields)
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
                IdentifierSyntax identifier => ResolveIdentifier(identifier.Name, scope),
                ListSyntax list => new ListValue(list.Items.Select(item => EvaluateExpression(item, scope)).ToList()),
                DagSyntax dag => new DagValue(dag.OperatorName, dag.Arguments.Select(argument => new DagArgumentValue(EvaluateExpression(argument.Value, scope), argument.Name)).ToList()),
                _ => throw new InvalidOperationException("Unknown TableGen expression."),
            };
        }

        private Value ResolveIdentifier(string name, IReadOnlyDictionary<string, Value> scope)
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

        private Value CoerceValue(string typeName, Value value)
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

        private Value CoerceExistingValue(Value existingValue, Value replacementValue)
        {
            return existingValue switch
            {
                BitValue when replacementValue is IntegerValue integer => new BitValue(integer.Value != 0),
                BitValue when replacementValue is BitValue => replacementValue,
                _ => replacementValue,
            };
        }

        private static IReadOnlyList<ClassSyntax> CreateBuiltinClasses()
        {
            return
            [
                new ClassSyntax(
                    "Dialect",
                    [],
                    [],
                    [
                        new FieldSyntax("string", "name", null),
                        new FieldSyntax("string", "cppNamespace", null),
                        new FieldSyntax("string", "summary", null),
                        new FieldSyntax("string", "description", null),
                        new FieldSyntax("bit", "hasConstantMaterializer", new IntegerSyntax(0)),
                    ]),
                new ClassSyntax(
                    "Op",
                    [
                        new TemplateParameterSyntax("Dialect", "dialect", null),
                        new TemplateParameterSyntax("string", "mnemonic", null),
                        new TemplateParameterSyntax("list<Trait>", "traits", new ListSyntax([])),
                    ],
                    [],
                    [
                        new FieldSyntax("Dialect", "dialect", new IdentifierSyntax("dialect")),
                        new FieldSyntax("string", "mnemonic", new IdentifierSyntax("mnemonic")),
                        new FieldSyntax("list<Trait>", "traits", new IdentifierSyntax("traits")),
                        new FieldSyntax("string", "summary", null),
                        new FieldSyntax("dag", "arguments", null),
                        new FieldSyntax("dag", "results", null),
                        new FieldSyntax("string", "assemblyFormat", null),
                        new FieldSyntax("string", "cppClassName", null),
                    ]),
                new ClassSyntax(
                    "AttrDef",
                    [
                        new TemplateParameterSyntax("Dialect", "dialect", null),
                        new TemplateParameterSyntax("string", "attrName", null),
                    ],
                    [],
                    [
                        new FieldSyntax("Dialect", "dialect", new IdentifierSyntax("dialect")),
                        new FieldSyntax("string", "attrName", new IdentifierSyntax("attrName")),
                        new FieldSyntax("string", "cppClassName", null),
                    ]),
                new ClassSyntax(
                    "TypeDef",
                    [
                        new TemplateParameterSyntax("Dialect", "dialect", null),
                        new TemplateParameterSyntax("string", "typeName", null),
                    ],
                    [],
                    [
                        new FieldSyntax("Dialect", "dialect", new IdentifierSyntax("dialect")),
                        new FieldSyntax("string", "typeName", new IdentifierSyntax("typeName")),
                        new FieldSyntax("string", "cppClassName", null),
                    ]),
            ];
        }
    }
}
