namespace MLIR.Text;

using System.Text;
using MLIR.Syntax;

/// <summary>
/// Provides token-aware, trivia-preserving writing services for MLIR syntax nodes.
/// </summary>
/// <remarks>
/// <para>
/// Formatting state — indentation level and a single pending suggested trivia — lives
/// here rather than being threaded as parameters through every <c>WriteTo</c> call.
/// </para>
/// <para>
/// The suggestion mechanism: call <see cref="SuggestTrivia"/> before emitting a node or
/// token to supply a default leading trivia.  The suggestion is consumed by the next
/// <see cref="WriteToken(SyntaxToken)"/> call that does not find preserved trivia on the
/// token.  Explicit <see cref="WriteToken(SyntaxToken, string)"/> calls bypass the
/// suggestion and always use the provided string.
/// </para>
/// </remarks>
public sealed class SyntaxWriter
{
    private readonly StringBuilder builder;

    /// <summary>Pending suggested trivia, consumed by the next parameterless <see cref="WriteToken(SyntaxToken)"/> call.</summary>
    private string? pendingTrivia;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxWriter"/> class.
    /// </summary>
    public SyntaxWriter()
        : this(new StringBuilder())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxWriter"/> class.
    /// </summary>
    /// <param name="builder">The underlying text builder to append to.</param>
    public SyntaxWriter(StringBuilder builder)
    {
        this.builder = builder;
    }

    /// <summary>
    /// Gets or sets the current indentation level.
    /// Callers set this before writing nested structures so that nodes can use it
    /// when computing suggested trivia (e.g., <c>"\n" + new string(' ', IndentLevel * 2)</c>).
    /// </summary>
    public int IndentLevel { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return builder.ToString();
    }

    /// <summary>
    /// Appends raw text directly to the output.
    /// </summary>
    /// <param name="text">The text to append.</param>
    public void Write(string text)
    {
        builder.Append(text);
    }

    /// <summary>
    /// Suggests a default leading trivia string for the next <see cref="WriteToken(SyntaxToken)"/> call.
    /// The suggestion is ignored when the token already carries preserved leading trivia.
    /// It is consumed (cleared) after the next emission regardless of whether it was applied.
    /// </summary>
    /// <param name="trivia">The trivia string to suggest.</param>
    public void SuggestTrivia(string trivia)
    {
        pendingTrivia = trivia;
    }

    /// <summary>
    /// Writes a full module syntax tree.
    /// </summary>
    /// <param name="module">The module to write.</param>
    public void WriteModule(ModuleSyntax module)
    {
        module.WriteTo(this);
    }

    /// <summary>
    /// Writes an operation syntax node.
    /// Callers should set <see cref="IndentLevel"/> and call <see cref="SuggestTrivia"/> as needed before invoking this method.
    /// </summary>
    /// <param name="operation">The operation to write.</param>
    public void WriteOperation(OperationSyntax operation)
    {
        operation.WriteTo(this);
    }

    /// <summary>
    /// Writes a region syntax node.
    /// Uses the current <see cref="IndentLevel"/> for indentation.
    /// </summary>
    /// <param name="region">The region to write.</param>
    public void WriteRegion(RegionSyntax region)
    {
        region.WriteTo(this);
    }

    /// <summary>
    /// Writes a block syntax node.
    /// Uses the current <see cref="IndentLevel"/> for indentation.
    /// </summary>
    /// <param name="block">The block to write.</param>
    public void WriteBlock(BlockSyntax block)
    {
        block.WriteTo(this);
    }

    /// <summary>
    /// Writes a delimited list of syntax tokens.
    /// Does nothing when <paramref name="list"/> has no opening delimiter token.
    /// </summary>
    /// <param name="list">The delimited token list to write.</param>
    public void WriteDelimitedList(DelimitedSyntaxList<SyntaxToken> list)
    {
        list.WriteTo(this, static (token, writer) => writer.WriteToken(token));
    }

    /// <summary>
    /// Writes a delimited list of syntax tokens.
    /// Does nothing when <paramref name="list"/> has no opening delimiter token.
    /// </summary>
    /// <param name="list">The delimited token list to write.</param>
    /// <param name="openLeadingTrivia">The explicit leading trivia for the opening delimiter token.</param>
    public void WriteDelimitedList(DelimitedSyntaxList<SyntaxToken> list, string openLeadingTrivia)
    {
        list.WriteTo(this, openLeadingTrivia, static (token, writer) => writer.WriteToken(token));
    }

    /// <summary>
    /// Writes a delimited list of named attribute syntax nodes.
    /// Does nothing when <paramref name="list"/> has no opening delimiter token.
    /// </summary>
    /// <param name="list">The delimited attribute list to write.</param>
    public void WriteDelimitedList(DelimitedSyntaxList<NamedAttributeSyntax> list)
    {
        list.WriteTo(this, static (attr, writer) => attr.WriteTo(writer));
    }

    /// <summary>
    /// Writes a delimited list of named attribute syntax nodes.
    /// Does nothing when <paramref name="list"/> has no opening delimiter token.
    /// </summary>
    /// <param name="list">The delimited attribute list to write.</param>
    /// <param name="openLeadingTrivia">The explicit leading trivia for the opening delimiter token.</param>
    public void WriteDelimitedList(DelimitedSyntaxList<NamedAttributeSyntax> list, string openLeadingTrivia)
    {
        list.WriteTo(this, openLeadingTrivia, static (attr, writer) => attr.WriteTo(writer));
    }

    /// <summary>
    /// Writes a delimited list of block argument syntax nodes.
    /// Does nothing when <paramref name="list"/> has no opening delimiter token.
    /// </summary>
    /// <param name="list">The delimited block argument list to write.</param>
    public void WriteDelimitedList(DelimitedSyntaxList<BlockArgumentSyntax> list)
    {
        list.WriteTo(this, static (arg, writer) => arg.WriteTo(writer));
    }

    /// <summary>
    /// Writes a delimited list of block argument syntax nodes.
    /// Does nothing when <paramref name="list"/> has no opening delimiter token.
    /// </summary>
    /// <param name="list">The delimited block argument list to write.</param>
    /// <param name="openLeadingTrivia">The explicit leading trivia for the opening delimiter token.</param>
    public void WriteDelimitedList(DelimitedSyntaxList<BlockArgumentSyntax> list, string openLeadingTrivia)
    {
        list.WriteTo(this, openLeadingTrivia, static (arg, writer) => arg.WriteTo(writer));
    }

    /// <summary>
    /// Writes a delimited list of type syntax nodes.
    /// Does nothing when <paramref name="list"/> has no opening delimiter token.
    /// </summary>
    /// <param name="list">The delimited type list to write.</param>
    /// <param name="openLeadingTrivia">The explicit leading trivia for the opening delimiter token.</param>
    public void WriteDelimitedList(DelimitedSyntaxList<TypeSyntax> list, string openLeadingTrivia)
    {
        list.WriteTo(this, openLeadingTrivia, static (type, writer) => type.WriteTo(writer));
    }

    /// <summary>
    /// Writes a separated list of syntax tokens.
    /// Does nothing when <paramref name="list"/> is empty.
    /// </summary>
    /// <param name="list">The separated token list to write.</param>
    public void WriteSeparatedList(SeparatedSyntaxList<SyntaxToken> list)
    {
        list.WriteTo(this, static (token, writer) => writer.WriteToken(token));
    }

    /// <summary>
    /// Writes a separated list of attribute value syntax nodes.
    /// Does nothing when <paramref name="list"/> is empty.
    /// </summary>
    /// <param name="list">The separated attribute value list to write.</param>
    /// <param name="firstLeadingTrivia">The explicit leading trivia for the first element.</param>
    public void WriteSeparatedList(SeparatedSyntaxList<AttributeValueSyntax> list, string firstLeadingTrivia)
    {
        SuggestTrivia(firstLeadingTrivia);
        list.WriteTo(this, static (attr, writer) => attr.WriteTo(writer));
    }

    /// <summary>
    /// Writes a type syntax node using the current pending suggested trivia for its leading trivia.
    /// Call <see cref="SuggestTrivia"/> beforehand to control the spacing.
    /// </summary>
    /// <param name="type">The type to write.</param>
    public void WriteType(TypeSyntax type)
    {
        type.WriteTo(this);
    }

    /// <summary>
    /// Writes a token using its preserved trivia when present, or the pending suggested trivia otherwise.
    /// The pending suggestion (set via <see cref="SuggestTrivia"/>) is consumed and cleared after this call.
    /// </summary>
    /// <param name="token">The token to write.</param>
    public void WriteToken(SyntaxToken token)
    {
        if (token.HasLeadingTrivia)
        {
            builder.Append(token.LeadingTrivia);
            builder.Append(token.Text);
            pendingTrivia = null;
            return;
        }

        builder.Append(pendingTrivia ?? string.Empty);
        pendingTrivia = null;
        builder.Append(token.Text);
    }

    /// <summary>
    /// Writes a token using its preserved trivia when present, or <paramref name="defaultLeadingTrivia"/> otherwise.
    /// This explicit form does not interact with the pending suggestion set by <see cref="SuggestTrivia"/>.
    /// </summary>
    /// <param name="token">The token to write.</param>
    /// <param name="defaultLeadingTrivia">The fallback leading trivia when the token carries no preserved trivia.</param>
    public void WriteToken(SyntaxToken token, string defaultLeadingTrivia)
    {
        builder.Append(token.LeadingTrivia ?? defaultLeadingTrivia);
        builder.Append(token.Text);
    }

    /// <summary>
    /// Writes preserved raw syntax text using the current pending suggested trivia for its leading trivia.
    /// The pending suggestion is consumed and cleared after this call.
    /// </summary>
    /// <param name="rawText">The raw syntax text to write.</param>
    public void WriteRaw(RawSyntaxText rawText)
    {
        if (rawText.HasLeadingTrivia)
        {
            builder.Append(rawText.LeadingTrivia);
            pendingTrivia = null;
        }
        else
        {
            builder.Append(pendingTrivia ?? string.Empty);
            pendingTrivia = null;
        }

        builder.Append(rawText.Text);
    }

    /// <summary>
    /// Writes preserved raw syntax text with an explicit fallback leading trivia.
    /// This explicit form does not interact with the pending suggestion set by <see cref="SuggestTrivia"/>.
    /// </summary>
    /// <param name="rawText">The raw syntax text to write.</param>
    /// <param name="defaultLeadingTrivia">The fallback leading trivia.</param>
    public void WriteRaw(RawSyntaxText rawText, string defaultLeadingTrivia)
    {
        builder.Append(rawText.LeadingTrivia ?? defaultLeadingTrivia);
        builder.Append(rawText.Text);
    }

    /// <summary>
    /// Writes a newline and indentation for the specified indent level.
    /// </summary>
    /// <param name="indentLevel">The indentation level to write.</param>
    public void SuggestIndentedNewLine(int indentLevel)
    {
        SuggestTrivia("\n" + new string(' ', indentLevel * 2));
    }

    /// <summary>
    /// Writes a newline and indentation for the current <see cref="IndentLevel"/>.
    /// </summary>
    public void SuggestIndentedNewLine()
    {
        SuggestIndentedNewLine(IndentLevel);
    }
}
