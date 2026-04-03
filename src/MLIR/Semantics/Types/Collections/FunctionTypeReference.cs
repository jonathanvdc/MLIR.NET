using System.Linq;
using MLIR.Dialects;
using MLIR.Syntax;
using MLIR.Syntax.Types.Collections;

namespace MLIR.Semantics.Types.Collections;

/// <summary>
/// Represents a builtin function type.
/// </summary>
public sealed class FunctionTypeReference : TypeReference
{
    /// <summary>
    /// Gets the shared builtin type definition.
    /// </summary>
    public static TypeDefinition TypeDefinition { get; } = new("function");

    /// <summary>
    /// Initializes a new parsed builtin function type reference.
    /// </summary>
    public FunctionTypeReference(FunctionTypeSyntax syntax, IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results)
        : this(inputs, results, syntax, syntax.Location)
    {
    }

    /// <summary>
    /// Initializes a new synthetic builtin function type reference.
    /// </summary>
    public FunctionTypeReference(IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results)
        : this(inputs, results, null, SourceLocation.Unknown)
    {
    }

    /// <summary>
    /// Gets the input types.
    /// </summary>
    public IReadOnlyList<TypeReference> Inputs { get; }

    /// <summary>
    /// Gets the result types.
    /// </summary>
    public IReadOnlyList<TypeReference> Results { get; }

    /// <inheritdoc/>
    public override string? Name => "function";

    /// <inheritdoc/>
    public override TypeDefinition? Definition => TypeDefinition;

    private FunctionTypeReference(IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results, TypeSyntax? syntax, SourceLocation location)
        : base(syntax ?? BuildSyntax(inputs, results), location)
    {
        Inputs = inputs;
        Results = results;
    }

    private static FunctionTypeSyntax BuildSyntax(IReadOnlyList<TypeReference> inputs, IReadOnlyList<TypeReference> results)
    {
        var inputCommas = new List<SyntaxToken>(Math.Max(0, inputs.Count - 1));
        for (var i = 1; i < inputs.Count; i++)
        {
            inputCommas.Add(new SyntaxToken(","));
        }

        if (results.Count == 1)
        {
            return new FunctionTypeSyntax(
                new DelimitedSyntaxList<TypeSyntax>(new SyntaxToken("("), inputs.Select(GetSyntax).ToArray(), inputCommas, new SyntaxToken(")")),
                new SyntaxToken("->"),
                GetSyntax(results[0]),
                new DelimitedSyntaxList<TypeSyntax>(null, [], [], null));
        }

        var resultCommas = new List<SyntaxToken>(Math.Max(0, results.Count - 1));
        for (var i = 1; i < results.Count; i++)
        {
            resultCommas.Add(new SyntaxToken(","));
        }

        return new FunctionTypeSyntax(
            new DelimitedSyntaxList<TypeSyntax>(new SyntaxToken("("), inputs.Select(GetSyntax).ToArray(), inputCommas, new SyntaxToken(")")),
            new SyntaxToken("->"),
            null,
            new DelimitedSyntaxList<TypeSyntax>(new SyntaxToken("("), results.Select(GetSyntax).ToArray(), resultCommas, new SyntaxToken(")")));
    }

    private static TypeSyntax GetSyntax(TypeReference type)
    {
        return type.Syntax ?? throw new InvalidOperationException("Function operand types must carry syntax.");
    }
}
