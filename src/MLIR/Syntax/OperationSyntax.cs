namespace MLIR.Syntax;

using System.Collections.Generic;
using MLIR.Text;
using MLIR.Semantics;

/// <summary>
/// Represents an MLIR operation.
/// </summary>
public sealed class OperationSyntax : SyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationSyntax"/> class.
    /// </summary>
    /// <param name="results">The SSA results produced by the operation.</param>
    /// <param name="name">The operation name as written in the source.</param>
    /// <param name="operands">The SSA operands passed to the operation.</param>
    /// <param name="successors">The successor block labels referenced by the operation.</param>
    /// <param name="regions">The regions nested under the operation.</param>
    /// <param name="attributes">The named attributes attached to the operation.</param>
    /// <param name="typeSignature">The raw trailing type signature, if present.</param>
    public OperationSyntax(
        IReadOnlyList<string> results,
        string name,
        IReadOnlyList<string> operands,
        IReadOnlyList<string> successors,
        IReadOnlyList<RegionSyntax> regions,
        IReadOnlyList<NamedAttributeSyntax> attributes,
        TypeSyntax? typeSignature)
        : this(
            new SeparatedSyntaxList<SyntaxToken>(
                CreateValueTokens(results),
                CreateDefaultCommaTokens(results.Count)),
            results.Count > 0 ? new SyntaxToken("=") : null,
            new SyntaxToken(name),
            new GenericOperationBodySyntax(
                new DelimitedSyntaxList<SyntaxToken>(
                    new SyntaxToken("("),
                    CreateValueTokens(operands),
                    CreateDefaultCommaTokens(operands.Count),
                    new SyntaxToken(")")),
                new DelimitedSyntaxList<SyntaxToken>(
                    successors.Count > 0 ? new SyntaxToken("[") : null,
                    CreateValueTokens(successors),
                    CreateDefaultCommaTokens(successors.Count),
                    successors.Count > 0 ? new SyntaxToken("]") : null),
                regions,
                new DelimitedSyntaxList<NamedAttributeSyntax>(
                    attributes.Count > 0 ? new SyntaxToken("{") : null,
                    attributes,
                    CreateDefaultCommaTokens(attributes.Count),
                    attributes.Count > 0 ? new SyntaxToken("}") : null),
                typeSignature != null ? new SyntaxToken(":") : null,
                typeSignature != null ? typeSignature : null))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationSyntax"/> class.
    /// </summary>
    /// <param name="resultList">The SSA result tokens produced by the operation, with their separator tokens.</param>
    /// <param name="equalsToken">The equals token, if present.</param>
    /// <param name="nameToken">The operation name token.</param>
    /// <param name="body">The operation body.</param>
    public OperationSyntax(
        SeparatedSyntaxList<SyntaxToken> resultList,
        SyntaxToken? equalsToken,
        SyntaxToken nameToken,
        OperationBodySyntax body)
    {
        ResultList = resultList;
        EqualsToken = equalsToken;
        NameToken = nameToken;
        Body = body;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationSyntax"/> class.
    /// </summary>
    /// <param name="resultList">The SSA result tokens produced by the operation, with their separator tokens.</param>
    /// <param name="equalsToken">The equals token, if present.</param>
    /// <param name="nameToken">The operation name token.</param>
    /// <param name="operandList">The delimited operand list.</param>
    /// <param name="successorList">The delimited successor list.</param>
    /// <param name="regions">The regions nested under the operation.</param>
    /// <param name="attributes">The delimited attribute dictionary.</param>
    /// <param name="typeSignatureColonToken">The colon token that introduces the type signature, if present.</param>
    /// <param name="typeSignature">The raw trailing type signature, if present.</param>
    public OperationSyntax(
        SeparatedSyntaxList<SyntaxToken> resultList,
        SyntaxToken? equalsToken,
        SyntaxToken nameToken,
        DelimitedSyntaxList<SyntaxToken> operandList,
        DelimitedSyntaxList<SyntaxToken> successorList,
        IReadOnlyList<RegionSyntax> regions,
        DelimitedSyntaxList<NamedAttributeSyntax> attributes,
        SyntaxToken? typeSignatureColonToken,
        TypeSyntax? typeSignature)
        : this(
            resultList,
            equalsToken,
            nameToken,
            new GenericOperationBodySyntax(
                operandList,
                successorList,
                regions,
                attributes,
                typeSignatureColonToken,
                typeSignature != null ? typeSignature : null))
    {
    }

    /// <summary>
    /// Gets the SSA result list, containing both the result tokens and the comma tokens between them.
    /// </summary>
    public SeparatedSyntaxList<SyntaxToken> ResultList { get; }

    /// <summary>
    /// Gets the equals token, if present.
    /// </summary>
    public SyntaxToken? EqualsToken { get; }

    /// <summary>
    /// Gets the operation name token.
    /// </summary>
    public SyntaxToken NameToken { get; }

    /// <summary>
    /// Gets the operation body.
    /// </summary>
    public OperationBodySyntax Body { get; }

    /// <summary>
    /// Gets the SSA results produced by the operation.
    /// </summary>
    public IReadOnlyList<string> Results => GetTexts(ResultList);

    /// <summary>
    /// Gets the operation name as written in the source.
    /// </summary>
    public string Name => NameToken.Text;

    /// <summary>
    /// Gets a value indicating whether the operation uses a custom assembly body.
    /// </summary>
    public bool HasCustomAssemblyBody => Body is not GenericOperationBodySyntax;

    /// <summary>
    /// Gets the merged source location spanning the entire operation, from the first result
    /// token (or the name token when there are no results) through to the end of the body.
    /// Returns an unknown location when no source-backed tokens are present.
    /// </summary>
    public override SourceLocation Location
    {
        get
        {
            // Start at the first result token when results are present; fall back to the
            // name token for operations that produce no results.
            var result = ResultList.Count > 0
                ? SourceLocation.Merge(ResultList[0].Location, NameToken.Location)
                : NameToken.Location;
            result = SourceLocation.Merge(result, Body.Location);
            return result;
        }
    }

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        if (ResultList.Count > 0)
        {
            // The pending trivia (if any) is consumed by the first result token.
            // Subsequent result tokens receive a space suggestion from SeparatedSyntaxList.
            ResultList.WriteTo(writer, static (token, w) => w.WriteToken(token));
            writer.WriteToken(EqualsToken!.Value, " ");
            writer.WriteToken(NameToken, " ");
        }
        else
        {
            writer.WriteToken(NameToken);
        }

        Body.WriteTo(writer);
    }

    private static IReadOnlyList<SyntaxToken> CreateValueTokens(IReadOnlyList<string> values)
    {
        var tokens = new List<SyntaxToken>();
        foreach (var value in values)
        {
            tokens.Add(new SyntaxToken(value));
        }

        return tokens;
    }

    private static IReadOnlyList<SyntaxToken> CreateDefaultCommaTokens(int count)
    {
        var separators = new List<SyntaxToken>();
        for (var i = 1; i < count; i++)
        {
            separators.Add(new SyntaxToken(","));
        }

        return separators;
    }

    private static IReadOnlyList<string> GetTexts(SeparatedSyntaxList<SyntaxToken> tokens)
    {
        var values = new List<string>();
        foreach (var token in tokens)
        {
            values.Add(token.Text);
        }

        return values;
    }
}
