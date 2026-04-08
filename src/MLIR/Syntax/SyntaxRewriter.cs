namespace MLIR.Syntax;

using System.Collections.Generic;

/// <summary>
/// Rewrites concrete syntax trees while preserving their logical shape.
/// </summary>
public class SyntaxRewriter
{
    /// <summary>
    /// Rewrites a syntax node.
    /// </summary>
    public virtual TNode Visit<TNode>(TNode node)
        where TNode : SyntaxNode
    {
        return (TNode)node.Rewrite(this);
    }

    /// <summary>
    /// Rewrites a token.
    /// </summary>
    public virtual Token VisitToken(Token token)
    {
        return token;
    }

    /// <summary>
    /// Rewrites an optional token.
    /// </summary>
    public virtual Token? VisitToken(Token? token)
    {
        return token.HasValue ? VisitToken(token.Value) : null;
    }

    /// <summary>
    /// Rewrites a raw syntax text fragment.
    /// </summary>
    public virtual RawSyntaxText VisitRawText(RawSyntaxText rawText)
    {
        return new RawSyntaxText(VisitTokenList(rawText.Tokens), rawText.Text);
    }

    /// <summary>
    /// Rewrites a list of syntax nodes.
    /// </summary>
    public virtual IReadOnlyList<TNode> VisitList<TNode>(IReadOnlyList<TNode> nodes)
        where TNode : SyntaxNode
    {
        if (nodes.Count == 0)
        {
            return nodes;
        }

        var result = new List<TNode>(nodes.Count);
        foreach (var node in nodes)
        {
            result.Add(Visit(node));
        }

        return result;
    }

    /// <summary>
    /// Rewrites a token list.
    /// </summary>
    public virtual IReadOnlyList<Token> VisitTokenList(IReadOnlyList<Token> tokens)
    {
        if (tokens.Count == 0)
        {
            return tokens;
        }

        var result = new List<Token>(tokens.Count);
        foreach (var token in tokens)
        {
            result.Add(VisitToken(token));
        }

        return result;
    }

    /// <summary>
    /// Rewrites a list of raw syntax text fragments.
    /// </summary>
    public virtual IReadOnlyList<RawSyntaxText> VisitRawTextList(IReadOnlyList<RawSyntaxText> rawTexts)
    {
        if (rawTexts.Count == 0)
        {
            return rawTexts;
        }

        var result = new List<RawSyntaxText>(rawTexts.Count);
        foreach (var rawText in rawTexts)
        {
            result.Add(VisitRawText(rawText));
        }

        return result;
    }

    /// <summary>
    /// Rewrites a separated list of tokens.
    /// </summary>
    public virtual SeparatedSyntaxList<Token> VisitSeparatedTokenList(SeparatedSyntaxList<Token> list)
    {
        return new SeparatedSyntaxList<Token>(
            VisitTokenList(list.Items),
            VisitTokenList(list.SeparatorTokens));
    }

    /// <summary>
    /// Rewrites a separated list of syntax nodes.
    /// </summary>
    public virtual SeparatedSyntaxList<TNode> VisitSeparatedList<TNode>(SeparatedSyntaxList<TNode> list)
        where TNode : SyntaxNode
    {
        if (list.Count == 0)
        {
            return list;
        }

        var items = new List<TNode>(list.Count);
        foreach (var node in list.Items)
        {
            items.Add(Visit(node));
        }

        return new SeparatedSyntaxList<TNode>(items, VisitTokenList(list.SeparatorTokens));
    }

    /// <summary>
    /// Rewrites a delimited list of syntax nodes.
    /// </summary>
    public virtual DelimitedSyntaxList<TNode> VisitDelimitedList<TNode>(DelimitedSyntaxList<TNode> list)
        where TNode : SyntaxNode
    {
        if (!list.OpenToken.HasValue)
        {
            return list;
        }

        return new DelimitedSyntaxList<TNode>(
            VisitToken(list.OpenToken),
            VisitList(list.Items),
            VisitTokenList(list.SeparatorTokens),
            VisitToken(list.CloseToken));
    }

    /// <summary>
    /// Rewrites a delimited list of tokens.
    /// </summary>
    public virtual DelimitedSyntaxList<Token> VisitDelimitedTokenList(DelimitedSyntaxList<Token> list)
    {
        if (!list.OpenToken.HasValue)
        {
            return list;
        }

        return new DelimitedSyntaxList<Token>(
            VisitToken(list.OpenToken),
            VisitTokenList(list.Items),
            VisitTokenList(list.SeparatorTokens),
            VisitToken(list.CloseToken));
    }
}
