namespace MLIR.Syntax.Attributes;

using System.Collections;
using System.Collections.Generic;
using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Represents one nested component of a symbol-reference attribute, including
/// the two <c>:</c> tokens and the following symbol-name token.
/// </summary>
public readonly struct SymbolRefNestedReferenceSyntax : IHasSourceLocation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolRefNestedReferenceSyntax"/> struct.
    /// </summary>
    /// <param name="firstColonToken">The first <c>:</c> token.</param>
    /// <param name="secondColonToken">The second <c>:</c> token.</param>
    /// <param name="symbolNameToken">The nested symbol-name token.</param>
    public SymbolRefNestedReferenceSyntax(Token firstColonToken, Token secondColonToken, Token symbolNameToken)
    {
        if (symbolNameToken.TokenKind != TokenKind.SymbolName)
        {
            throw new System.ArgumentException("Nested symbol reference token must be a symbol-name token.", nameof(symbolNameToken));
        }

        FirstColonToken = firstColonToken;
        SecondColonToken = secondColonToken;
        SymbolNameToken = symbolNameToken;
    }

    /// <summary>
    /// Gets the first <c>:</c> token.
    /// </summary>
    public Token FirstColonToken { get; }

    /// <summary>
    /// Gets the second <c>:</c> token.
    /// </summary>
    public Token SecondColonToken { get; }

    /// <summary>
    /// Gets the nested symbol-name token.
    /// </summary>
    public Token SymbolNameToken { get; }

    /// <inheritdoc/>
    public SourceLocation Location => SourceLocation.Merge(FirstColonToken.Location, SymbolNameToken.Location);

    /// <summary>
    /// Writes this nested reference to the supplied syntax writer.
    /// </summary>
    public void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(FirstColonToken);
        writer.WriteToken(SecondColonToken);
        writer.WriteToken(SymbolNameToken);
    }

    /// <summary>
    /// Rewrites this nested reference with the supplied syntax rewriter.
    /// </summary>
    public SymbolRefNestedReferenceSyntax Rewrite(SyntaxRewriter rewriter)
    {
        return new SymbolRefNestedReferenceSyntax(
            rewriter.VisitToken(FirstColonToken),
            rewriter.VisitToken(SecondColonToken),
            rewriter.VisitToken(SymbolNameToken));
    }
}

/// <summary>
/// Represents a builtin symbol-reference attribute literal such as <c>@foo</c> or
/// <c>@parent::@child</c>.
/// </summary>
/// <remarks>
/// MLIR lexes <c>::</c> as two colon tokens in this runtime, so nested references
/// preserve both colon tokens while each <c>@name</c> component is represented as
/// a single <see cref="TokenKind.SymbolName"/> token.
/// </remarks>
public sealed class SymbolRefAttributeValueSyntax : AttributeValueSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolRefAttributeValueSyntax"/> class.
    /// </summary>
    /// <param name="rootSymbolNameToken">The root symbol-name token.</param>
    /// <param name="nestedReferences">The nested symbol-reference components.</param>
    public SymbolRefAttributeValueSyntax(
        Token rootSymbolNameToken,
        IReadOnlyList<SymbolRefNestedReferenceSyntax> nestedReferences)
    {
        if (rootSymbolNameToken.TokenKind != TokenKind.SymbolName)
        {
            throw new System.ArgumentException("Root symbol reference token must be a symbol-name token.", nameof(rootSymbolNameToken));
        }

        RootSymbolNameToken = rootSymbolNameToken;
        NestedReferences = nestedReferences;
        SymbolNameTokens = new SymbolNameTokenList(this);
    }

    /// <summary>
    /// Gets the root symbol-name token.
    /// </summary>
    public Token RootSymbolNameToken { get; }

    /// <summary>
    /// Gets the nested symbol-reference components.
    /// </summary>
    public IReadOnlyList<SymbolRefNestedReferenceSyntax> NestedReferences { get; }

    /// <summary>
    /// Gets a read-only token view of all symbol-name tokens in this reference.
    /// </summary>
    public IReadOnlyList<Token> SymbolNameTokens { get; }

    /// <summary>
    /// Gets the number of symbol-name components in this symbol reference.
    /// </summary>
    public int Count => 1 + NestedReferences.Count;

    /// <inheritdoc/>
    public override SourceLocation Location => RootSymbolNameToken.Location;

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(RootSymbolNameToken);
        for (var i = 0; i < NestedReferences.Count; i++)
        {
            NestedReferences[i].WriteTo(writer);
        }
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        var nestedReferences = new SymbolRefNestedReferenceSyntax[NestedReferences.Count];
        for (var i = 0; i < NestedReferences.Count; i++)
        {
            nestedReferences[i] = NestedReferences[i].Rewrite(rewriter);
        }

        return new SymbolRefAttributeValueSyntax(
            rewriter.VisitToken(RootSymbolNameToken),
            nestedReferences);
    }

    private sealed class SymbolNameTokenList(SymbolRefAttributeValueSyntax syntax) : IReadOnlyList<Token>
    {
        public int Count => syntax.Count;

        public Token this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return syntax.RootSymbolNameToken;
                }

                return syntax.NestedReferences[index - 1].SymbolNameToken;
            }
        }

        public IEnumerator<Token> GetEnumerator()
        {
            yield return syntax.RootSymbolNameToken;
            for (var i = 0; i < syntax.NestedReferences.Count; i++)
            {
                yield return syntax.NestedReferences[i].SymbolNameToken;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
