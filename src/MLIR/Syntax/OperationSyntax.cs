namespace MLIR.Syntax;

using System.Collections.Generic;
using MLIR.Text;

/// <summary>
/// Represents an MLIR operation.
/// </summary>
public sealed class OperationSyntax
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
        RawSyntaxText? typeSignature)
        : this(
            CreateValueTokens(results),
            CreateDefaultCommaTokens(results.Count),
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
                typeSignature != null ? new RawTypeSyntax(typeSignature) : null))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationSyntax"/> class.
    /// </summary>
    /// <param name="resultTokens">The SSA result tokens produced by the operation.</param>
    /// <param name="resultCommaTokens">The comma tokens between results.</param>
    /// <param name="equalsToken">The equals token, if present.</param>
    /// <param name="nameToken">The operation name token.</param>
    /// <param name="body">The operation body.</param>
    public OperationSyntax(
        IReadOnlyList<SyntaxToken> resultTokens,
        IReadOnlyList<SyntaxToken> resultCommaTokens,
        SyntaxToken? equalsToken,
        SyntaxToken nameToken,
        OperationBodySyntax body)
    {
        ResultTokens = resultTokens;
        ResultCommaTokens = resultCommaTokens;
        EqualsToken = equalsToken;
        NameToken = nameToken;
        Body = body;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationSyntax"/> class.
    /// </summary>
    /// <param name="resultTokens">The SSA result tokens produced by the operation.</param>
    /// <param name="resultCommaTokens">The comma tokens between results.</param>
    /// <param name="equalsToken">The equals token, if present.</param>
    /// <param name="nameToken">The operation name token.</param>
    /// <param name="operandList">The delimited operand list.</param>
    /// <param name="successorList">The delimited successor list.</param>
    /// <param name="regions">The regions nested under the operation.</param>
    /// <param name="attributes">The delimited attribute dictionary.</param>
    /// <param name="typeSignatureColonToken">The colon token that introduces the type signature, if present.</param>
    /// <param name="typeSignature">The raw trailing type signature, if present.</param>
    public OperationSyntax(
        IReadOnlyList<SyntaxToken> resultTokens,
        IReadOnlyList<SyntaxToken> resultCommaTokens,
        SyntaxToken? equalsToken,
        SyntaxToken nameToken,
        DelimitedSyntaxList<SyntaxToken> operandList,
        DelimitedSyntaxList<SyntaxToken> successorList,
        IReadOnlyList<RegionSyntax> regions,
        DelimitedSyntaxList<NamedAttributeSyntax> attributes,
        SyntaxToken? typeSignatureColonToken,
        RawSyntaxText? typeSignature)
        : this(
            resultTokens,
            resultCommaTokens,
            equalsToken,
            nameToken,
            new GenericOperationBodySyntax(
                operandList,
                successorList,
                regions,
                attributes,
                typeSignatureColonToken,
                typeSignature != null ? new RawTypeSyntax(typeSignature) : null))
    {
    }

    /// <summary>
    /// Gets the SSA result tokens.
    /// </summary>
    public IReadOnlyList<SyntaxToken> ResultTokens { get; }

    /// <summary>
    /// Gets the comma tokens between results.
    /// </summary>
    public IReadOnlyList<SyntaxToken> ResultCommaTokens { get; }

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
    /// Attempts to get the operation body as a generic MLIR body.
    /// </summary>
    public bool TryGetGenericBody(out GenericOperationBodySyntax? genericBody)
    {
        return Body.TryGetGenericBody(out genericBody);
    }

    /// <summary>
    /// Gets the operation body as a generic MLIR body.
    /// </summary>
    public GenericOperationBodySyntax GenericBody => Body.GetGenericBody();

    /// <summary>
    /// Gets the delimited operand list.
    /// </summary>
    public DelimitedSyntaxList<SyntaxToken> OperandList => GenericBody.OperandList;

    /// <summary>
    /// Gets the delimited successor list.
    /// </summary>
    public DelimitedSyntaxList<SyntaxToken> SuccessorList => GenericBody.SuccessorList;

    /// <summary>
    /// Gets the regions nested under the operation.
    /// </summary>
    public IReadOnlyList<RegionSyntax> Regions => GenericBody.Regions;

    /// <summary>
    /// Gets the delimited attribute dictionary.
    /// </summary>
    public DelimitedSyntaxList<NamedAttributeSyntax> Attributes => GenericBody.Attributes;

    /// <summary>
    /// Gets the colon token that introduces the type signature, if present.
    /// </summary>
    public SyntaxToken? TypeSignatureColonToken => GenericBody.TypeSignatureColonToken;

    /// <summary>
    /// Gets the trailing type signature syntax, if present.
    /// </summary>
    public TypeSyntax? TypeSignatureSyntax => GenericBody.TypeSignatureSyntax;

    /// <summary>
    /// Attempts to get the trailing type signature as raw syntax text.
    /// </summary>
    public bool TryGetRawTypeSignature(out RawSyntaxText? rawTypeSignature)
    {
        return GenericBody.TryGetRawTypeSignature(out rawTypeSignature);
    }

    /// <summary>
    /// Gets the trailing type signature as raw syntax text.
    /// </summary>
    public RawSyntaxText? RawTypeSignature => GenericBody.RawTypeSignature;

    /// <summary>
    /// Gets the SSA results produced by the operation.
    /// </summary>
    public IReadOnlyList<string> Results => GetTexts(ResultTokens);

    /// <summary>
    /// Gets the operation name as written in the source.
    /// </summary>
    public string Name => NameToken.Text;

    /// <summary>
    /// Gets the SSA operands passed to the operation.
    /// </summary>
    public IReadOnlyList<string> Operands => GetTexts(OperandList.Items);

    /// <summary>
    /// Gets the successor block labels referenced by the operation.
    /// </summary>
    public IReadOnlyList<string> Successors => GetTexts(SuccessorList.Items);

    /// <summary>
    /// Gets a value indicating whether the operation uses a custom assembly body.
    /// </summary>
    public bool HasCustomAssemblyBody => Body is not GenericOperationBodySyntax;

    /// <summary>
    /// Writes this operation to the supplied syntax writer.
    /// </summary>
    /// <param name="writer">The syntax writer to write to.</param>
    /// <param name="indentLevel">The indentation level to use when indentation is synthesized.</param>
    /// <param name="defaultLeadingTrivia">The fallback leading trivia to use when syntax does not carry explicit trivia.</param>
    public void WriteTo(
        SyntaxWriter writer,
        int indentLevel,
        string defaultLeadingTrivia)
    {
        if (ResultTokens.Count > 0)
        {
            for (var i = 0; i < ResultTokens.Count; i++)
            {
                if (i > 0)
                {
                    writer.WriteToken(ResultCommaTokens[i - 1], string.Empty);
                }

                writer.WriteToken(ResultTokens[i], i > 0 ? " " : defaultLeadingTrivia, i == 0 ? indentLevel : null);
            }

            writer.WriteToken(EqualsToken!.Value, " ");
            writer.WriteToken(NameToken, " ");
        }
        else
        {
            writer.WriteToken(NameToken, defaultLeadingTrivia, indentLevel);
        }

        Body.WriteTo(writer, indentLevel);
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

    private static IReadOnlyList<string> GetTexts(IReadOnlyList<SyntaxToken> tokens)
    {
        var values = new List<string>();
        foreach (var token in tokens)
        {
            values.Add(token.Text);
        }

        return values;
    }
}
