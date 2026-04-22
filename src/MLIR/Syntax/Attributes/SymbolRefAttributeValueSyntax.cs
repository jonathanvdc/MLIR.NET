namespace MLIR.Syntax.Attributes;

using System.Collections;
using System.Collections.Generic;
using MLIR.Semantics;
using MLIR.Text;

/// <summary>
/// Represents one component of a symbol-reference attribute, consisting of
/// an <c>@</c> token and its following symbol-name token.
/// </summary>
/// <param name="atToken">The <c>@</c> token.</param>
/// <param name="nameToken">The symbol-name token.</param>
public readonly struct SymbolRefAttributeComponentSyntax(Token atToken, Token nameToken) : IHasSourceLocation
{
    /// <summary>
    /// Gets the <c>@</c> token.
    /// </summary>
    public Token AtToken { get; } = atToken;

    /// <summary>
    /// Gets the symbol-name token.
    /// </summary>
    public Token NameToken { get; } = nameToken;

    /// <inheritdoc/>
    public SourceLocation Location => SourceLocation.Merge(AtToken.Location, NameToken.Location);

    /// <summary>
    /// Writes this component to the supplied syntax writer.
    /// </summary>
    public void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(AtToken);
        writer.WriteToken(NameToken);
    }

    /// <summary>
    /// Rewrites this component with the supplied syntax rewriter.
    /// </summary>
    public SymbolRefAttributeComponentSyntax Rewrite(SyntaxRewriter rewriter)
    {
        return new SymbolRefAttributeComponentSyntax(
            rewriter.VisitToken(AtToken),
            rewriter.VisitToken(NameToken));
    }
}

/// <summary>
/// Represents the <c>::</c> separator between nested symbol-reference components.
/// </summary>
/// <param name="firstColonToken">The first <c>:</c> token.</param>
/// <param name="secondColonToken">The second <c>:</c> token.</param>
public readonly struct SymbolRefAttributeSeparatorSyntax(Token firstColonToken, Token secondColonToken) : IHasSourceLocation
{
    /// <summary>
    /// Gets the first <c>:</c> token.
    /// </summary>
    public Token FirstColonToken { get; } = firstColonToken;

    /// <summary>
    /// Gets the second <c>:</c> token.
    /// </summary>
    public Token SecondColonToken { get; } = secondColonToken;

    /// <inheritdoc/>
    public SourceLocation Location => SourceLocation.Merge(FirstColonToken.Location, SecondColonToken.Location);

    /// <summary>
    /// Writes this separator to the supplied syntax writer.
    /// </summary>
    public void WriteTo(SyntaxWriter writer)
    {
        writer.WriteToken(FirstColonToken);
        writer.WriteToken(SecondColonToken);
    }

    /// <summary>
    /// Rewrites this separator with the supplied syntax rewriter.
    /// </summary>
    public SymbolRefAttributeSeparatorSyntax Rewrite(SyntaxRewriter rewriter)
    {
        return new SymbolRefAttributeSeparatorSyntax(
            rewriter.VisitToken(FirstColonToken),
            rewriter.VisitToken(SecondColonToken));
    }
}

/// <summary>
/// Represents a builtin symbol-reference attribute literal such as <c>@foo</c> or
/// <c>@parent::@child</c>.
/// </summary>
/// <remarks>
/// MLIR lexes <c>::</c> as two colon tokens in this runtime, so nested-reference
/// separators are represented as structured two-token separator values.
/// The semantic name values are intentionally not stored here; binding derives them
/// from the preserved name tokens so parsed syntax remains the source of truth.
/// </remarks>
public sealed class SymbolRefAttributeValueSyntax : AttributeValueSyntax
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SymbolRefAttributeValueSyntax"/> class.
    /// </summary>
    /// <param name="components">The symbol-reference components.</param>
    /// <param name="separators">The nested-reference <c>::</c> separators.</param>
    public SymbolRefAttributeValueSyntax(
        IReadOnlyList<SymbolRefAttributeComponentSyntax> components,
        IReadOnlyList<SymbolRefAttributeSeparatorSyntax> separators)
    {
        if (components.Count == 0)
        {
            throw new System.ArgumentException("A symbol reference must contain at least one component.", nameof(components));
        }

        if (separators.Count != components.Count - 1)
        {
            throw new System.ArgumentException("A symbol reference must contain exactly one separator per nested reference.", nameof(separators));
        }

        Components = components;
        Separators = separators;
        NameTokens = new NameTokenList(components);
    }

    /// <summary>
    /// Gets the symbol-reference components.
    /// </summary>
    public IReadOnlyList<SymbolRefAttributeComponentSyntax> Components { get; }

    /// <summary>
    /// Gets the nested-reference <c>::</c> separators.
    /// </summary>
    public IReadOnlyList<SymbolRefAttributeSeparatorSyntax> Separators { get; }

    /// <summary>
    /// Gets a read-only token view of the symbol names in this reference.
    /// </summary>
    public IReadOnlyList<Token> NameTokens { get; }

    /// <summary>
    /// Gets the number of reference components in this symbol reference.
    /// </summary>
    public int Count => Components.Count;

    /// <inheritdoc/>
    public override SourceLocation Location => Components[0].Location;

    /// <inheritdoc/>
    public override void WriteTo(SyntaxWriter writer)
    {
        Components[0].WriteTo(writer);
        for (var i = 1; i < Components.Count; i++)
        {
            Separators[i - 1].WriteTo(writer);
            Components[i].WriteTo(writer);
        }
    }

    /// <inheritdoc/>
    public override SyntaxNode Rewrite(SyntaxRewriter rewriter)
    {
        var components = new SymbolRefAttributeComponentSyntax[Components.Count];
        for (var i = 0; i < Components.Count; i++)
        {
            components[i] = Components[i].Rewrite(rewriter);
        }

        var separators = new SymbolRefAttributeSeparatorSyntax[Separators.Count];
        for (var i = 0; i < Separators.Count; i++)
        {
            separators[i] = Separators[i].Rewrite(rewriter);
        }

        return new SymbolRefAttributeValueSyntax(components, separators);
    }

    private sealed class NameTokenList(IReadOnlyList<SymbolRefAttributeComponentSyntax> components) : IReadOnlyList<Token>
    {
        public int Count => components.Count;

        public Token this[int index] => components[index].NameToken;

        public IEnumerator<Token> GetEnumerator()
        {
            for (var i = 0; i < components.Count; i++)
            {
                yield return components[i].NameToken;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
