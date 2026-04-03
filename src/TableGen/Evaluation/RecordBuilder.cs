namespace TableGen.Evaluation;

using System;
using System.Collections.Generic;
using TableGen.Syntax;

/// <summary>
/// Builds fully evaluated TableGen records and class instantiations.
/// </summary>
internal sealed class RecordBuilder
{
    private readonly EvaluationContext context;
    private readonly ExpressionEvaluator expressionEvaluator;
    private readonly Dictionary<string, Record> evaluatedDefinitions = new();

    public RecordBuilder(EvaluationContext context)
    {
        this.context = context;
        expressionEvaluator = new ExpressionEvaluator(
            context,
            (classSyntax, arguments, outerScope, tryResolveValue) => InstantiateClass(classSyntax, arguments, outerScope, tryResolveValue),
            definition => BuildDefinition(definition));
    }

    public EvaluationResult<InterpretedDocument> BuildDocument()
    {
        var emptyScope = Scope.Empty;
        foreach (var defvar in context.Document.Declarations.OfType<DefVarSyntax>())
        {
            var defvarValue = expressionEvaluator.TryEvaluate(defvar.Value, emptyScope);
            if (!defvarValue.IsSuccess)
            {
                return EvaluationResult<InterpretedDocument>.Failure(defvarValue.Error!);
            }

            context.DefvarValues[defvar.Name] = defvarValue.Value;
        }

        var records = new List<Record>(context.Definitions.Count);
        foreach (var definition in context.Definitions)
        {
            var record = BuildDefinition(definition);
            if (!record.IsSuccess)
            {
                return EvaluationResult<InterpretedDocument>.Failure(record.Error!);
            }

            records.Add(record.Value);
        }

        return EvaluationResult<InterpretedDocument>.Success(new InterpretedDocument(records));
    }

    public EvaluationResult<Record> BuildDefinition(DefSyntax definition)
    {
        if (evaluatedDefinitions.TryGetValue(definition.Name, out var existingRecord))
        {
            return EvaluationResult<Record>.Success(existingRecord);
        }

        var scope = Scope.Empty;
        var baseClasses = new List<string>();
        var seenBaseClasses = new HashSet<string>();
        CollectBaseClasses(definition.Bases, seenBaseClasses, baseClasses);

        var state = new PendingRecordState();
        var bases = ApplyPendingBases(definition.Bases, scope, state);
        if (!bases.IsSuccess)
        {
            return EvaluationResult<Record>.Failure(bases.Error!);
        }

        var body = ApplyPendingBody(definition.BodyItems, scope, state);
        if (!body.IsSuccess)
        {
            return EvaluationResult<Record>.Failure(body.Error!);
        }

        var fields = ResolveFields(state);
        if (!fields.IsSuccess)
        {
            return EvaluationResult<Record>.Failure(fields.Error!);
        }

        var record = new Record(definition.Name, baseClasses, fields.Value);
        evaluatedDefinitions[definition.Name] = record;
        return EvaluationResult<Record>.Success(record);
    }

    public EvaluationResult<Dictionary<string, Value>> InstantiateClass(
        ClassSyntax classSyntax,
        IReadOnlyList<ExpressionSyntax> arguments,
        Scope outerScope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue = null)
    {
        var scope = Scope.Empty;

        for (var i = 0; i < classSyntax.TemplateParameters.Count; i++)
        {
            var parameter = classSyntax.TemplateParameters[i];
            Value value;
            if (i < arguments.Count)
            {
                var argumentValue = expressionEvaluator.TryEvaluate(arguments[i], outerScope, tryResolveValue);
                if (!argumentValue.IsSuccess)
                {
                    return EvaluationResult<Dictionary<string, Value>>.Failure(argumentValue.Error!);
                }

                value = argumentValue.Value;
            }
            else if (parameter.DefaultValue != null)
            {
                var defaultValue = expressionEvaluator.TryEvaluate(parameter.DefaultValue, scope, tryResolveValue);
                if (!defaultValue.IsSuccess)
                {
                    return EvaluationResult<Dictionary<string, Value>>.Failure(defaultValue.Error!);
                }

                value = defaultValue.Value;
            }
            else
            {
                return EvaluationResult<Dictionary<string, Value>>.Failure(
                    new InvalidOperationException($"Missing value for template parameter '{parameter.Name}' on class '{classSyntax.Name}'."));
            }

            scope = scope.With(parameter.Name, value);
        }

        var state = new PendingRecordState();
        var bases = ApplyPendingBases(classSyntax.Bases, scope, state);
        if (!bases.IsSuccess)
        {
            return EvaluationResult<Dictionary<string, Value>>.Failure(bases.Error!);
        }

        var body = ApplyPendingBody(classSyntax.BodyItems, scope, state);
        if (!body.IsSuccess)
        {
            return EvaluationResult<Dictionary<string, Value>>.Failure(body.Error!);
        }

        return ResolveFields(state);
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

    private EvaluationResult<bool> ApplyPendingBases(
        IReadOnlyList<BaseSyntax> bases,
        Scope scope,
        PendingRecordState state)
    {
        foreach (var @base in bases)
        {
            if (!context.Classes.TryGetValue(@base.Name, out var classSyntax))
            {
                return EvaluationResult<bool>.Failure(new KeyNotFoundException($"Unknown TableGen class '{@base.Name}'."));
            }

            var classState = InstantiatePendingClass(classSyntax, @base.Arguments, scope);
            if (!classState.IsSuccess)
            {
                return EvaluationResult<bool>.Failure(classState.Error!);
            }

            state.Import(classState.Value);
        }

        return EvaluationResult<bool>.Success(true);
    }

    private EvaluationResult<PendingRecordState> InstantiatePendingClass(
        ClassSyntax classSyntax,
        IReadOnlyList<ExpressionSyntax> arguments,
        Scope outerScope)
    {
        var scope = Scope.Empty;

        for (var i = 0; i < classSyntax.TemplateParameters.Count; i++)
        {
            var parameter = classSyntax.TemplateParameters[i];
            Value value;
            if (i < arguments.Count)
            {
                var argumentValue = expressionEvaluator.TryEvaluate(arguments[i], outerScope);
                if (!argumentValue.IsSuccess)
                {
                    return EvaluationResult<PendingRecordState>.Failure(argumentValue.Error!);
                }

                value = argumentValue.Value;
            }
            else if (parameter.DefaultValue != null)
            {
                var defaultValue = expressionEvaluator.TryEvaluate(parameter.DefaultValue, scope);
                if (!defaultValue.IsSuccess)
                {
                    return EvaluationResult<PendingRecordState>.Failure(defaultValue.Error!);
                }

                value = defaultValue.Value;
            }
            else
            {
                return EvaluationResult<PendingRecordState>.Failure(
                    new InvalidOperationException($"Missing value for template parameter '{parameter.Name}' on class '{classSyntax.Name}'."));
            }

            scope = scope.With(parameter.Name, value);
        }

        var state = new PendingRecordState();
        var bases = ApplyPendingBases(classSyntax.Bases, scope, state);
        if (!bases.IsSuccess)
        {
            return EvaluationResult<PendingRecordState>.Failure(bases.Error!);
        }

        var body = ApplyPendingBody(classSyntax.BodyItems, scope, state);
        if (!body.IsSuccess)
        {
            return EvaluationResult<PendingRecordState>.Failure(body.Error!);
        }

        return EvaluationResult<PendingRecordState>.Success(state);
    }

    private EvaluationResult<bool> ApplyPendingBody(
        IReadOnlyList<BodyItemSyntax> bodyItems,
        Scope scope,
        PendingRecordState state)
    {
        var currentScope = scope;
        foreach (var item in bodyItems)
        {
            switch (item)
            {
                case FieldSyntax field:
                    state.DefineField(field, currentScope);
                    break;
                case LetSyntax let:
                    state.ApplyLet(let, currentScope);
                    break;
                case LocalDefVarSyntax defVar:
                {
                    var value = expressionEvaluator.TryEvaluate(defVar.Value, currentScope, TryResolveField(state));
                    if (!value.IsSuccess)
                    {
                        return EvaluationResult<bool>.Failure(value.Error!);
                    }

                    currentScope = currentScope.With(defVar.Name, value.Value);
                    break;
                }
                case AssertSyntax assert:
                {
                    var condition = expressionEvaluator.TryEvaluate(assert.Condition, currentScope, TryResolveField(state));
                    if (!condition.IsSuccess)
                    {
                        return EvaluationResult<bool>.Failure(condition.Error!);
                    }

                    var truthy = ExpressionEvaluator.TryIsTruthy(condition.Value);
                    if (!truthy.IsSuccess)
                    {
                        return EvaluationResult<bool>.Failure(truthy.Error!);
                    }

                    if (!truthy.Value)
                    {
                        Exception? messageError = null;
                        var message = assert.Message == null
                            ? "TableGen assertion failed."
                            : GetAssertionMessage(assert.Message, currentScope, state, out messageError);
                        if (messageError != null)
                        {
                            return EvaluationResult<bool>.Failure(messageError);
                        }

                        return EvaluationResult<bool>.Failure(new InvalidOperationException(message));
                    }

                    break;
                }
            }
        }

        return EvaluationResult<bool>.Success(true);
    }

        private string GetAssertionMessage(
            ExpressionSyntax messageExpression,
            Scope scope,
            PendingRecordState state,
            out Exception? error)
    {
        var message = expressionEvaluator.TryEvaluate(messageExpression, scope, TryResolveField(state));
        if (!message.IsSuccess)
        {
            error = message.Error;
            return string.Empty;
        }

        var text = ExpressionEvaluator.TryValueToString(message.Value);
        error = text.Error;
        return text.IsSuccess ? text.Value : string.Empty;
    }

    private EvaluationResult<Dictionary<string, Value>> ResolveFields(PendingRecordState state)
    {
        var fields = new Dictionary<string, Value>();
        foreach (var pair in state.Fields)
        {
            if (!pair.Value.HasExpression)
            {
                continue;
            }

            var value = TryResolveFieldValue(state, pair.Key);
            if (!value.IsSuccess)
            {
                return EvaluationResult<Dictionary<string, Value>>.Failure(value.Error!);
            }

            fields[pair.Key] = value.Value;
        }

        return EvaluationResult<Dictionary<string, Value>>.Success(fields);
    }

    private ExpressionEvaluator.TryResolveValue TryResolveField(PendingRecordState state)
    {
        return name => TryResolveFieldValue(state, name);
    }

    private EvaluationResult<Value> TryResolveFieldValue(PendingRecordState state, string name)
    {
        if (!state.TryGetField(name, out var field) || !field.HasExpression)
        {
            return EvaluationResult<Value>.Failure(new KeyNotFoundException($"Unknown field '{name}'."));
        }

        if (field.HasResolvedValue)
        {
            return EvaluationResult<Value>.Success(field.ResolvedValue!);
        }

        if (field.IsResolving)
        {
            return EvaluationResult<Value>.Failure(new InvalidOperationException($"Detected a cycle while resolving field '{name}'."));
        }

        field.IsResolving = true;
        try
        {
            var resolved = expressionEvaluator.TryEvaluate(field.Expression!, field.LexicalScope, TryResolveField(state));
            if (!resolved.IsSuccess)
            {
                return EvaluationResult<Value>.Failure(resolved.Error!);
            }

            var finalValue = resolved.Value;
            if (field.DeclaredTypeName != null)
            {
                var coerced = ExpressionEvaluator.TryCoerceValue(field.DeclaredTypeName, finalValue);
                if (!coerced.IsSuccess)
                {
                    return EvaluationResult<Value>.Failure(coerced.Error!);
                }

                finalValue = coerced.Value;
            }

            field.ResolvedValue = finalValue;
            field.HasResolvedValue = true;
            return EvaluationResult<Value>.Success(finalValue);
        }
        finally
        {
            field.IsResolving = false;
        }
    }
}
