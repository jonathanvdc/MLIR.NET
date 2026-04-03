namespace TableGen.Evaluation;

using System;
using System.Collections.Generic;
using TableGen.Syntax;

/// <summary>
/// Builds fully evaluated TableGen records and class instantiations.
/// </summary>
internal sealed class RecordBuilder
{
    /// <summary>
    /// Holds document-wide lookup tables and caches shared across evaluation.
    /// </summary>
    private readonly EvaluationContext context;

    /// <summary>
    /// Evaluates expressions that appear inside field initializers, lets, and template arguments.
    /// </summary>
    private readonly ExpressionEvaluator expressionEvaluator;

    /// <summary>
    /// Memoizes fully built top-level definitions by record name.
    /// </summary>
    private readonly Dictionary<string, Record> evaluatedDefinitions = new();

    /// <summary>
    /// Memoizes class instantiations by class name and evaluated template argument fingerprint.
    /// </summary>
    private readonly Dictionary<(string ClassName, string ArgumentKey), Dictionary<string, Value>> instantiatedClasses = new();

    /// <summary>
    /// Initializes a builder for a single parsed document.
    /// </summary>
    /// <param name="context">The shared document-level evaluation state.</param>
    public RecordBuilder(EvaluationContext context)
    {
        this.context = context;
        expressionEvaluator = new ExpressionEvaluator(
            context,
            (classSyntax, arguments, outerScope, tryResolveValue) => InstantiateClass(classSyntax, arguments, outerScope, tryResolveValue),
            definition => BuildDefinition(definition));
    }

    /// <summary>
    /// Evaluates the entire document into concrete records.
    /// </summary>
    /// <returns>The interpreted document on success, or a diagnostic on failure.</returns>
    public EvaluationResult<InterpretedDocument> BuildDocument()
    {
        var emptyScope = Scope.Empty;

        // Top-level defvars behave like global constants that later expressions may reference.
        foreach (var defvar in context.Document.Declarations.OfType<DefVarSyntax>())
        {
            var defvarValue = expressionEvaluator.TryEvaluate(defvar.Value, emptyScope);
            if (!defvarValue.IsSuccess)
            {
                return EvaluationResult<InterpretedDocument>.Failure(defvarValue.Diagnostic!);
            }

            context.DefvarValues[defvar.Name] = defvarValue.Value;
        }

        var records = new List<Record>(context.Definitions.Count);
        foreach (var definition in context.Definitions)
        {
            var record = BuildDefinition(definition);
            if (!record.IsSuccess)
            {
                return EvaluationResult<InterpretedDocument>.Failure(record.Diagnostic!);
            }

            records.Add(record.Value);
        }

        return EvaluationResult<InterpretedDocument>.Success(new InterpretedDocument(records));
    }

    /// <summary>
    /// Builds one top-level <c>def</c> into its fully evaluated record form.
    /// </summary>
    /// <param name="definition">The definition to evaluate.</param>
    /// <returns>The fully evaluated record or a diagnostic.</returns>
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
            return EvaluationResult<Record>.Failure(bases.Diagnostic!);
        }

        ApplyTopLevelLets(definition.TopLevelLets, scope, state);

        var body = ApplyPendingBody(definition.BodyItems, scope, state);
        if (!body.IsSuccess)
        {
            return EvaluationResult<Record>.Failure(body.Diagnostic!);
        }

        var fields = ResolveFields(state);
        if (!fields.IsSuccess)
        {
            return EvaluationResult<Record>.Failure(fields.Diagnostic!);
        }

        var record = new Record(definition.Name, baseClasses, fields.Value);
        evaluatedDefinitions[definition.Name] = record;
        return EvaluationResult<Record>.Success(record);
    }

    /// <summary>
    /// Instantiates a class and resolves all of its fields for expression-time use.
    /// </summary>
    /// <param name="classSyntax">The class declaration to instantiate.</param>
    /// <param name="arguments">The supplied template arguments.</param>
    /// <param name="outerScope">The lexical scope in which the instantiation appears.</param>
    /// <param name="tryResolveValue">Optional deferred lookup for field references.</param>
    /// <returns>A dictionary containing the instantiated field values or a diagnostic.</returns>
    public EvaluationResult<Dictionary<string, Value>> InstantiateClass(
        ClassSyntax classSyntax,
        IReadOnlyList<ExpressionSyntax> arguments,
        Scope outerScope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue = null)
    {
        var boundArguments = new List<Value>(classSyntax.TemplateParameters.Count);
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
                    return EvaluationResult<Dictionary<string, Value>>.Failure(argumentValue.Diagnostic!);
                }

                value = argumentValue.Value;
            }
            else if (parameter.DefaultValue != null)
            {
                var defaultValue = expressionEvaluator.TryEvaluate(parameter.DefaultValue, scope, tryResolveValue);
                if (!defaultValue.IsSuccess)
                {
                    return EvaluationResult<Dictionary<string, Value>>.Failure(defaultValue.Diagnostic!);
                }

                value = defaultValue.Value;
            }
            else
            {
                return EvaluationResult<Dictionary<string, Value>>.Failure(
                    InvalidOperation($"Missing value for template parameter '{parameter.Name}' on class '{classSyntax.Name}'."));
            }

            scope = scope.With(parameter.Name, value);
            boundArguments.Add(value);
        }

        var cacheKey = (classSyntax.Name, ValueFingerprint.Create(boundArguments));
        if (instantiatedClasses.TryGetValue(cacheKey, out var cachedFields))
        {
            return EvaluationResult<Dictionary<string, Value>>.Success(CloneFields(cachedFields));
        }

        var state = new PendingRecordState();
        var bases = ApplyPendingBases(classSyntax.Bases, scope, state);
        if (!bases.IsSuccess)
        {
            return EvaluationResult<Dictionary<string, Value>>.Failure(bases.Diagnostic!);
        }

        ApplyTopLevelLets(classSyntax.TopLevelLets, scope, state);

        var body = ApplyPendingBody(classSyntax.BodyItems, scope, state);
        if (!body.IsSuccess)
        {
            return EvaluationResult<Dictionary<string, Value>>.Failure(body.Diagnostic!);
        }

        var resolvedFields = ResolveFields(state);
        if (!resolvedFields.IsSuccess)
        {
            return resolvedFields;
        }

        instantiatedClasses[cacheKey] = CloneFields(resolvedFields.Value);
        return EvaluationResult<Dictionary<string, Value>>.Success(CloneFields(resolvedFields.Value));
    }

    /// <summary>
    /// Collects the transitive base-class names for a record, preserving first-seen order.
    /// </summary>
    /// <param name="bases">The direct bases to walk.</param>
    /// <param name="seenBaseClasses">Tracks base names that have already been emitted.</param>
    /// <param name="baseClasses">Accumulates the ordered base-class list.</param>
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
                // Recurse into class declarations so a record remembers the full inherited chain.
                CollectBaseClasses(classSyntax.Bases, seenBaseClasses, baseClasses);
            }
        }
    }

    /// <summary>
    /// Instantiates and imports each direct base class into the pending state.
    /// </summary>
    /// <param name="bases">The direct bases to apply.</param>
    /// <param name="scope">The lexical scope for evaluating base arguments.</param>
    /// <param name="state">The pending record state receiving inherited fields.</param>
    /// <returns>A success flag or a diagnostic.</returns>
    private EvaluationResult<bool> ApplyPendingBases(
        IReadOnlyList<BaseSyntax> bases,
        Scope scope,
        PendingRecordState state)
    {
        foreach (var @base in bases)
        {
            if (!context.Classes.TryGetValue(@base.Name, out var classSyntax))
            {
                return EvaluationResult<bool>.Failure(MissingKey($"Unknown TableGen class '{@base.Name}'."));
            }

            var classState = InstantiatePendingClass(classSyntax, @base.Arguments, scope);
            if (!classState.IsSuccess)
            {
                return EvaluationResult<bool>.Failure(classState.Diagnostic!);
            }

            state.Import(classState.Value);
        }

        return EvaluationResult<bool>.Success(true);
    }

    /// <summary>
    /// Instantiates a class into pending-field form without forcing its fields immediately.
    /// </summary>
    /// <param name="classSyntax">The class declaration to instantiate.</param>
    /// <param name="arguments">The supplied template arguments.</param>
    /// <param name="outerScope">The lexical scope in which the instantiation occurs.</param>
    /// <returns>The pending field state for the instantiation or a diagnostic.</returns>
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
                    return EvaluationResult<PendingRecordState>.Failure(argumentValue.Diagnostic!);
                }

                value = argumentValue.Value;
            }
            else if (parameter.DefaultValue != null)
            {
                var defaultValue = expressionEvaluator.TryEvaluate(parameter.DefaultValue, scope);
                if (!defaultValue.IsSuccess)
                {
                    return EvaluationResult<PendingRecordState>.Failure(defaultValue.Diagnostic!);
                }

                value = defaultValue.Value;
            }
            else
            {
                return EvaluationResult<PendingRecordState>.Failure(
                    InvalidOperation($"Missing value for template parameter '{parameter.Name}' on class '{classSyntax.Name}'."));
            }

            scope = scope.With(parameter.Name, value);
        }

        var state = new PendingRecordState();
        var bases = ApplyPendingBases(classSyntax.Bases, scope, state);
        if (!bases.IsSuccess)
        {
            return EvaluationResult<PendingRecordState>.Failure(bases.Diagnostic!);
        }

        ApplyTopLevelLets(classSyntax.TopLevelLets, scope, state);

        var body = ApplyPendingBody(classSyntax.BodyItems, scope, state);
        if (!body.IsSuccess)
        {
            return EvaluationResult<PendingRecordState>.Failure(body.Diagnostic!);
        }

        return EvaluationResult<PendingRecordState>.Success(state);
    }

    /// <summary>
    /// Applies outer <c>let ... in</c> bindings to the current pending state.
    /// </summary>
    /// <param name="topLevelLets">The top-level lets captured on the surrounding declaration.</param>
    /// <param name="scope">The lexical scope where those lets were declared.</param>
    /// <param name="state">The pending state being updated.</param>
    private void ApplyTopLevelLets(
        IReadOnlyList<LetSyntax> topLevelLets,
        Scope scope,
        PendingRecordState state)
    {
        foreach (var let in topLevelLets)
        {
            state.ApplyTopLevelLet(let, scope);
        }
    }

    /// <summary>
    /// Clones a field dictionary so cached class instantiations are not mutated by callers.
    /// </summary>
    /// <param name="fields">The field dictionary to copy.</param>
    /// <returns>A shallow copy of the field dictionary.</returns>
    private static Dictionary<string, Value> CloneFields(IReadOnlyDictionary<string, Value> fields)
    {
        var clone = new Dictionary<string, Value>(fields.Count);
        foreach (var pair in fields)
        {
            clone[pair.Key] = pair.Value;
        }

        return clone;
    }

    /// <summary>
    /// Applies the body items of a class or record to the pending state in lexical order.
    /// </summary>
    /// <param name="bodyItems">The body items to interpret.</param>
    /// <param name="scope">The starting lexical scope.</param>
    /// <param name="state">The pending record state being updated.</param>
    /// <returns>A success flag or a diagnostic.</returns>
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
                    // Local defvars extend only the remainder of the current body, matching TableGen's lexical scope rules.
                    var value = expressionEvaluator.TryEvaluate(defVar.Value, currentScope, TryResolveField(state));
                    if (!value.IsSuccess)
                    {
                        return EvaluationResult<bool>.Failure(value.Diagnostic!);
                    }

                    currentScope = currentScope.With(defVar.Name, value.Value);
                    break;
                }
                case AssertSyntax assert:
                {
                    var condition = expressionEvaluator.TryEvaluate(assert.Condition, currentScope, TryResolveField(state));
                    if (!condition.IsSuccess)
                    {
                        return EvaluationResult<bool>.Failure(condition.Diagnostic!);
                    }

                    var truthy = ValueUtilities.TryIsTruthy(condition.Value);
                    if (!truthy.IsSuccess)
                    {
                        return EvaluationResult<bool>.Failure(truthy.Diagnostic!);
                    }

                    if (!truthy.Value)
                    {
                        EvaluationDiagnostic? messageError = null;
                        var message = assert.Message == null
                            ? "TableGen assertion failed."
                            : GetAssertionMessage(assert.Message, currentScope, state, out messageError);
                        if (messageError != null)
                        {
                            return EvaluationResult<bool>.Failure(messageError);
                        }

                        return EvaluationResult<bool>.Failure(InvalidOperation(message));
                    }

                    break;
                }
            }
        }

        return EvaluationResult<bool>.Success(true);
    }

    /// <summary>
    /// Evaluates an assert message expression and converts it to text.
    /// </summary>
    /// <param name="messageExpression">The expression that should produce the assertion message.</param>
    /// <param name="scope">The lexical scope for evaluating the message.</param>
    /// <param name="state">The pending record state used for lazy field resolution.</param>
    /// <param name="error">Receives the diagnostic if message evaluation fails.</param>
    /// <returns>The final message text when successful.</returns>
    private string GetAssertionMessage(
        ExpressionSyntax messageExpression,
        Scope scope,
        PendingRecordState state,
        out EvaluationDiagnostic? error)
    {
        var message = expressionEvaluator.TryEvaluate(messageExpression, scope, TryResolveField(state));
        if (!message.IsSuccess)
        {
            error = message.Diagnostic;
            return string.Empty;
        }

        var text = ValueUtilities.TryValueToString(message.Value);
        error = text.Diagnostic;
        return text.IsSuccess ? text.Value : string.Empty;
    }

    /// <summary>
    /// Resolves every pending field into its final evaluated value.
    /// </summary>
    /// <param name="state">The pending state whose fields should be forced.</param>
    /// <returns>The resolved field map or a diagnostic.</returns>
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
                    return EvaluationResult<Dictionary<string, Value>>.Failure(value.Diagnostic!);
                }

            fields[pair.Key] = value.Value;
        }

        return EvaluationResult<Dictionary<string, Value>>.Success(fields);
    }

    /// <summary>
    /// Creates a callback that resolves fields lazily against the given pending state.
    /// </summary>
    /// <param name="state">The pending state whose fields should be resolved on demand.</param>
    /// <returns>A lazy field resolver callback.</returns>
    private ExpressionEvaluator.TryResolveValue TryResolveField(PendingRecordState state)
    {
        return name => TryResolveFieldValue(state, name);
    }

    /// <summary>
    /// Resolves a single field value, memoizing the result and detecting dependency cycles.
    /// </summary>
    /// <param name="state">The pending state that owns the field.</param>
    /// <param name="name">The field name to resolve.</param>
    /// <returns>The resolved field value or a diagnostic.</returns>
    private EvaluationResult<Value> TryResolveFieldValue(PendingRecordState state, string name)
    {
            if (!state.TryGetField(name, out var field) || !field.HasExpression)
            {
            return EvaluationResult<Value>.Failure(MissingKey($"Unknown field '{name}'."));
            }

        if (field.HasResolvedValue)
        {
            return EvaluationResult<Value>.Success(field.ResolvedValue!);
        }

        if (field.IsResolving)
        {
            return EvaluationResult<Value>.Failure(InvalidOperation($"Detected a cycle while resolving field '{name}'."));
        }

        field.IsResolving = true;
        try
        {
            // Field expressions close over the lexical scope in which they were declared, not the scope in which they are forced.
            var resolved = expressionEvaluator.TryEvaluate(field.Expression!, field.LexicalScope, TryResolveField(state));
            if (!resolved.IsSuccess)
            {
                return EvaluationResult<Value>.Failure(resolved.Diagnostic!);
            }

            var finalValue = resolved.Value;
            if (field.DeclaredTypeName != null)
            {
                var coerced = ValueUtilities.TryCoerceValue(field.DeclaredTypeName, finalValue);
                if (!coerced.IsSuccess)
                {
                    return EvaluationResult<Value>.Failure(coerced.Diagnostic!);
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

    /// <summary>
    /// Creates an invalid-operation diagnostic with a consistent helper call site.
    /// </summary>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>The constructed diagnostic.</returns>
    private static EvaluationDiagnostic InvalidOperation(string message)
    {
        return new EvaluationDiagnostic(EvaluationDiagnosticKind.InvalidOperation, message);
    }

    /// <summary>
    /// Creates a missing-key diagnostic with a consistent helper call site.
    /// </summary>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>The constructed diagnostic.</returns>
    private static EvaluationDiagnostic MissingKey(string message)
    {
        return new EvaluationDiagnostic(EvaluationDiagnosticKind.MissingKey, message);
    }
}
