namespace TableGen.Evaluation;

using MLIR.Text;
using System.Linq;

using TableGen.Syntax;

/// <summary>
/// Resolves names and type relationships that depend on document-wide evaluation context.
/// </summary>
internal sealed class IdentifierResolver(EvaluationContext context)
{
    /// <summary>
    /// Resolves an identifier expression by consulting local scope, deferred field lookups, global defvars, and definitions.
    /// </summary>
    /// <param name="name">The identifier name as written in source.</param>
    /// <param name="scope">The current lexical scope.</param>
    /// <param name="tryResolveValue">An optional callback for lazy field resolution.</param>
    /// <returns>The resolved value or a symbolic/reference value when no concrete binding exists.</returns>
    public ParseResult<Value> ResolveIdentifier(
        string name,
        Scope scope,
        ExpressionEvaluator.TryResolveValue? tryResolveValue)
    {
        if (scope.TryGetValue(name, out var value))
        {
            return ParseResult<Value>.Success(value);
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
            return ParseResult<Value>.Success(value);
        }

        if (name == "true")
        {
            return ParseResult<Value>.Success(new BitValue(true));
        }

        if (name == "false")
        {
            return ParseResult<Value>.Success(new BitValue(false));
        }

        if (context.DefinitionsByName.ContainsKey(name))
        {
            return ParseResult<Value>.Success(new RecordReferenceValue(name));
        }

        return ParseResult<Value>.Success(new SymbolReferenceValue(name));
    }

    /// <summary>
    /// Determines whether a runtime value satisfies a TableGen class type used by <c>!isa</c>.
    /// </summary>
    /// <param name="value">The value to classify.</param>
    /// <param name="typeName">The queried TableGen type name.</param>
    /// <returns><see langword="true"/> when the value conforms to the queried class; otherwise <see langword="false"/>.</returns>
    public bool IsValueOfType(Value value, string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        var nonNullTypeName = typeName!;
        return value switch
        {
            RecordLikeValue record => record.HasBaseClass(nonNullTypeName),
            RecordReferenceValue recordReference => context.DefinitionsByName.TryGetValue(recordReference.RecordName, out var definition)
                && definition.Bases.Any(@base => ClassIsA(@base.Name, nonNullTypeName)),
            _ => false,
        };
    }

    /// <summary>
    /// Checks whether one class is transitively derived from another, with caching.
    /// </summary>
    /// <param name="className">The candidate derived class.</param>
    /// <param name="typeName">The target base class.</param>
    /// <returns><see langword="true"/> when <paramref name="className"/> is-a <paramref name="typeName"/>.</returns>
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
