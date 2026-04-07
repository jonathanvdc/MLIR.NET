namespace TableGen.Evaluation;

using System;
using System.Collections.Generic;

using TableGen.Syntax;

/// <summary>
/// Evaluates TableGen expressions against a scope and document-level context.
/// </summary>
internal sealed class ExpressionEvaluator
{
    /// <summary>
    /// Holds document-wide lookup tables and caches used by expression evaluation.
    /// </summary>
    private readonly EvaluationContext context;

    /// <summary>
    /// Instantiates classes so expression-time class calls can compute field values.
    /// </summary>
    private readonly Func<ClassSyntax, IReadOnlyList<ExpressionSyntax>, Scope, TryResolveValue?, EvaluationResult<AnonymousRecordValue>> instantiateClass;

    /// <summary>
    /// Builds top-level definitions on demand for record field access.
    /// </summary>
    private readonly Func<DefSyntax, EvaluationResult<Record>> buildDefinition;

    /// <summary>
    /// Resolves identifiers and <c>!isa</c> queries using document-wide state.
    /// </summary>
    private readonly IdentifierResolver identifierResolver;

    /// <summary>
    /// Evaluates bang operators such as <c>!if</c> and <c>!foreach</c>.
    /// </summary>
    private readonly BangOperatorEvaluator bangOperatorEvaluator;

    /// <summary>
    /// Resolves names that are not stored directly in lexical scope, such as lazily computed fields.
    /// </summary>
    /// <param name="name">The name to resolve.</param>
    /// <returns>The resolved value or a diagnostic.</returns>
    internal delegate EvaluationResult<Value> TryResolveValue(string name);

    /// <summary>
    /// Initializes a new expression evaluator.
    /// </summary>
    /// <param name="context">The shared document-level evaluation state.</param>
    /// <param name="instantiateClass">Callback used for expression-time class instantiation.</param>
    /// <param name="buildDefinition">Callback used for on-demand record building.</param>
    public ExpressionEvaluator(
        EvaluationContext context,
        Func<ClassSyntax, IReadOnlyList<ExpressionSyntax>, Scope, TryResolveValue?, EvaluationResult<AnonymousRecordValue>> instantiateClass,
        Func<DefSyntax, EvaluationResult<Record>> buildDefinition)
    {
        this.context = context;
        this.instantiateClass = instantiateClass;
        this.buildDefinition = buildDefinition;
        identifierResolver = new IdentifierResolver(context);
        bangOperatorEvaluator = new BangOperatorEvaluator(context, identifierResolver, TryEvaluate);
    }

    /// <summary>
    /// Attempts to evaluate an expression into a runtime <see cref="Value"/>.
    /// </summary>
    /// <param name="expression">The syntax node to evaluate.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <returns>The evaluated value or a diagnostic.</returns>
    public EvaluationResult<Value> TryEvaluate(
        ExpressionSyntax expression,
        Scope scope,
        TryResolveValue? tryResolveValue = null)
    {
        switch (expression)
        {
            case IntegerSyntax integer:
                return Success(new IntegerValue(integer.Value));
            case StringSyntax str:
                return Success(new StringValue(str.Value));
            case UnsetSyntax:
                return Success(new UnsetValue());
            case IdentifierSyntax identifier:
                return identifierResolver.ResolveIdentifier(identifier.Name, scope, tryResolveValue);
            case ListSyntax list:
                return TryEvaluateList(list, scope, tryResolveValue);
            case DagSyntax dag:
                return TryEvaluateDag(dag, scope, tryResolveValue);
            case ConcatSyntax concat:
                return EvaluateConcatenation(concat, scope, tryResolveValue);
            case BangCallSyntax bangCall:
                return bangOperatorEvaluator.Evaluate(bangCall, scope, tryResolveValue);
            case FoldlSyntax foldl:
                return EvaluateFoldl(foldl, scope, tryResolveValue);
            case ForeachSyntax forEach:
                return EvaluateForeach(forEach, scope, tryResolveValue);
            case AnonymousClassInstantiationSyntax anonInst:
                return EvaluateAnonymousClassInstantiation(anonInst, scope, tryResolveValue);
            case FieldAccessSyntax fieldAccess:
                return EvaluateFieldAccess(fieldAccess, scope, tryResolveValue);
            case SubscriptSyntax subscript:
                return EvaluateSubscript(subscript, scope, tryResolveValue);
            case ClassInstantiationSyntax instantiation:
                return EvaluateClassInstantiation(instantiation, scope, tryResolveValue);
            default:
                return Failure(InvalidOperation("Unknown TableGen expression."));
        }
    }

    /// <summary>
    /// Evaluates an expression and throws if evaluation fails.
    /// </summary>
    /// <param name="expression">The syntax node to evaluate.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <returns>The evaluated value.</returns>
    public Value Evaluate(
        ExpressionSyntax expression,
        Scope scope,
        TryResolveValue? tryResolveValue = null)
    {
        var result = TryEvaluate(expression, scope, tryResolveValue);
        if (!result.IsSuccess)
        {
            throw result.Diagnostic!.ToException();
        }

        return result.Value;
    }

    /// <summary>
    /// Evaluates a list literal by evaluating each element in order.
    /// </summary>
    /// <param name="list">The list syntax node.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <returns>The evaluated list value or a diagnostic.</returns>
    private EvaluationResult<Value> TryEvaluateList(
        ListSyntax list,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var items = new List<Value>(list.Items.Count);
        foreach (var item in list.Items)
        {
            var itemResult = TryEvaluate(item, scope, tryResolveValue);
            if (!itemResult.IsSuccess)
            {
                return Failure(itemResult.Diagnostic!);
            }

            items.Add(itemResult.Value);
        }

        return Success(new ListValue(items));
    }

    /// <summary>
    /// Evaluates a dag literal by evaluating each argument expression while preserving argument names.
    /// </summary>
    /// <param name="dag">The dag syntax node.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <returns>The evaluated dag value or a diagnostic.</returns>
    private EvaluationResult<Value> TryEvaluateDag(
        DagSyntax dag,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var arguments = new List<DagArgumentValue>(dag.Arguments.Count);
        foreach (var argument in dag.Arguments)
        {
            var valueResult = TryEvaluate(argument.Value, scope, tryResolveValue);
            if (!valueResult.IsSuccess)
            {
                return Failure(valueResult.Diagnostic!);
            }

            arguments.Add(new DagArgumentValue(valueResult.Value, argument.Name));
        }

        return Success(new DagValue(dag.OperatorName, arguments));
    }

    /// <summary>
    /// Evaluates the TableGen <c>#</c> concatenation operator.
    /// </summary>
    /// <param name="concat">The concatenation syntax node.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <returns>The concatenated string value or a diagnostic.</returns>
    private EvaluationResult<Value> EvaluateConcatenation(
        ConcatSyntax concat,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var left = TryEvaluate(concat.Left, scope, tryResolveValue);
        if (!left.IsSuccess)
        {
            return Failure(left.Diagnostic!);
        }

        var right = TryEvaluate(concat.Right, scope, tryResolveValue);
        if (!right.IsSuccess)
        {
            return Failure(right.Diagnostic!);
        }

        // In TableGen, # is overloaded: it concatenates two strings or two lists.
        // Check for list concatenation first so that list traits (e.g. `[A] # B.traits`)
        // are handled before falling back to string concatenation.
        if (left.Value is ListValue leftList && right.Value is ListValue rightList)
        {
            var merged = new System.Collections.Generic.List<Value>(leftList.Items.Count + rightList.Items.Count);
            merged.AddRange(leftList.Items);
            merged.AddRange(rightList.Items);
            return Success(new ListValue(merged));
        }

        var leftString = ValueUtilities.TryValueToString(left.Value);
        if (!leftString.IsSuccess)
        {
            return Failure(leftString.Diagnostic!);
        }

        var rightString = ValueUtilities.TryValueToString(right.Value);
        if (!rightString.IsSuccess)
        {
            return Failure(rightString.Diagnostic!);
        }

        return Success(new StringValue(leftString.Value + rightString.Value));
    }

    /// <summary>
    /// Evaluates a <c>!foldl</c> expression by threading an accumulator across list elements.
    /// </summary>
    /// <param name="foldl">The fold expression syntax node.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <returns>The final accumulator value or a diagnostic.</returns>
    private EvaluationResult<Value> EvaluateFoldl(
        FoldlSyntax foldl,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var accValue = TryEvaluate(foldl.Init, scope, tryResolveValue);
        if (!accValue.IsSuccess)
        {
            return Failure(accValue.Diagnostic!);
        }

        var listValue = TryEvaluate(foldl.List, scope, tryResolveValue);
        if (!listValue.IsSuccess)
        {
            return Failure(listValue.Diagnostic!);
        }

        if (listValue.Value is not ListValue list)
        {
            return Failure(InvalidCast("Expected a list value."));
        }

        var current = accValue.Value;
        foreach (var item in list.Items)
        {
            // Each iteration extends the outer scope with the current accumulator and list element bindings.
            var innerScope = scope.With(foldl.AccVar, current).With(foldl.CurVar, item);
            var body = TryEvaluate(foldl.Body, innerScope, tryResolveValue);
            if (!body.IsSuccess)
            {
                return Failure(body.Diagnostic!);
            }

            current = body.Value;
        }

        return Success(current);
    }

    /// <summary>
    /// Evaluates a class instantiation expression and extracts a single field from the resulting instance.
    /// </summary>
    /// <param name="instantiation">The class instantiation syntax node.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <returns>The requested field value or a diagnostic.</returns>
    private EvaluationResult<Value> EvaluateClassInstantiation(
        ClassInstantiationSyntax instantiation,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        if (!context.Classes.TryGetValue(instantiation.ClassName, out var classSyntax))
        {
            return Failure(MissingKey($"Unknown TableGen class '{instantiation.ClassName}'."));
        }

        var record = instantiateClass(classSyntax, instantiation.Arguments, scope, tryResolveValue);
        if (!record.IsSuccess)
        {
            return Failure(record.Diagnostic!);
        }

        return record.Value.Fields.TryGetValue(instantiation.FieldName, out var fieldValue)
            ? Success(fieldValue)
            : Failure(MissingKey($"Class '{instantiation.ClassName}' has no field '{instantiation.FieldName}'."));
    }

    /// <summary>
    /// Evaluates an anonymous class instantiation expression into an in-memory record value.
    /// </summary>
    /// <param name="inst">The anonymous instantiation syntax node.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <returns>The anonymous record value or a diagnostic.</returns>
    private EvaluationResult<Value> EvaluateAnonymousClassInstantiation(
        AnonymousClassInstantiationSyntax inst,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        if (!context.Classes.TryGetValue(inst.ClassName, out var classSyntax))
        {
            return Failure(MissingKey($"Unknown TableGen class '{inst.ClassName}'."));
        }

        var record = instantiateClass(classSyntax, inst.Arguments, scope, tryResolveValue);
        return record.IsSuccess
            ? Success(record.Value)
            : Failure(record.Diagnostic!);
    }

    /// <summary>
    /// Evaluates a field access on either an anonymous record value or a top-level record reference.
    /// </summary>
    /// <param name="fieldAccess">The field-access syntax node.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <returns>The field value when present, otherwise <see cref="UnsetValue"/>.</returns>
    private EvaluationResult<Value> EvaluateFieldAccess(
        FieldAccessSyntax fieldAccess,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var obj = TryEvaluate(fieldAccess.Object, scope, tryResolveValue);
        if (!obj.IsSuccess)
        {
            return Failure(obj.Diagnostic!);
        }

        if (obj.Value is AnonymousRecordValue rec)
        {
            return Success(rec.Fields.TryGetValue(fieldAccess.FieldName, out var fv) ? fv : new UnsetValue());
        }

        if (obj.Value is RecordReferenceValue recRef && context.DefinitionsByName.TryGetValue(recRef.RecordName, out var defSyntax))
        {
            var record = buildDefinition(defSyntax);
            if (!record.IsSuccess)
            {
                return Failure(record.Diagnostic!);
            }

            return Success(record.Value.Fields.TryGetValue(fieldAccess.FieldName, out var fieldVal) ? fieldVal : new UnsetValue());
        }

        return Success(new UnsetValue());
    }

    /// <summary>
    /// Evaluates list and string subscripts, including TableGen's negative-index behavior.
    /// </summary>
    /// <param name="subscript">The subscript syntax node.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <returns>The indexed value or a diagnostic.</returns>
    private EvaluationResult<Value> EvaluateSubscript(
        SubscriptSyntax subscript,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var target = TryEvaluate(subscript.Target, scope, tryResolveValue);
        if (!target.IsSuccess)
        {
            return Failure(target.Diagnostic!);
        }

        var index = TryEvaluateInteger(subscript.Index, scope, tryResolveValue, "subscript");
        if (!index.IsSuccess)
        {
            return Failure(index.Diagnostic!);
        }

        switch (target.Value)
        {
            case ListValue list:
            {
                var normalized = ValueUtilities.TryNormalizeIndex(index.Value, list.Items.Count, "list subscript");
                return normalized.IsSuccess
                    ? Success(list.Items[normalized.Value])
                    : Failure(normalized.Diagnostic!);
            }
            case StringValue str:
            {
                var normalized = ValueUtilities.TryNormalizeIndex(index.Value, str.Value.Length, "string subscript");
                return normalized.IsSuccess
                    ? Success(new StringValue(str.Value[normalized.Value].ToString()))
                    : Failure(normalized.Diagnostic!);
            }
            default:
                return Failure(InvalidOperation($"Cannot subscript {target.Value.GetType().Name}."));
        }
    }

    /// <summary>
    /// Evaluates a <c>!foreach</c> expression by mapping the body across each list element.
    /// </summary>
    /// <param name="forEach">The foreach syntax node.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <returns>The resulting list value or a diagnostic.</returns>
    private EvaluationResult<Value> EvaluateForeach(
        ForeachSyntax forEach,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        var listValue = TryEvaluate(forEach.List, scope, tryResolveValue);
        if (!listValue.IsSuccess)
        {
            return Failure(listValue.Diagnostic!);
        }

        if (listValue.Value is not ListValue list)
        {
            return Failure(InvalidCast("Expected a list value."));
        }

        var results = new List<Value>(list.Items.Count);
        foreach (var item in list.Items)
        {
            var innerScope = scope.With(forEach.VarName, item);
            var body = TryEvaluate(forEach.Body, innerScope, tryResolveValue);
            if (!body.IsSuccess)
            {
                return Failure(body.Diagnostic!);
            }

            results.Add(body.Value);
        }

        return Success(new ListValue(results));
    }

    /// <summary>
    /// Evaluates an expression and then requires the result to be an integer.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">Optional deferred resolver for field references.</param>
    /// <param name="contextName">A human-readable operator name for diagnostics.</param>
    /// <returns>The integer value or a diagnostic.</returns>
    private EvaluationResult<int> TryEvaluateInteger(
        ExpressionSyntax expression,
        Scope scope,
        TryResolveValue? tryResolveValue,
        string contextName)
    {
        var value = TryEvaluate(expression, scope, tryResolveValue);
        return !value.IsSuccess
            ? EvaluationResult<int>.Failure(value.Diagnostic!)
            : ValueUtilities.TryToInteger(value.Value, contextName);
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

    /// <summary>
    /// Creates an invalid-cast diagnostic with a consistent helper call site.
    /// </summary>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>The constructed diagnostic.</returns>
    private static EvaluationDiagnostic InvalidCast(string message)
    {
        return new EvaluationDiagnostic(EvaluationDiagnosticKind.InvalidCast, message);
    }

    /// <summary>
    /// Creates a successful value result.
    /// </summary>
    /// <param name="value">The computed value.</param>
    /// <returns>A successful evaluation result.</returns>
    private static EvaluationResult<Value> Success(Value value)
    {
        return EvaluationResult<Value>.Success(value);
    }

    /// <summary>
    /// Creates a failed value result.
    /// </summary>
    /// <param name="diagnostic">The diagnostic describing the failure.</param>
    /// <returns>A failed evaluation result.</returns>
    private static EvaluationResult<Value> Failure(EvaluationDiagnostic diagnostic)
    {
        return EvaluationResult<Value>.Failure(diagnostic);
    }
}
