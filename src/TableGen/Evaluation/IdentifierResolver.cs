namespace TableGen.Evaluation;

using System.Linq;

using TableGen.Syntax;

internal sealed class IdentifierResolver(EvaluationContext context)
{
    public EvaluationResult<Value> ResolveIdentifier(
        string name,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        if (scope.TryGetValue(name, out var value))
        {
            return EvaluationResult<Value>.Success(value);
        }

        if (tryResolveValue != null)
        {
            var resolved = tryResolveValue(name);
            if (resolved.IsSuccess)
            {
                return resolved;
            }
        }

        if (context.DefvarValues.TryGetValue(name, out value))
        {
            return EvaluationResult<Value>.Success(value);
        }

        if (name == "true")
        {
            return EvaluationResult<Value>.Success(new BitValue(true));
        }

        if (name == "false")
        {
            return EvaluationResult<Value>.Success(new BitValue(false));
        }

        if (context.DefinitionsByName.ContainsKey(name))
        {
            return EvaluationResult<Value>.Success(new RecordReferenceValue(name));
        }

        return EvaluationResult<Value>.Success(new SymbolReferenceValue(name));
    }

    public bool IsValueOfType(Value value, string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        var nonNullTypeName = typeName!;
        return value switch
        {
            AnonymousRecordValue record => ClassIsA(record.ClassName, nonNullTypeName),
            RecordReferenceValue recordReference => context.DefinitionsByName.TryGetValue(recordReference.RecordName, out var definition)
                && definition.Bases.Any(@base => ClassIsA(@base.Name, nonNullTypeName)),
            _ => false,
        };
    }

    private bool ClassIsA(string className, string typeName)
    {
        if (context.ClassIsACache.TryGetValue((className, typeName), out var cached))
        {
            return cached;
        }

        bool result;
        if (className == typeName)
        {
            result = true;
        }
        else
        {
            result = context.Classes.TryGetValue(className, out var classSyntax)
                && classSyntax.Bases.Any(@base => ClassIsA(@base.Name, typeName));
        }

        context.ClassIsACache[(className, typeName)] = result;
        return result;
    }
}
