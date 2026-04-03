namespace TableGen.Evaluation;

using System;
using System.Collections.Generic;

using TableGen.Syntax;

/// <summary>
/// Evaluates TableGen expressions against a scope and document-level context.
/// </summary>
internal sealed class ExpressionEvaluator
{
    private readonly EvaluationContext context;
    private readonly Func<ClassSyntax, IReadOnlyList<ExpressionSyntax>, Scope, TryResolveValue?, EvaluationResult<Dictionary<string, Value>>> instantiateClass;
    private readonly Func<DefSyntax, EvaluationResult<Record>> buildDefinition;
    private readonly IdentifierResolver identifierResolver;
    private readonly BangOperatorEvaluator bangOperatorEvaluator;

    internal delegate EvaluationResult<Value> TryResolveValue(string name);

    public ExpressionEvaluator(
        EvaluationContext context,
        Func<ClassSyntax, IReadOnlyList<ExpressionSyntax>, Scope, TryResolveValue?, EvaluationResult<Dictionary<string, Value>>> instantiateClass,
        Func<DefSyntax, EvaluationResult<Record>> buildDefinition)
    {
        this.context = context;
        this.instantiateClass = instantiateClass;
        this.buildDefinition = buildDefinition;
        identifierResolver = new IdentifierResolver(context);
        bangOperatorEvaluator = new BangOperatorEvaluator(context, identifierResolver, TryEvaluate);
    }

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

    private EvaluationResult<Value> EvaluateClassInstantiation(
        ClassInstantiationSyntax instantiation,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        if (!context.Classes.TryGetValue(instantiation.ClassName, out var classSyntax))
        {
            return Failure(MissingKey($"Unknown TableGen class '{instantiation.ClassName}'."));
        }

        var fields = instantiateClass(classSyntax, instantiation.Arguments, scope, tryResolveValue);
        if (!fields.IsSuccess)
        {
            return Failure(fields.Diagnostic!);
        }

        return fields.Value.TryGetValue(instantiation.FieldName, out var fieldValue)
            ? Success(fieldValue)
            : Failure(MissingKey($"Class '{instantiation.ClassName}' has no field '{instantiation.FieldName}'."));
    }

    private EvaluationResult<Value> EvaluateAnonymousClassInstantiation(
        AnonymousClassInstantiationSyntax inst,
        Scope scope,
        TryResolveValue? tryResolveValue)
    {
        if (!context.Classes.TryGetValue(inst.ClassName, out var classSyntax))
        {
            return Failure(MissingKey($"Unknown TableGen class '{inst.ClassName}'."));
        }

        var fields = instantiateClass(classSyntax, inst.Arguments, scope, tryResolveValue);
        return fields.IsSuccess
            ? Success(new AnonymousRecordValue(inst.ClassName, fields.Value))
            : Failure(fields.Diagnostic!);
    }

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

    private static EvaluationDiagnostic InvalidOperation(string message)
    {
        return new EvaluationDiagnostic(EvaluationDiagnosticKind.InvalidOperation, message);
    }

    private static EvaluationDiagnostic MissingKey(string message)
    {
        return new EvaluationDiagnostic(EvaluationDiagnosticKind.MissingKey, message);
    }

    private static EvaluationDiagnostic InvalidCast(string message)
    {
        return new EvaluationDiagnostic(EvaluationDiagnosticKind.InvalidCast, message);
    }

    private static EvaluationResult<Value> Success(Value value)
    {
        return EvaluationResult<Value>.Success(value);
    }

    private static EvaluationResult<Value> Failure(EvaluationDiagnostic diagnostic)
    {
        return EvaluationResult<Value>.Failure(diagnostic);
    }
}
